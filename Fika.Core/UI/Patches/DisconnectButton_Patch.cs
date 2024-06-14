using Comfort.Common;
using EFT.UI;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Networking;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches
{
    public class DisconnectButton_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod(nameof(MenuScreen.method_11));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            if (MatchmakerAcceptPatches.IsServer)
            {
                FikaServer server = Singleton<FikaServer>.Instance;
                if (server != null && server.NetServer.ConnectedPeersCount > 0)
                {
                    NotificationManagerClass.DisplayWarningNotification($"You cannot disconnect while there are still peers connected! Remaining: {server.NetServer.ConnectedPeersCount}");
                    return false;
                }
            }
            return true;
        }
    }
}
