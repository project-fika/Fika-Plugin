using System.Collections.Generic;
using Comfort.Common;
using Dissonance.Integrations.MirrorIgnorance;
using EFT.Network;

namespace Fika.Core.Networking.VOIP;

public static class FikaVOIPPeers
{
    private const int LOCAL_INDEX = -1;

    private static readonly Dictionary<int, NetPeer> _peers = [];
    private static readonly Dictionary<int, NetworkConnection> _connections = [];

    public static MirrorConn Local
    {
        get
        {
            return new MirrorConn(GetConnection(LOCAL_INDEX));
        }
    }

    public static MirrorConn ForPeer(NetPeer peer)
    {
        _peers[peer.Id] = peer;
        return new MirrorConn(GetConnection(peer.Id));
    }

    public static bool IsLocal(MirrorConn connection)
    {
        return connection.Connection != null && connection.Connection.ConnectionIndex == LOCAL_INDEX;
    }

    public static bool TryGetPeer(MirrorConn connection, out NetPeer peer)
    {
        peer = null;
        return connection.Connection != null && _peers.TryGetValue(connection.Connection.ConnectionIndex, out peer);
    }

    public static void UpdateConnectionStates()
    {
        foreach (var (id, peer) in _peers)
        {
            if (peer.ConnectionState != ConnectionState.Connected && _connections.TryGetValue(id, out var connection))
            {
                connection.IsConnected = false;
            }
        }
    }

    public static void SendToLocalClient(Il2CppSystem.ArraySegment<byte> data)
    {
        Singleton<FikaServer>.Instance.VOIPClient?.NetworkReceivedPacket(data);
    }

    public static void Clear()
    {
        _peers.Clear();
        _connections.Clear();
    }

    private static NetworkConnection GetConnection(int index)
    {
        if (!_connections.TryGetValue(index, out var connection))
        {
            connection = new NetworkConnection(0, index, "fika", 0)
            {
                IsConnected = true
            };
            _connections[index] = connection;
        }

        return connection;
    }
}
