using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Mines;

public class MineDirectional_OnTriggerEnter_Patch : FikaPatch
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
