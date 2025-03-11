using Comfort.Common;
using LiteNetLib;
using System;

namespace Fika.Core.Networking.VOIP
{
    public readonly struct FikaVOIPPeer(IPeer connection) : IEquatable<FikaVOIPPeer>
    {
        public readonly IPeer Peer = connection;

        public override int GetHashCode()
        {
            return Peer.GetHashCode();
        }

        public override string ToString()
        {
            return Peer.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is FikaVOIPPeer peer && Equals(peer);
        }

        public bool Equals(FikaVOIPPeer other)
        {
            if (Peer == null)
            {
                return other.Peer == null;
            }

            if (Peer is RemotePeer localRemote && other.Peer is RemotePeer otherRemote)
            {
                return localRemote.Peer.Equals(otherRemote.Peer);
            }

            return Peer.Equals(other.Peer);
        }
    }

    public interface IPeer
    {
        void SendData(ArraySegment<byte> data, bool reliable);
    }

    public class LocalPeer : IPeer
    {
        public void SendData(ArraySegment<byte> data, bool reliable)
        {
            Singleton<FikaServer>.Instance.VOIPClient.NetworkReceivedPacket(data);
        }
    }

    public class RemotePeer(NetPeer peer) : IPeer
    {
        public NetPeer Peer
        {
            get
            {
                return peer;
            }
        }

        private readonly NetPeer peer = peer;

        public void SendData(ArraySegment<byte> data, bool reliable)
        {
            VOIPPacket packet = new()
            {
                Data = data.Array
            };
            Singleton<IFikaNetworkManager>.Instance.SendVOIPPacket(ref packet, reliable, peer);
        }
    }
}
