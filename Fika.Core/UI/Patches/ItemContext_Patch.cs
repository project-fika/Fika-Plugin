using EFT.Communications;
using EFT.InventoryLogic.Operations;
using EFT.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.UI.Models;
using HarmonyLib;
using System.Text.Json.Serialization;
using SPTushonka.Reflection.Patching;
using TMPro;
using static Fika.Core.UI.FikaUIGlobals;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.UI.Patches;

/// <summary>
/// Used to send items to other players
/// </summary>
[IgnoreAutoPatch]
public sealed class ItemContext_Patch : ModulePatch
{
    private static int _lastIndex;
    private static readonly MongoID _uiFixesParent = new("55d7217a4bdc2d86028b456d");

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ItemUiContext).GetMethod(nameof(ItemUiContext.GetItemContextInteractions));
    }

    [PatchPostfix]
    private static void Postfix(ContextInteractions<EItemInfoButton> __result)
    {
        if (__result is not BaseItemContextInteractions itemContextInteractions)
        {
            return;
        }

        var item = itemContextInteractions.Item;
        if (item == null)
        {
            return;
        }

        var itemContext = itemContextInteractions.ItemContext;
        if (itemContext.ViewType == EItemViewType.Inventory)
        {
            if (InGameStatus.InRaid)
            {
                return;
            }

            // Save as variable in case we need to add more checks later...
            var menuUI = Singleton<MenuUI>.Instance;

            if (menuUI.HideoutAreaTransferItemsScreen.isActiveAndEnabled
                || menuUI.HideoutMannequinEquipmentScreen.isActiveAndEnabled
                || menuUI.HideoutCircleOfCultistsScreen.isActiveAndEnabled)
            {
                return;
            }

            var parentItems = item.GetAllParentItems();
            if (parentItems.Any(x => x is InventoryEquipment))
            {
                return;
            }

            if (item.Parent.Container.ParentItem.TemplateId == _uiFixesParent) // Fix for UI Fixes
            {
                return;
            }

            if (item.PinLockState is EItemPinLockState.Pinned or EItemPinLockState.Locked) // Don't send pinned or locked items (who even does this?)
            {
                return;
            }

            var dynamicInteractions = itemContextInteractions._dynamicInteractions;
            if (dynamicInteractions == null)
            {
                dynamicInteractions = new();
                itemContextInteractions._dynamicInteractions = dynamicInteractions;
            }
            dynamicInteractions["SEND"] = new("SEND", "SEND", new Action(() =>
            {
                foreach (var itemId in FikaPlugin.Instance.Settings.BlacklistedItems)
                {
                    foreach (var attachedItem in item.GetAllItems())
                    {
                        if (attachedItem.TemplateId == itemId)
                        {
                            var itemText = ColorizeText(EColor.BLUE, item.ShortName.Localized());
                            if (attachedItem == item)
                            {
                                FikaGlobals.DisplayMessage(string.Format(LocaleUtils.ITEM_BLACKLISTED.Localized(), itemText),
                                    iconType: ENotificationIconType.Alert);
                            }
                            else
                            {
                                var itemName = attachedItem.ShortName.Localized();
                                var attachedItemText = ColorizeText(EColor.BLUE, itemName);
                                FikaGlobals.DisplayMessage(string.Format(LocaleUtils.ITEM_CONTAINS_BLACKLISTED.Localized(),
                                    [itemText, attachedItemText]),
                                    iconType: ENotificationIconType.Alert);
                            }
                            return;
                        }
                    }
                }

                AvailableReceiversRequest body = new(itemContext.Item.Id);
                var availableUsers = FikaRequestHandler.AvailableReceivers(body);

                var items = MultiSelect.Count > 0 ? MultiSelect.Items.Select(i => i.Id).ToArray() : [item.Id];
#if DEBUG
                FikaGlobals.LogInfo($"{items.Length} items are selected");
#endif

                // convert availableUsers.Keys
                var optionDatas = new Il2CppSystem.Collections.Generic.List<TMP_Dropdown.OptionData>();
                foreach (var user in availableUsers.Keys)
                {
                    optionDatas.Add(new TMP_Dropdown.OptionData
                    {
                        text = user
                    });
                }

                // Get menu item
                var currentUI = GameObject.Find("SendItemMenu(Clone)");
                if (currentUI != null)
                {
                    GameObject.Destroy(currentUI);
                }

                // Create the window
                var matchMakerUiPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.SendItemMenu);
                var uiGameObj = UnityEngine.Object.Instantiate(matchMakerUiPrefab);
                uiGameObj.transform.SetParent(GameObject.Find("Preloader UI/Preloader UI/UIContext/").transform);
                var screenController = CommonUI.Instance.InventoryScreen.ScreenController;
                screenController.OnClose += () => UnityEngine.Object.Destroy(uiGameObj);
                var sendItemUI = SendItemUI.Create(uiGameObj);
                if (sendItemUI == null)
                {
                    GameObject.Destroy(uiGameObj);
                    return;
                }

                sendItemUI.PlayersDropdown.ClearOptions();
                sendItemUI.PlayersDropdown.AddOptions(optionDatas);

                if (sendItemUI.PlayersDropdown.options.Count >= _lastIndex)
                {
                    sendItemUI.PlayersDropdown.value = _lastIndex;
                }

                sendItemUI.PlayersDropdown.onValueChanged.AddListener(new Action<int>(_ => Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuDropdownSelect)));
                sendItemUI.CloseButton.onClick.AddListener(new Action(() => Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick)));
                sendItemUI.CloseButton.onClick.AddListener(new Action(() => UnityEngine.Object.Destroy(uiGameObj)));

                sendItemUI.PlayersFilter.onValueChanged.AddListener(new Action<string>(input =>
                {
                    if (string.IsNullOrEmpty(input))
                    {
                        sendItemUI.PlayersDropdown.ClearOptions();
                        sendItemUI.PlayersDropdown.AddOptions(optionDatas);
                        sendItemUI.PlayersDropdown.interactable = true;
                        return;
                    }

                    sendItemUI.PlayersDropdown.ClearOptions();

                    var filtered = new Il2CppSystem.Collections.Generic.List<string>();
                    foreach (var option in availableUsers.Keys)
                    {
                        if (option.Contains(input, System.StringComparison.OrdinalIgnoreCase))
                        {
                            filtered.Add(option);
                        }
                    }

                    sendItemUI.PlayersDropdown.AddOptions(filtered);
                    sendItemUI.PlayersDropdown.interactable = filtered.Count > 0;
                }));

                sendItemUI.SendButton.onClick.AddListener(new Action(() =>
                {
                    if (sendItemUI.PlayersDropdown.options.Count == 0) // no one valid to send to
                    {
                        return;
                    }

                    if (sendItemUI.PlayersDropdown.options[sendItemUI.PlayersDropdown.value].text != null)
                    {
                        var player = sendItemUI.PlayersDropdown.options[sendItemUI.PlayersDropdown.value].text;
                        _lastIndex = sendItemUI.PlayersDropdown.value;
                        if (Singleton<ClientApplication<IEftSession>>.Instantiated)
                        {
                            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.TradeOperationComplete);
                            Singleton<ClientApplication<IEftSession>>.Instance
                                .GetClientBackEndSession()
                                .SendOperationRightNow(new SendItemsPayload
                                {
                                    ItemIds = items,
                                    Target = availableUsers[player]
                                }, new Action<IResult>(ar =>
                                {
                                    if (ar.Failed)
                                    {
                                        PreloaderUI.Instance.ShowErrorScreen("Fika.Core.ItemContextPatch", ar.Error ?? "An unknown error has occurred");
                                    }
                                }));
                        }
                        else
                        {
                            PreloaderUI.Instance.ShowErrorScreen("Fika.Core.ItemContextPatch", "!Singleton<IEftSession>.Instantiated");
                        }
                    }
                    GameObject.Destroy(uiGameObj);
                }));
            }), ResourcesCache.Pop<Sprite>("Characteristics/Icons/UnloadAmmo"), null, null);
        }
    }

    public sealed class SendItemsPayload : CommandWithOwner
    {
        public SendItemsPayload(IntPtr pointer) : base(pointer)
        {
        }

        public SendItemsPayload() : base(ClassInjector.DerivedConstructorPointer<SendItemsPayload>())
        {
            ClassInjector.DerivedConstructorBody(this);
            ClassInjector.InvokeBaseConstructor<CommandWithOwner>(this);
        }

        [JsonPropertyName("Action")]
        public string Action = "SendToPlayer";

        [JsonPropertyName("itemIds")]
        public string[] ItemIds;

        [JsonPropertyName("target")]
        public string Target;
    }
}