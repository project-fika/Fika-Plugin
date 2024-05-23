using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Networking.Http;
using Fika.Core.UI.Models;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Fika.Core.UI.Patches
{
    public class ItemContext_Patch : ModulePatch
    {
        private static int lastIndex = 0;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleContextMenu).GetMethod(nameof(SimpleContextMenu.method_0)).MakeGenericMethod(typeof(EItemInfoButton));
        }

        [PatchPrefix]
        private static void Prefix(ItemInfoInteractionsAbstractClass<EItemInfoButton> contextInteractions, Item item)
        {
            if (contextInteractions is not GClass3021 gclass)
            {
                return;
            }

            ItemContextAbstractClass itemContext = Traverse.Create(contextInteractions).Field<ItemContextAbstractClass>("gclass2813_0").Value;
            if (itemContext.ViewType == EItemViewType.Inventory)
            {
                if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld)
                {
                    return;
                }

                // Save as variable in case we need to add more checks later...
                MenuUI menuUI = Singleton<MenuUI>.Instance;

                if (menuUI.HideoutAreaTransferItemsScreen.isActiveAndEnabled)
                {
                    return;
                }

                IEnumerable<Item> parentItems = item.GetAllParentItems();
                if (parentItems.Any(x => x is EquipmentClass))
                {
                    return;
                }

                Dictionary<string, DynamicInteractionClass> dynamicInteractions = Traverse.Create(contextInteractions).Field<Dictionary<string, DynamicInteractionClass>>("dictionary_0").Value;
                if (dynamicInteractions == null)
                {
                    dynamicInteractions = new Dictionary<string, DynamicInteractionClass>();
                }

                dynamicInteractions["SEND"] = new("SEND", "SEND", () =>
                {
                    var body = new AvailableReceiversRequest(itemContext.Item.Id);
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
                    GameObject currentUI = GameObject.Find("SendItemMenu(Clone)");
                    if (currentUI != null)
                    {
                        Object.Destroy(currentUI);
                    }

                    // Create the window
                    GameObject matchMakerUiPrefab = InternalBundleLoader.Instance.GetAssetBundle("senditemmenu").LoadAsset<GameObject>("SendItemMenu");
                    GameObject uiGameObj = Object.Instantiate(matchMakerUiPrefab);
                    uiGameObj.transform.SetParent(GameObject.Find("Preloader UI/Preloader UI/UIContext/").transform);
                    InventoryScreen.GClass3116 screenController = Traverse.Create(CommonUI.Instance.InventoryScreen).Field<InventoryScreen.GClass3116>("ScreenController").Value;
                    screenController.OnClose += () => { Object.Destroy(uiGameObj); };
                    SendItemUI sendItemUI = uiGameObj.GetComponent<SendItemUI>();
                    sendItemUI.PlayersDropdown.ClearOptions();
                    sendItemUI.PlayersDropdown.AddOptions(optionDatas);

                    if (sendItemUI.PlayersDropdown.options.Count >= lastIndex)
                    {
                        sendItemUI.PlayersDropdown.value = lastIndex;
                    }

                    sendItemUI.PlayersDropdown.onValueChanged.AddListener((value) =>
                    {
                        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuDropdownSelect);
                    });

                    sendItemUI.CloseButton.onClick.AddListener(() =>
                    {
                        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
                    });

                    sendItemUI.CloseButton.onClick.AddListener(() =>
                    {
                        Object.Destroy(uiGameObj);
                    });

                    sendItemUI.SendButton.onClick.AddListener(() =>
                    {
                        if (sendItemUI.PlayersDropdown.options[sendItemUI.PlayersDropdown.value].text != null)
                        {
                            string player = sendItemUI.PlayersDropdown.options[sendItemUI.PlayersDropdown.value].text;
                            lastIndex = sendItemUI.PlayersDropdown.value;
                            if (Singleton<ClientApplication<ISession>>.Instantiated)
                            {
                                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.TradeOperationComplete);
                                Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession().SendOperationRightNow(new { Action = "SendToPlayer", id = itemContext.Item.Id, target = availableUsers[player] }, ar =>
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
}
