using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using EFT.UI;

namespace Fika.Core.UI.Patches
{
    public class MenuScreen_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(x => x.Name == "Show" && x.GetParameters()[0].Name == "profile");
        }
        
        [PatchPrefix]
        private static bool PreFix(MenuScreen __instance, MatchmakerPlayerControllerClass matchmaker)
        {
            // Get the group controller straight from the main screen setup
            FikaGroupUtils.GroupController = matchmaker;

            return true;
        }
    }
}