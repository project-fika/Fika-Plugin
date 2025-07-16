using Comfort.Common;
using Fika.Core.Networking.Packets.Communication;
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
        public bool IsLocal { get; set; }
        void SendData(ArraySegment<byte> data, bool reliable);
    }

    public class LocalPeer : IPeer
    {
        public bool IsLocal { get; set; } = true;

        public void SendData(ArraySegment<byte> data, bool reliable)
        {
            Singleton<FikaServer>.Instance.VOIPClient.NetworkReceivedPacket(data);
        }
    }

    public struct RemotePeer(NetPeer peer) : IPeer
    {
        public bool IsLocal { get; set; } = false;

        public readonly NetPeer Peer
        {
            get
            {
                return _peer;
            }
        }

        private readonly NetPeer _peer = peer;

        public void SendData(ArraySegment<byte> data, bool reliable)
        {
            if (reliable)
            {
                VOIPPacket packet = new()
                {
                    Data = data.Array
                };
                Singleton<IFikaNetworkManager>.Instance.SendVOIPPacket(ref packet, _peer);
            }
            else
            {
                Singleton<IFikaNetworkManager>.Instance.SendVOIPData(data, _peer);
            }
        }
    }
}
