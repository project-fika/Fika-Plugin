using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Players;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.AkiSupport.Overrides
{
    internal class AddEnemyToAllGroupsInBotZonePatch_Override : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), nameof(BotsController.AddEnemyToAllGroupsInBotZone));
        }

        /// <summary>
        /// AddEnemyToAllGroupsInBotZone()
        /// Goal: by default, AddEnemyToAllGroupsInBotZone doesn't check if the bot group is on the same side as the player.
        /// The effect of this is that when you are a Scav and kill a Usec, every bot group in the zone will aggro you including other Scavs.
        /// This should fix that.
        /// </summary>
        [PatchPrefix]
        private static bool PatchPrefix(BotsController __instance, IPlayer aggressor, IPlayer groupOwner, IPlayer target)
        {
            if (!groupOwner.IsAI)
            {
                return false;
            }

            // If you damage yourself exit early as we dont want to try add ourself to our own enemy list
            if (aggressor.IsYourPlayer && target.IsYourPlayer)
            {
                return false;
            }

            if (aggressor is ObservedCoopPlayer)
            {
                if (target.IsYourPlayer)
                {
                    return false;
                }
            }

            BotZone botZone = groupOwner.AIData.BotOwner.BotsGroup.BotZone;
            foreach (KeyValuePair<BotZone, GClass492> item in __instance.Groups())
            {
                if (item.Key != botZone)
                {
                    continue;
                }

                foreach (BotsGroup group in item.Value.GetGroups(notNull: true))
                {
                    bool differentSide = aggressor.Side != group.Side;
                    bool sameSide = aggressor.Side == target.Side;

                    if (!group.Enemies.ContainsKey(aggressor)
                        && (differentSide || !sameSide)
                        && !group.HaveMemberWithRole(WildSpawnType.gifter)
                        && group.ShallRevengeFor(target)
                        )
                    {
                        group.AddEnemy(aggressor, EBotEnemyCause.AddEnemyToAllGroupsInBotZone);
                    }
                }
            }

            return false;
        }
    }
}
