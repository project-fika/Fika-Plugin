using EFT.Airdrop;
using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.HostClasses;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Airdrops;

public class OfflineAirplaneServerLogicClass_UpdateOfflineClientLogic_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ServerPlane)
            .GetMethod(nameof(ServerPlane.UpdateOfflineClientLogic));
    }

    [PatchPostfix]
    public static void Postfix(SynchronizableObjectPacket ____offlineSyncPacket)
    {
        var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
        hostWorld.WorldPacket.SyncObjectPackets.Add(____offlineSyncPacket);
        if (____offlineSyncPacket.Outdated)
        {
            hostWorld.SetCritical();
        }
    }
}
