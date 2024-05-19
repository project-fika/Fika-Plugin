using Aki.Reflection.Patching;
using EFT.UI;
using System.Reflection;
using UnityEngine.UI;

namespace Fika.Core.UI.Patches
{
    public class InventoryScroll_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleStashPanel).GetMethod(nameof(SimpleStashPanel.Show));
        }

        [PatchPrefix]
        public static void Prefix(ScrollRect ____stashScroll)
        {
            if (____stashScroll != null)
            {
                if (FikaPlugin.FasterInventoryScroll.Value)
                {
                    ____stashScroll.scrollSensitivity = FikaPlugin.FasterInventoryScrollSpeed.Value;
                }
                else
                {
                    ____stashScroll.scrollSensitivity = 63;
                }
            }
        }
    }
}
