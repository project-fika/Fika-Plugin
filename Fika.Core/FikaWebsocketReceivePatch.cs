using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.UI.Custom;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Aki.Reflection.Patching;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

namespace Fika.Core
{
    public class FikaWebsocketReceivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(NotificationManagerClass).GetMethod(nameof(NotificationManagerClass.method_6));
        }

        [PatchPrefix]
        private static bool Prefix(byte[] bytes)
        {
            string text = Encoding.UTF8.GetString(bytes);
            JObject jsonObject = JObject.Parse(text);
            bool flag = !jsonObject.ContainsKey("type");
            bool flag2;
            if (flag)
            {
                flag2 = true;
            }
            else
            {
                string type = jsonObject.Value<string>("type");
                jsonObject.Remove("type");
                string text2 = type;
                string text3 = text2;
                if (!(text3 == "fikaDedicatedJoinMatch"))
                {
                    flag2 = true;
                }
                else
                {
                    string matchId = jsonObject.Value<string>("matchId");
                    MatchMakerAcceptScreen matchMakerAcceptScreen = GameObject.FindObjectOfType<MatchMakerAcceptScreen>();
                    if (matchMakerAcceptScreen == null)
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", "Failed to find MatchMakerAcceptScreen", () =>
                        {
                            var acceptScreen = GameObject.FindObjectOfType<MatchMakerAcceptScreen>();
                            var controller = Traverse.Create(acceptScreen).Field<MatchMakerAcceptScreen.GClass3150>("ScreenController").Value;
                            controller.CloseScreen();
                        });

                        return false;
                    }

                    if (matchId is not null)
                    {
                        //Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.QuestCompleted);
                        TarkovApplication tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;
                        tarkovApplication.StartCoroutine(MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, matchId, null, () =>
                        {
                            Traverse.Create(matchMakerAcceptScreen).Field<DefaultUIButton>("_acceptButton").Value.OnClick.Invoke();
                        }));
                    }
                    else
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", "Received fikaJoinMatch WS event but there was no matchId", () =>
                        {
                            var acceptScreen = GameObject.FindObjectOfType<MatchMakerAcceptScreen>();
                            var controller = Traverse.Create(acceptScreen).Field<MatchMakerAcceptScreen.GClass3150>("ScreenController").Value;
                            controller.CloseScreen();
                        });
                    }

                    flag2 = false;
                }
            }
            return flag2;
        }
    }
}
