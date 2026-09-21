using System;
using Comfort.Common;
using Dissonance.Integrations.MirrorIgnorance;
using Dissonance.Networking;
using Dissonance.Networking.Server;
using Fika.Core.Main.Utils;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Networking.VOIP;

public class FikaVOIPServer : MirrorIgnoranceServer
{
    private static readonly Action<BaseServer<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>> _baseConnect =
        typeof(BaseServer<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>).GetMethod(nameof(Connect))
            .CreateBaseCall<Action<BaseServer<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>>>();

    private static readonly Action<BaseServer<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>> _baseDisconnect =
        typeof(BaseServer<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>).GetMethod(nameof(Disconnect))
            .CreateBaseCall<Action<BaseServer<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>>>();

    public FikaVOIPServer(IntPtr pointer) : base(pointer)
    {
    }

    public FikaVOIPServer(FikaCommsNetwork commsNetwork) : base(Il2CppInjection.Allocate<FikaVOIPServer>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<MirrorIgnoranceServer>(this, commsNetwork);
    }

    public override void Connect()
    {
        _baseConnect(this);
    }

    public override void Disconnect()
    {
        _baseDisconnect(this);
        FikaVOIPPeers.Clear();
    }

    public override ServerState Update()
    {
        FikaVOIPPeers.UpdateConnectionStates();
        return base.Update();
    }

    public override void SendReliable(MirrorConn connection, Il2CppSystem.ArraySegment<byte> packet)
    {
        Send(connection, packet, DeliveryMethod.ReliableOrdered);
    }

    public override void SendUnreliable(MirrorConn connection, Il2CppSystem.ArraySegment<byte> packet)
    {
        Send(connection, packet, DeliveryMethod.Sequenced);
    }

    private static void Send(MirrorConn connection, Il2CppSystem.ArraySegment<byte> packet, DeliveryMethod deliveryMethod)
    {
        if (packet.Count == 0)
        {
            FikaGlobals.LogError("Packet length was 0!");
            return;
        }

        if (FikaVOIPPeers.IsLocal(connection))
        {
            FikaVOIPPeers.SendToLocalClient(packet);
            return;
        }

        if (FikaVOIPPeers.TryGetPeer(connection, out var peer))
        {
            FikaGlobals.NetworkManager.SendVOIPData(packet.ToManaged(), deliveryMethod, peer);
            return;
        }

        FikaGlobals.LogError("FikaVOIPServer: no peer for this connection");
    }
}
