using Comfort.Common;
using System;

namespace Fika.Core.Networking.VOIP;

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
    void SendData(ArraySegment<byte> data, DeliveryMethod deliveryMethod);
}

public class LocalPeer : IPeer
{
    public bool IsLocal { get; set; } = true;

    public void SendData(ArraySegment<byte> data, DeliveryMethod deliveryMethod)
    {
        Singleton<FikaServer>.Instance.VOIPClient.NetworkReceivedPacket(data);
    }
}

public struct RemotePeer(NetPeer peer) : IPeer
{
    public bool IsLocal { get; set; }

    public readonly NetPeer Peer { get; } = peer;

    public readonly void SendData(ArraySegment<byte> data, DeliveryMethod deliveryMethod)
    {
        Singleton<IFikaNetworkManager>.Instance.SendVOIPData(data, deliveryMethod, Peer);
    }
}
