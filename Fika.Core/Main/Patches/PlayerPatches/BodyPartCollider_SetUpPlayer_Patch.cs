using EFT;
using Fika.Core.Main.BotClasses;
using Fika.Core.Main.ObservedClasses.PlayerBridge;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.PlayerPatches
{
    public class BodyPartCollider_SetUpPlayer_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BodyPartCollider).GetMethod(nameof(BodyPartCollider.SetUpPlayer));
        }

        [PatchPrefix]
        public static bool Prefix(BodyPartCollider __instance, IPlayer iPlayer)
        {
            if (iPlayer != null)
            {
                if (iPlayer is FikaBot fikaBot)
                {
                    __instance.InitColliderSettings();
                    __instance.playerBridge = new BotPlayerBridge(fikaBot);
                    return false;
                }

                if (iPlayer is ObservedPlayer observedPlayer)
                {
                    __instance.InitColliderSettings();
                    if (FikaBackendUtils.IsServer)
                    {
                        __instance.playerBridge = new ObservedHostBridge(observedPlayer);
                        return false;
                    }
                    __instance.playerBridge = new ObservedClientBridge(observedPlayer);
                    return false;
                }
            }
            return true;
        }
    }
}
