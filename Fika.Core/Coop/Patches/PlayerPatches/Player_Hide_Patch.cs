using EFT;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class Player_Hide_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod(nameof(LocalPlayer.Hide));
        }

        // Check for GClass increments
        [PatchPrefix]
        public static bool Prefix(GClass896 ___gclass896_0)
        {
            ___gclass896_0.Hide();
            return false;
        }
    }
}
