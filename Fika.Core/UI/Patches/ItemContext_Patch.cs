using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.UI.Models;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches;

/// <summary>
/// Used to send items to other players
/// </summary>
[IgnoreAutoPatch]
public class ItemContext_Patch : ModulePatch
{
    private static int _lastIndex;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(SimpleContextMenu)
            .GetMethod(nameof(SimpleContextMenu.method_0))
            .MakeGenericMethod(typeof(EItemInfoButton));
    }

    [PatchPrefix]
    private static void Prefix(ItemInfoInteractionsAbstractClass<EItemInfoButton> contextInteractions, Item item)
    {
        if (contextInteractions is not ContextInteractionsAbstractClass gclass)
        {
            return;
        }

        // check for GClass increments
        var itemContext = gclass.ItemContextAbstractClass;
        if (itemContext.ViewType == EItemViewType.Inventory)
        {
            if (GClass2340.InRaid)
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

            if (item.Parent.Container.ParentItem.TemplateId == "55d7217a4bdc2d86028b456d") // Fix for UI Fixes
            {
                return;
            }

            if (item.PinLockState is EItemPinLockState.Pinned or EItemPinLockState.Locked) // Don't send pinned or locked items (who even does this?)
            {
                return;
            }

            // Check for GClass increments
            var dynamicInteractions = gclass.Dictionary_0
                ?? [];
            dynamicInteractions["SEND"] = new("SEND", "SEND", () =>
            {
                foreach (var itemId in FikaPlugin.Instance.BlacklistedItems)
                {
                    foreach (var attachedItem in item.GetAllItems())
                    {
                        if (attachedItem.TemplateId == itemId)
                        {
                            var itemText = ColorizeText(EColor.BLUE, item.ShortName.Localized());
                            if (attachedItem == item)
                            {
                                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.ITEM_BLACKLISTED.Localized(), itemText),
                                    iconType: EFT.Communications.ENotificationIconType.Alert);
                            }
                            else
                            {
                                var itemName = attachedItem.ShortName.Localized();
                                var attachedItemText = ColorizeText(EColor.BLUE, itemName);
                                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.ITEM_CONTAINS_BLACKLISTED.Localized(),
                                    [itemText, attachedItemText]),
                                    iconType: EFT.Communications.ENotificationIconType.Alert);
                            }
                            return;
                        }
                    }
                }

                AvailableReceiversRequest body = new(itemContext.Item.Id);
                var availableUsers = FikaRequestHandler.AvailableReceivers(body);

                // convert availableUsers.Keys
                List<TMP_Dropdown.OptionData> optionDatas = [];
                foreach (var user in availableUsers.Keys)
                {
                    optionDatas.Add(new()
                    {
                        text = user
                    });
                }

                // Get menu item
                var currentUI = GameObject.Find("SendItemMenu(Clone)");
                if (currentUI != null)
                {
                    Object.Destroy(currentUI);
                }

                // Create the window
                var matchMakerUiPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.SendItemMenu);
                var uiGameObj = Object.Instantiate(matchMakerUiPrefab);
                uiGameObj.transform.SetParent(GameObject.Find("Preloader UI/Preloader UI/UIContext/").transform);
                var screenController = Traverse.Create(CommonUI.Instance.InventoryScreen)
                    .Field<InventoryScreen.GClass3871>("ScreenController").Value;
                screenController.OnClose += () => Object.Destroy(uiGameObj);
                var sendItemUI = uiGameObj.GetComponent<SendItemUI>();
                sendItemUI.PlayersDropdown.ClearOptions();
                sendItemUI.PlayersDropdown.AddOptions(optionDatas);

                if (sendItemUI.PlayersDropdown.options.Count >= _lastIndex)
                {
                    sendItemUI.PlayersDropdown.value = _lastIndex;
                }

                sendItemUI.PlayersDropdown.onValueChanged.AddListener((_) => Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuDropdownSelect));
                sendItemUI.CloseButton.onClick.AddListener(() => Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick));
                sendItemUI.CloseButton.onClick.AddListener(() => Object.Destroy(uiGameObj));

                sendItemUI.SendButton.onClick.AddListener(() =>
                {
                    if (sendItemUI.PlayersDropdown.options[sendItemUI.PlayersDropdown.value].text != null)
                    {
                        var player = sendItemUI.PlayersDropdown.options[sendItemUI.PlayersDropdown.value].text;
                        _lastIndex = sendItemUI.PlayersDropdown.value;
                        if (Singleton<ClientApplication<ISession>>.Instantiated)
                        {
                            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.TradeOperationComplete);
                            Singleton<ClientApplication<ISession>>.Instance
                                .GetClientBackEndSession()
                                .SendOperationRightNow(new { Action = "SendToPlayer", id = itemContext.Item.Id, target = availableUsers[player] }, ar =>
                                {
                                    if (ar.Failed)
                                    {
                                        PreloaderUI.Instance.ShowErrorScreen("Fika.Core.ItemContextPatch", ar.Error ?? "An unknown error has occurred");
                                    }
                                });
                        }
                        else
                        {
                            PreloaderUI.Instance.ShowErrorScreen("Fika.Core.ItemContextPatch", "!Singleton<ISession>.Instantiated");
                        }
                    }
                    Object.Destroy(uiGameObj);
                });
            }, CacheResourcesPopAbstractClass.Pop<Sprite>("Characteristics/Icons/UnloadAmmo"));
        }
    }
}