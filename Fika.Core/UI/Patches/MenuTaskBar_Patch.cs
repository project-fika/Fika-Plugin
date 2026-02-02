using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.UI;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using SPT.Reflection.Patching;
using UnityEngine.UI;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches;

public class MenuTaskBar_Patch : ModulePatch
{
    private static DateTime _lastDownload = DateTime.Now.AddMinutes(-2);

    protected override MethodBase GetTargetMethod()
    {
        return typeof(MenuTaskBar).GetMethod(nameof(MenuTaskBar.Awake));
    }

    [PatchPostfix]
    public static void Postfix(Dictionary<EMenuType, AnimatedToggle> ____toggleButtons, Dictionary<EMenuType,
        HoverTooltipArea> ____hoverTooltipAreas, ref GameObject[] ____newInformation)
    {
        var watchlistGameobject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/Watchlist");
        if (watchlistGameobject != null)
        {
            GameObject.Destroy(watchlistGameobject);
            var fleaMarketGameObject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/FleaMarket");
            if (fleaMarketGameObject != null)
            {
                var downloadProfileGameObject = GameObject.Instantiate(fleaMarketGameObject);
                downloadProfileGameObject.name = "DownloadProfile";
                downloadProfileGameObject.transform.SetParent(fleaMarketGameObject.transform.parent, false);
                downloadProfileGameObject.transform.SetSiblingIndex(10);

                var downloadProfileButton = downloadProfileGameObject.transform.GetChild(0).gameObject;
                downloadProfileButton.name = "DownloadProfileButton";

                var text = downloadProfileGameObject.GetComponentInChildren<LocalizedText>();
                if (text != null)
                {
                    text.method_2(LocaleUtils.UI_DOWNLOAD_PROFILE.Localized());
                    text.LocalizationKey = "";
                }

                var buildListObject = GameObject.Find("/Menu UI/UI/EquipmentBuildsScreen/Panels/BuildsListPanel/Header/SizeSample/Selected/Icon");
                if (buildListObject != null)
                {
                    var downloadImage = buildListObject.GetComponent<Image>();
                    var downloadProfileImage = downloadProfileButton.transform.GetChild(0).gameObject.GetComponent<Image>();
                    if (downloadProfileImage != null && downloadImage != null)
                    {
                        downloadProfileImage.sprite = downloadImage.sprite;
                    }
                }

                var animatedToggle = downloadProfileGameObject.GetComponentInChildren<AnimatedToggle>();
                if (animatedToggle != null)
                {
                    animatedToggle.onValueChanged.AddListener(async (_) =>
                    {
                        var elapsed = DateTime.Now - _lastDownload;
                        if (elapsed.TotalMinutes <= 1)
                        {
                            NotificationManagerClass.DisplayMessageNotification(
                                $"Please wait {(int)Math.Ceiling(60 - elapsed.TotalSeconds)} seconds before attempting to download your profile again!",
                                iconType: ENotificationIconType.Alert, textColor: Color.red);
                            return;
                        }

                        try
                        {
                            var profileResponse = await FikaRequestHandler.GetProfile();
                            var responseHasError = !string.IsNullOrEmpty(profileResponse.ErrorMessage);
                            var error = responseHasError ? profileResponse.ErrorMessage : "Failed to retrieve profile";
                            if (!responseHasError && profileResponse?.Profile != null)
                            {
                                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonBottomBarClick);
                                var installDir = Environment.CurrentDirectory;
                                var fikaDir = installDir + @"\SPT\user\fika";

                                if (!string.IsNullOrEmpty(installDir))
                                {
                                    if (!Directory.Exists(fikaDir))
                                    {
                                        Directory.CreateDirectory(fikaDir);
                                    }

                                    // create dir, put profile in dir and moddata, change to dictionary, key = modName

                                    var profileId = Singleton<ClientApplication<ISession>>.Instance.Session.Profile.ProfileId;

                                    var profileDir = Path.Combine(fikaDir, profileId);
                                    if (!Directory.Exists(profileDir))
                                    {
                                        Directory.CreateDirectory(profileDir);
                                    }

                                    if (File.Exists(@$"{profileDir}\{profileId}.json"))
                                    {
                                        File.Copy(Path.Combine(profileDir, $"{profileId}.json"), Path.Combine(profileDir, $"{profileId}.json.BAK"), true);
                                    }

                                    if (profileResponse.ModData != null)
                                    {
                                        foreach ((var modName, var modData) in profileResponse.ModData)
                                        {
                                            File.WriteAllText(Path.Combine(profileDir, $"{modName}.json"), modData);
                                        }
                                    }

                                    File.WriteAllText(Path.Combine(profileDir, $"{profileId}.json"), profileResponse.Profile.ToString());
                                    NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.SAVED_PROFILE.Localized(),
                                        [ColorizeText(EColor.BLUE, profileId), fikaDir]));

                                    /* ____toggleButtons.Remove(EMenuType.NewsHub);
                                     ____hoverTooltipAreas.Remove(EMenuType.NewsHub);
                                     GameObject.Destroy(downloadProfileGameObject);*/
                                }
                            }
                            else
                            {
                                NotificationManagerClass.DisplayWarningNotification(error);
                            }
                        }
                        catch (Exception ex)
                        {
                            NotificationManagerClass.DisplayWarningNotification(LocaleUtils.UNKNOWN_ERROR.Localized());
                            FikaGlobals.LogError(ex.Message);
                        }

                        _lastDownload = DateTime.Now;
                    });

                    var surveyButton = ____hoverTooltipAreas[EMenuType.NewsHub];

                    ____toggleButtons.Remove(EMenuType.NewsHub);
                    ____hoverTooltipAreas.Remove(EMenuType.NewsHub);
                    GameObject.Destroy(surveyButton.gameObject);
                    List<GameObject> newList = [.. ____newInformation];
                    newList.Remove(newList[^1]);
                    ____newInformation = [.. newList];

                    ____toggleButtons.Add(EMenuType.NewsHub, animatedToggle);
                    ____hoverTooltipAreas.Add(EMenuType.NewsHub, downloadProfileGameObject.GetComponent<HoverTooltipArea>());
                }
            }
        }
    }
}
