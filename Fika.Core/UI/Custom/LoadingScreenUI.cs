using System.Collections.Generic;
using Comfort.Common;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Backend;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; internal set; }

    public GameObject LoadingPlayerPrefab;
    public Transform Stack;

    private Dictionary<int, LoadingScreenPlayer> _loadingPlayers;

    private void Awake()
    {
        _loadingPlayers = [];
    }

    public LoadingScreenPlayersPacket GetPlayersPacket()
    {
        var count = _loadingPlayers.Count;
        var netIds = new int[count];
        var nicknames = new string[count];

        var index = 0;
        foreach ((var netId, var loadingPlayer) in _loadingPlayers)
        {
            netIds[index] = netId;
            nicknames[index] = loadingPlayer.Nickname.text;
            index++;
        }

        return new LoadingScreenPlayersPacket
        {
            NetIds = netIds,
            Nicknames = nicknames
        };
    }

    public LoadingScreenPacket[] GetLatestStates()
    {
        var packets = new LoadingScreenPacket[_loadingPlayers.Count];

        var index = 0;
        foreach ((var netId, var loadingPlayer) in _loadingPlayers)
        {
            packets[index] = new LoadingScreenPacket
            {
                NetId = netId,
                Progress = loadingPlayer.Progress
            };
            index++;
        }

        return packets;
    }

    public void UpdateAndBroadcast(float progress)
    {
        var netId = Singleton<IFikaNetworkManager>.Instance.NetId;
        var loadingPacket = new LoadingScreenPacket
        {
            NetId = netId,
            Progress = progress
        };
        Singleton<IFikaNetworkManager>.Instance.SendData(ref loadingPacket, DeliveryMethod.Unreliable, true);
        SetProgress(netId, progress);
    }

    public void SetProgress(int netId, float progress)
    {
        if (_loadingPlayers.TryGetValue(netId, out var player))
        {
            player.SetProgress(progress);
        }
    }

    public void AddPlayer(int netId, string nickname)
    {
        if (_loadingPlayers.ContainsKey(netId))
        {
            return;
        }

        var go = GameObject.Instantiate(LoadingPlayerPrefab, Stack);
        var lsp = go.GetComponent<LoadingScreenPlayer>();
        lsp.SetNickname(nickname);
        _loadingPlayers.Add(netId, lsp);
        go.SetActive(true);
    }

    public void DeletePlayer(int netId)
    {
        if (_loadingPlayers.TryGetValue(netId, out var lsp))
        {
            _loadingPlayers.Remove(netId);
            GameObject.Destroy(lsp.gameObject);
        }
    }

    private void OnDestroy()
    {
        foreach (var item in _loadingPlayers)
        {
            GameObject.Destroy(item.Value.gameObject);
        }

        _loadingPlayers.Clear();
    }
}
