using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Mines;

public class MineDirectional_OnTriggerEnter_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MineDirectional).GetMethod(nameof(MineDirectional.OnTriggerEnter));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        if (FikaBackendUtils.IsClient)
        {
            return false;
        }
        return true;
    }
}
