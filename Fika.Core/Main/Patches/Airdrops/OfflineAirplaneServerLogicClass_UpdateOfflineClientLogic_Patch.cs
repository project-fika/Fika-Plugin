using Comfort.Common;
using Fika.Core.Main.HostClasses;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Airdrops;

public class OfflineAirplaneServerLogicClass_UpdateOfflineClientLogic_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(OfflineAirplaneServerLogicClass).GetMethod(nameof(OfflineAirplaneServerLogicClass.UpdateOfflineClientLogic));
    }

    [PatchPostfix]
    public static void Postfix(AirplaneDataPacketStruct ___AirplaneDataPacketStruct)
    {
        Singleton<FikaHostGameWorld>.Instance.FikaHostWorld.WorldPacket.SyncObjectPackets.Add(___AirplaneDataPacketStruct);
    }
}
