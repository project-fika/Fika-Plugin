using System.Reflection;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Mines;

public class MineDirectionalColliders_OnTriggerEnter_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MineDirectionalColliders).GetMethod(nameof(MineDirectionalColliders.OnTriggerEnter));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return !FikaBackendUtils.IsClient;
    }
}
