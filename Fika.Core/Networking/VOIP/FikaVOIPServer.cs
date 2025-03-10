using Dissonance;
using Dissonance.Networking;
using Dissonance.Networking.Server;
using Fika.Core.Coop.Utils;
using LiteNetLib;
using System;
using System.Collections.Generic;

namespace Fika.Core.Networking.VOIP
{
    class FikaVOIPServer : BaseServer<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer>
    {
        private readonly List<NetPeer> peers;

        public FikaVOIPServer(FikaCommsNetwork commsNetwork)
        {
            peers = [];
        }

        protected override void AddClient(ClientInfo<FikaVOIPPeer> client)
        {
            base.AddClient(client);
            if (DissonanceComms.Instance != null)
            {
                if (client.PlayerName != DissonanceComms.Instance.LocalPlayerName)
                {
                    if (client.Connection.Peer is RemotePeer peer)
                    {
                        peers.Add(peer.Peer);
                        return; 
                    }
                    FikaGlobals.LogError($"FikaVOIPServer::AddClient: Connection.Peer was not a RemotePeer!");
                }                
            }

            FikaGlobals.LogError($"FikaVOIPServer::AddClient: DissonanceComms.Instance was null when attempting to add a client!");
        }

        public override ServerState Update()
        {
            for (int i = peers.Count - 1; i >= 0; i--)
            {
                if (peers[i].ConnectionState != ConnectionState.Connected)
                {
                    ClientDisconnected(new(new RemotePeer(peers[i])));
                    peers.RemoveAt(i);
                }
            }
            return base.Update();
        }

        protected override void ReadMessages()
        {

        }

        protected override void SendReliable(FikaVOIPPeer connection, ArraySegment<byte> packet)
        {
            if (packet.Array != null && packet.Array.Length == 0)
            {
                FikaGlobals.LogError("Packet length was 0!");
                return;
            }

            connection.Peer.SendData(packet, true);
        }

        protected override void SendUnreliable(FikaVOIPPeer connection, ArraySegment<byte> packet)
        {
            if (packet.Array != null && packet.Array.Length == 0)
            {
                FikaGlobals.LogError("Packet length was 0!");
                return;
            }

            connection.Peer.SendData(packet, false);
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
}
