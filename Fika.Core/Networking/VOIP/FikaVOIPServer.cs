using Comfort.Common;
using Dissonance.Networking;
using Dissonance.Networking.Server;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Communication;
using System;
using System.Collections.Generic;

namespace Fika.Core.Networking.VOIP;

public class FikaVOIPServer(FikaCommsNetwork commsNetwork) : BaseServer<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer>
{
    private readonly List<NetPeer> _peers = [];
    private readonly FikaCommsNetwork _fikaComms = commsNetwork;

    protected override void AddClient(ClientInfo<FikaVOIPPeer> client)
    {
        base.AddClient(client);
        if (client.PlayerName != _fikaComms.PlayerName)
        {
            if (client.Connection.Peer is RemotePeer peer)
            {
                _peers.Add(peer.Peer);
                return;
            }
            FikaGlobals.LogError($"FikaVOIPServer::AddClient: Connection.Peer was not a RemotePeer!");
        }
    }

    public override ServerState Update()
    {
        for (int i = _peers.Count - 1; i >= 0; i--)
        {
            if (_peers[i].ConnectionState != ConnectionState.Connected)
            {
                NetPeer peer = _peers[i];
                FikaGlobals.LogInfo($"FikaVOIPServer::Update: Peer {peer} disconnected from VOIP service");
                ClientDisconnected(new(new RemotePeer(peer)));
                _peers.RemoveAt(i);
            }
        }
        return base.Update();
    }

    protected override void ReadMessages()
    {

    }

    public override void Disconnect()
    {
        Singleton<IFikaNetworkManager>.Instance.RegisterPacket<VOIPPacket>(OnVoicePacketReceived);
        base.Disconnect();
    }

    private void OnVoicePacketReceived(VOIPPacket packet)
    {
        // Do nothing
    }

    protected override void SendReliable(FikaVOIPPeer connection, ArraySegment<byte> packet)
    {
        if (packet.Array != null && packet.Array.Length == 0)
        {
            FikaGlobals.LogError("Packet length was 0!");
            return;
        }

        connection.Peer.SendData(packet, DeliveryMethod.ReliableOrdered);
    }

    protected override void SendUnreliable(FikaVOIPPeer connection, ArraySegment<byte> packet)
    {
        if (packet.Array != null && packet.Array.Length == 0)
        {
            FikaGlobals.LogError("Packet length was 0!");
            return;
        }

        connection.Peer.SendData(packet, DeliveryMethod.Sequenced);
    }

    public override void SendReliable(List<FikaVOIPPeer> connections, ArraySegment<byte> packet)
    {
        if (connections == null)
        {
            throw new ArgumentNullException("connections");
        }

        for (int i = 0; i < connections.Count; i++)
        {
            SendReliable(connections[i], packet);
        }
    }

    public override void SendUnreliable(List<FikaVOIPPeer> connections, ArraySegment<byte> packet)
    {
        if (connections == null)
        {
            throw new ArgumentNullException("connections");
        }

        for (int i = 0; i < connections.Count; i++)
        {
            SendUnreliable(connections[i], packet);
        }
    }
}
