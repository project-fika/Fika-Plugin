using System;
using Comfort.Common;
using EFT;
using EFT.GameTriggers;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Components;

internal class LocalFikaTriggersModule : LocalClientTriggersModule
{
    public override bool ApplyDamage(string profileId, DamageData[] damageData)
    {
        var clientLocalGameWorld = Singleton<GameWorld>.Instance as ClientLocalGameWorld;
        if (clientLocalGameWorld == null)
        {
            FikaGlobals.LogWarning($"GameWorld was not of correct type, was {Singleton<GameWorld>.Instance.GetType().Name}");
            return false;
        }

        for (var i = 0; i < clientLocalGameWorld.AllAlivePlayersList.Count; i++)
        {
            var player = clientLocalGameWorld.AllAlivePlayersList[i];
            if (string.Equals(player.ProfileId, profileId, StringComparison.OrdinalIgnoreCase))
            {
                if (player.IsYourPlayer || player.IsAI)
                {
                    foreach (var damageData2 in damageData)
                    {
                        if (PlayerBones.BodyPartCollidersStaticMap.TryGetValue(damageData2.BodyPartColliderType, out var ebodyPart))
                        {
                            var damageInfoStruct = new DamageInfoStruct
                            {
                                BodyPartColliderType = damageData2.BodyPartColliderType,
                                Damage = damageData2.Amount,
                                DamageType = EDamageType.Environment
                            };
                            player.ApplyDamageInfo(damageInfoStruct, ebodyPart, damageData2.BodyPartColliderType, damageData2.Amount);
                        }
                    }
                    return player.HealthController.IsAlive;
                }
            }
        }

        return false;
    }
}
