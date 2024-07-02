using SPT.Reflection.Patching;
using System.Reflection;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;

namespace Fika.Core.Coop.Patches
{
    public class MatchmakerPlayerController_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(MatchmakerPlayerControllerClass).GetMethod("NotificationReceivedHandler");

        [PatchPrefix]
        private static bool PatchPrefix(MatchmakerPlayerControllerClass __instance, NotificationAbstractClass notification)
        {
            return notification switch
            {
                GClass2020 _ => false,
                _ => true
            };
        }

        [PatchPostfix]
        private static void PostPrefix(MatchmakerPlayerControllerClass __instance, NotificationAbstractClass notification)
        {
            if (notification is not GClass2020)
            {
                return;
            };
            
            // Get the serverId of the match the group leader is joining
            RaidGroupResponse raidGroup = FikaRequestHandler.GetGroupRaid();
            
            // Do nothing if the group leader isn't in a raid
            if (raidGroup.ServerId is not { Length: > 0 })
            {
                return;
            }
            
            // Join the raid
            FikaBackendUtils.MatchMakerUIScript.JoinMatchCoroutine(FikaBackendUtils.Profile.ProfileId, raidGroup.ServerId);
        }
    }
}