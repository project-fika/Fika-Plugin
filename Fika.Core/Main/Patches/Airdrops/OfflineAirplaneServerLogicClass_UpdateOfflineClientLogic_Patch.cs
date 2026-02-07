using System.Reflection;
using Comfort.Common;
using Fika.Core.Main.HostClasses;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Airdrops;

public class OfflineAirplaneServerLogicClass_UpdateOfflineClientLogic_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(OfflineAirplaneServerLogicClass)
            .GetMethod(nameof(OfflineAirplaneServerLogicClass.UpdateOfflineClientLogic));
    }

    [PatchPostfix]
    public static void Postfix(AirplaneDataPacketStruct ___AirplaneDataPacketStruct)
    {
        var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
        hostWorld.WorldPacket.SyncObjectPackets.Add(___AirplaneDataPacketStruct);
        if (___AirplaneDataPacketStruct.Outdated)
        {
            hostWorld.SetCritical();
        }
    }
}
