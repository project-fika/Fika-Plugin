using EFT.Airdrop;
using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.HostClasses;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.Airdrops;

public class ServerPlane_UpdateOfflineClientLogic_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ServerPlane)
            .GetMethod(nameof(ServerPlane.UpdateOfflineClientLogic));
    }

    [PatchPostfix]
    public static void Postfix(EFT.Airdrop.ServerPlane __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
        hostWorld.WorldPacket.SyncObjectPackets.Add(__instance._offlineSyncPacket);
        if (__instance._offlineSyncPacket.Outdated)
        {
            hostWorld.SetCritical();
        }
    }
}
