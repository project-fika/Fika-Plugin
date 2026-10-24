using System.Collections.Generic;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.UI.Custom;
using System;
using Il2CppInterop.Runtime.Injection;

public class LoadingScreenUI : MonoBehaviour
{
    public LoadingScreenUI(IntPtr pointer) : base(pointer)
    {
    }

    public static LoadingScreenUI Instance { get; internal set; }

    [PrefabPath("Canvas/Stack/LoadingPlayer")]
    public GameObject LoadingPlayerPrefab;

    public Transform Stack;

    private Dictionary<int, LoadingScreenPlayer> _loadingPlayers;

    protected void Awake()
    {
        _loadingPlayers = [];
    }

    /// <summary>
    /// Attaches and wires the component on an instantiated prefab
    /// </summary>
    /// <param name="root">The instantiated prefab</param>
    public static LoadingScreenUI Create(GameObject root)
    {
        var ui = PrefabWiring.Attach<LoadingScreenUI>(root);
        if (ui == null)
        {
            FikaGlobals.LogError("Could not wire up the LoadingScreenUI!");
            GameObject.Destroy(root);
        }

        return ui;
    }

    public void ReInitAfterTransit()
    {
        FikaGlobals.LogInfo("Restarting LoadingScreenUI");
        var netId = FikaGlobals.NetworkManager.NetId;
        var pmcName = FikaBackendUtils.IsHeadless ? "Headless" : FikaBackendUtils.PMCName;

        AddPlayer(netId, pmcName);

        var loadingPacket = new LoadingScreenPlayersPacket
        {
            NetIds = [netId],
            Nicknames = [pmcName]
        };

        FikaGlobals.NetworkManager.SendData(ref loadingPacket, DeliveryMethod.ReliableUnordered, true);
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
        var netId = FikaGlobals.NetworkManager.NetId;
        var loadingPacket = new LoadingScreenPacket
        {
            NetId = netId,
            Progress = progress
        };
        FikaGlobals.NetworkManager.SendData(ref loadingPacket, DeliveryMethod.Unreliable, true);
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
        var lsp = PrefabWiring.Attach<LoadingScreenPlayer>(go);
        if (lsp == null)
        {
            FikaGlobals.LogError("Could not wire up the LoadingScreenPlayer!");
            GameObject.Destroy(go);
            return;
        }

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

    protected void OnDestroy()
    {
        ClearData();
    }

    public void ClearData()
    {
        foreach (var item in _loadingPlayers)
        {
            GameObject.Destroy(item.Value.gameObject);
        }

        _loadingPlayers.Clear();
    }
}
