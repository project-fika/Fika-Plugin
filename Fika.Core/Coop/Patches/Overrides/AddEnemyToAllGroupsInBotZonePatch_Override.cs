using EFT;
using Fika.Core.Coop.Players;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Overrides
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
            foreach (KeyValuePair<BotZone, GClass491> item in __instance.Groups())
            {
                if (item.Key != botZone)
                {
                    continue;
                }

                foreach (BotsGroup group in item.Value.GetGroups(notNull: true))
                {
                    bool differentSide = aggressor.Side != group.Side;
                    bool sameSide = aggressor.Side == target.Side;

                    if (!group.HaveFollowTarget(aggressor)
                        && !group.Enemies.ContainsKey(aggressor)
                        && (differentSide || !sameSide)
                        && !group.HaveMemberWithRole(WildSpawnType.gifter)
                        && !group.HaveMemberWithRole(WildSpawnType.sectantWarrior)
                        && !group.HaveMemberWithRole(WildSpawnType.sectantPriest)
                        && !group.InitialFileSettings.Boss.NOT_ADD_TO_ENEMY_ON_KILLS
                        && group.ShallRevengeFor(target))
                    {
                        group.AddEnemy(aggressor, EBotEnemyCause.AddEnemyToAllGroupsInBotZone);
                    }
                }
            }

            return false;
        }
    }
}
