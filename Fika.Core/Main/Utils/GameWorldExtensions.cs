using EFT;

namespace Fika.Core.Main.Utils;

public static class GameWorldExtensions
{
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
