using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Networking.Http;
using Newtonsoft.Json.Linq;
using SPT.Reflection.Patching;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.UI.Patches
{
    public class MenuTaskBar_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuTaskBar).GetMethod(nameof(MenuTaskBar.Awake));
        }

        [PatchPostfix]
        public static void Postfix(MenuTaskBar __instance)
        {
            GameObject watchlistGameobject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/Watchlist");
            if (watchlistGameobject != null)
            {
                GameObject.Destroy(watchlistGameobject);
                GameObject fleaMarketGameObject = GameObject.Find("Preloader UI/Preloader UI/BottomPanel/Content/TaskBar/Tabs/FleaMarket");
                if (fleaMarketGameObject != null)
                {
                    GameObject downloadProfileGameObject = GameObject.Instantiate(fleaMarketGameObject);
                    downloadProfileGameObject.name = "DownloadProfile";
                    downloadProfileGameObject.transform.SetParent(fleaMarketGameObject.transform.parent, false);
                    downloadProfileGameObject.transform.SetSiblingIndex(10);

                    LocalizedText text = downloadProfileGameObject.GetComponentInChildren<LocalizedText>();
                    if (text != null)
                    {
                        text.method_2("DOWNLOAD PROFILE");
                    }

                    AnimatedToggle animatedToggle = downloadProfileGameObject.GetComponentInChildren<AnimatedToggle>();
                    if (animatedToggle != null)
                    {
                        animatedToggle.onValueChanged.AddListener(async (arg) =>
                        {
                            try
                            {
                                JObject profile = await FikaRequestHandler.GetProfile();

                                if (profile != null)
                                {
                                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonBottomBarClick);
                                    string installDir = Environment.CurrentDirectory;
                                    string fikaDir = installDir + @"/user/fika";

                                    if (!string.IsNullOrEmpty(installDir))
                                    {
                                        if (!Directory.Exists(fikaDir))
                                        {
                                            Directory.CreateDirectory(fikaDir);
                                        }

                                        string profileId = Singleton<ClientApplication<ISession>>.Instance.Session.Profile.ProfileId;

                                        if (File.Exists(@$"{fikaDir}/{profileId}.json"))
                                        {
                                            File.Copy(@$"{fikaDir}/{profileId}.json", @$"{fikaDir}/{profileId}.json.BAK", true);
                                        }

                                        File.WriteAllText(@$"{fikaDir}/{profileId}.json", profile.ToString());
                                        NotificationManagerClass.DisplayMessageNotification($"Saved profile '{profileId}'");

                                        GameObject.Destroy(downloadProfileGameObject);
                                    }
                                }
                                else
                                {
                                    NotificationManagerClass.DisplayWarningNotification("Failed to retrieve profile");
                                }
                            }
                            catch (Exception ex)
                            {
                                NotificationManagerClass.DisplayWarningNotification("An unknown exception was thrown, check your log file");
                                FikaPlugin.Instance.FikaLogger.LogError(ex.Message);
                            }
                        });
                    }
                }
            }
        }
    }
}
