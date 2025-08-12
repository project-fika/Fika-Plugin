using EFT.UI.Ragfair;
using Fika.Core.Patching;
using Fika.Core.UI.Custom;
using System.Reflection;

namespace Fika.Core.UI.Patches.MainMenuUI;

public class RagfairScreen_Show_Patch : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagfairScreen).GetMethod(nameof(RagfairScreen.Show));
    }

    [PatchPostfix]
    public static void Postfix()
    {
        if (MainMenuUIScript.Exist)
        {
            MainMenuUIScript.Instance.UpdatePresence(FikaUIGlobals.EFikaPlayerPresence.IN_FLEA);
        }
    }
}
