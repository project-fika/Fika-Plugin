using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Matchmaker;

namespace Fika.Core.Coop.Patches
{
    internal class BaseLocalGame_method_6_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BaseLocalGame<GamePlayerOwner>).GetMethod(nameof(BaseLocalGame<GamePlayerOwner>.method_11));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref LocationSettingsClass.Location location)
        {
            if (MatchmakerAcceptPatches.IsClient && MatchmakerAcceptPatches.IsReconnect)
            {
				location.Loot = MatchmakerAcceptPatches.ReconnectPacket.Value.Items;
            }

			return true;
		}
    }
}