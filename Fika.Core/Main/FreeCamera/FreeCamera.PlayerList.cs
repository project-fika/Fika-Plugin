using System.Collections.Generic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.FreeCamera;

public partial class FreeCamera
{
    private bool _hidePlayerList;
    private FikaPlayer _lastSpectatingPlayer;
    private Dictionary<int, ListPlayer> _playersTracker;
    private List<int> _playersToRemove;

    ECameraState _cameraState;

    private void OnPlayerSpawned(FikaPlayer player)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Adding ListPlayer for {player.Profile.GetCorrectedNickname()}");
#endif
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

    public void UpdatePlayerList()
    {
        _playersToRemove.Clear();
        foreach ((var netId, var listPlayer) in _playersTracker)
        {
            var shouldRemove = listPlayer.ManualUpdate();
            if (shouldRemove)
            {
                _playersToRemove.Add(netId);
            }
        }

        foreach (var idToRemove in _playersToRemove)
        {
            _playersTracker.Remove(idToRemove);
        }
    }
}
