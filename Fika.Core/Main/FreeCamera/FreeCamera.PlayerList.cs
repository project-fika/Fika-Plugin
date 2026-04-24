using System.Collections.Generic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.FreeCamera;

public partial class FreeCamera
{
    private bool _hidePlayerList;
    private FikaPlayer _lastSpectatingPlayer;
    private Dictionary<int, ListPlayer> _playersTracker;

    ECameraState _cameraState;

    private void OnPlayerSpawned(FikaPlayer player)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Adding ListPlayer for {player.Profile.GetCorrectedNickname()}");
#endif
        if (!_allowSpectateBots && (player.IsAI || player.IsObservedAI))
        {
            for (var i = 0; i < _coopHandler.HumanPlayers.Count; i++)
            {
                var humanPlayer = _coopHandler.HumanPlayers[i];
                if (humanPlayer.HealthController.IsAlive && !_coopHandler.ExtractedPlayers.Contains(humanPlayer.NetId))
                {
                    return;
                }
            }
        }

        if (!_playersTracker.ContainsKey(player.NetId))
        {
            var newObj = Instantiate(_freecamUI.ListPlayerPrefab, _freecamUI.ListOfPlayers.transform);
            var listPlayer = newObj.GetComponent<ListPlayer>();
            _playersTracker.Add(player.NetId, listPlayer);
            listPlayer.Init(player);
            return;
        }

#if DEBUG
        FikaGlobals.LogWarning($"ListPlayer for {player.Profile.GetCorrectedNickname()} already existed");
#endif
    }

    private bool IsPlayerHuman(FikaPlayer player)
    {
        return (FikaBackendUtils.IsClient && !player.IsObservedAI) || (FikaBackendUtils.IsServer && !player.IsAI);
    }

    private void OnPlayerDestroyed(FikaPlayer player)
    {
        if (_playersTracker.Remove(player.NetId, out var listPlayer))
        {
            Destroy(listPlayer.gameObject);
        }

        if (!_allowSpectateBots && IsPlayerHuman(player))
        {
            for (var i = 0; i < _coopHandler.HumanPlayers.Count; i++)
            {
                if (_coopHandler.HumanPlayers[i].HealthController.IsAlive)
                {
                    return;
                }
            }
            ForceAddPlayers();
        }
    }

    private void OnPlayerDeath(FikaPlayer player)
    {
        if (_playersTracker.Remove(player.NetId, out var listPlayer))
        {
            Destroy(listPlayer.gameObject);
        }

        if (!_allowSpectateBots && IsPlayerHuman(player))
        {
            for (var i = 0; i < _coopHandler.HumanPlayers.Count; i++)
            {
                if (_coopHandler.HumanPlayers[i].HealthController.IsAlive)
                {
                    return;
                }
            }
            ForceAddPlayers();
        }
    }

    private void UpdatePlayerList()
    {
        foreach (var player in _playersTracker.Values)
        {
            player.ManualUpdate();
        }
    }
}
