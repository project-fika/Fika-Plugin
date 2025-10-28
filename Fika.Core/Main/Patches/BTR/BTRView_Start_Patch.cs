using EFT.GlobalEvents;
using EFT.Vehicle;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.BTR;

/// <summary>
/// Ensures that the BtrViewReadyEvent fires on the headless due to it having no <see cref="EFT.GamePlayerOwner"/>
/// </summary>
[DebugPatch]
public class BTRView_Start_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRView)
            .GetMethod(nameof(BTRView.Start));
    }

    [PatchPostfix]
    public static void Postfix()
    {
        if (FikaBackendUtils.IsHeadless)
        {
            var btrViewReadyEvent = GlobalEventHandlerClass.Instance.CreateCommonEvent<BtrViewReadyEvent>();
            btrViewReadyEvent.Invoke(string.Empty);
        }
    }
}
