using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.LocalGame
{
    /// <summary>
    /// Used to prevent the queue on the world to be stuck in an endless loop
    /// </summary>
    public class World_AddSpawnQuestLootPacket_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(World).GetMethod(nameof(World.AddSpawnQuestLootPacket));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }
}