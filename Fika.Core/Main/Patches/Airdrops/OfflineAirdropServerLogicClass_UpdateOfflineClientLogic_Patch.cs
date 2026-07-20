using EFT.Airdrop;
using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.HostClasses;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Airdrops;

public class OfflineAirdropServerLogicClass_UpdateOfflineClientLogic_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ServerAirDrop)
            .GetMethod(nameof(ServerAirDrop.UpdateOfflineClientLogic));
    }

    [PatchPostfix]
    public static void Postfix(SynchronizableObjectPacket ____offlineSyncPacket)
    {
        var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
        hostWorld.WorldPacket.SyncObjectPackets.Add(____offlineSyncPacket);
        if (____offlineSyncPacket.PacketData.AirdropDataPacket.FallingStage is EAirdropFallingStage.Landed)
        {
            hostWorld.SetCritical();
        }
    }
}
