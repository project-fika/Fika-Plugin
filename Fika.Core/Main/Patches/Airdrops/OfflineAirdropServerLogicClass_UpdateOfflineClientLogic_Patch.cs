using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.HostClasses;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Airdrops;

public class OfflineAirdropServerLogicClass_UpdateOfflineClientLogic_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(OfflineAirdropServerLogicClass)
            .GetMethod(nameof(OfflineAirdropServerLogicClass.UpdateOfflineClientLogic));
    }

    [PatchPostfix]
    public static void Postfix(AirplaneDataPacketStruct ___AirplaneDataPacketStruct)
    {
        var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
        hostWorld.WorldPacket.SyncObjectPackets.Add(___AirplaneDataPacketStruct);
        if (___AirplaneDataPacketStruct.PacketData.AirdropDataPacket.FallingStage is EAirdropFallingStage.Landed)
        {
            hostWorld.SetCritical();
        }
    }
}
