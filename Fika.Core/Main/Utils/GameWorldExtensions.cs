using EFT;
using EFT.InventoryLogic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using JsonType;

namespace Fika.Core.Main.Utils;

public static class GameWorldExtensions
{
    /// <summary>
    /// Spawns a battle pass document from <paramref name="lootItem"/> belonging to <paramref name="profileId"/>
    /// </summary>
    public static void SpawnBattlePassDocument(this GameWorld gameWorld, JsonLootItem lootItem, Item item, string profileId)
    {
        var originalItem = lootItem.Item;
        var originalProfiles = lootItem.ValidProfiles;

        lootItem.Item = item;
        lootItem.ValidProfiles = new Il2CppReferenceArray<MongoID>(1L)
        {
            [0] = new MongoID(profileId)
        };

        var firstIndex = gameWorld.LootItems.Count;

        try
        {
            gameWorld.SpawnLootItem(lootItem, true, null, null);
        }
        finally
        {
            lootItem.Item = originalItem;
            lootItem.ValidProfiles = originalProfiles;
        }

        HideFromOtherPlayers(gameWorld, firstIndex, profileId);
    }

    private static void HideFromOtherPlayers(GameWorld gameWorld, int firstIndex, string profileId)
    {
        var mainPlayer = gameWorld.MainPlayer;
        if (mainPlayer == null || mainPlayer.ProfileId == profileId)
        {
            return;
        }

        var lootItems = gameWorld.LootItems;
        for (var i = firstIndex; i < lootItems.Count; i++)
        {
            lootItems.GetByIndex(i).gameObject.SetActive(false);
        }
    }

    public static IObserverToPlayerBridge GetAlivePlayerBridgeByProfileID(this GameWorld gameWorld, string profileId)
    {
        foreach (var player in gameWorld.AllAlivePlayersList)
        {
            if (player.ProfileId == profileId)
            {
                return gameWorld.GetAlivePlayerBridgeByRaidID(player.RaidId);
            }
        }
        
        return null;
    }
}
