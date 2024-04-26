using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.Players;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    /// <summary>
    /// This patch prevents a null exception when an <see cref="ObservedCoopPlayer"/> is hit by a mine explosion
    /// </summary>
    internal class Minefield_method_2_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Minefield).GetMethod(nameof(Minefield.method_2));
        }

        [PatchPrefix]
        public static bool Prefix(IPlayer player)
        {
            if (player is ObservedCoopPlayer)
            {
                return false;
            }
            return true;
        }
    }
}
