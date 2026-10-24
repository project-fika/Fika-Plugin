using System.Reflection;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using static EFT.TarkovApplication;
using Fika.Core.Main.Utils;

namespace Fika.Core.UI.Patches.LoadingScreen;

public class MapCachingPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return FindLoadMapAndDataLambda("_LoadMapAndData_b__0");
    }
    
    internal static MethodInfo FindLoadMapAndDataLambda(string name)
    {
        foreach (var type in typeof(EFT.TarkovApplication).GetNestedTypes())
        {
            var method = AccessTools.DeclaredMethod(type, name);
            if (method != null)
            {
                return method;
            }
        }

        return null;
    }

    [PatchPrefix]
    public static void Postfix(float totalProgress)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        var progress = 25f + (totalProgress * 25f);
        LoadingScreenUI.Instance.UpdateAndBroadcast(progress);
    }
}
