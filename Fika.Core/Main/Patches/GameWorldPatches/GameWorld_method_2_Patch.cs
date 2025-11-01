using Comfort.Common;
using EFT;
using Fika.Core.Main.HostClasses;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.GameWorldPatches;

public class GameWorld_method_2_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld)
            .GetMethod(nameof(GameWorld.method_2));
    }

    [PatchPrefix]
    public static bool Prefix(Throwable grenade)
    {
        if (grenade != null && grenade.HasNetData)
        {
            var hostWorld = Singleton<FikaHostGameWorld>.Instance.FikaHostWorld;
            if (hostWorld != null)
            {
                hostWorld.WorldPacket.GrenadePackets.Add(grenade.GetNetPacket());
                hostWorld.SetCritical();
            }
        }

        return false;
    }
}
