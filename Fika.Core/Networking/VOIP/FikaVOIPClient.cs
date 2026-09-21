using System;
using Comfort.Common;
using Dissonance.Integrations.MirrorIgnorance;
using Dissonance.Networking;
using Fika.Core.Main.Utils;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Networking.VOIP;

public class FikaVOIPClient : MirrorIgnoranceClient
{
    private static readonly Action<BaseClient<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>> _baseDisconnect =
        typeof(BaseClient<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>).GetMethod(nameof(Disconnect))
            .CreateBaseCall<Action<BaseClient<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>>>();

    private readonly FikaCommsNetwork _commsNet;

    public FikaVOIPClient(IntPtr pointer) : base(pointer)
    {
    }

    public FikaVOIPClient(FikaCommsNetwork network) : base(Il2CppInjection.Allocate<FikaVOIPClient>())
    {
        ClassInjector.DerivedConstructorBody(this);
        _commsNet = network;
        ClassInjector.InvokeBaseConstructor<MirrorIgnoranceClient>(this, network);
    }

    public override void Connect()
    {
        Connected();
    }

    public override void Disconnect()
    {
        _baseDisconnect(this);
    }

    public override void SendReliable(Il2CppSystem.ArraySegment<byte> packet)
    {
        if (_commsNet.PreprocessPacketToServer(packet))
        {
            return;
        }

        FikaGlobals.NetworkManager.SendVOIPData(packet.ToManaged(), DeliveryMethod.ReliableOrdered);
    }

    public override void SendUnreliable(Il2CppSystem.ArraySegment<byte> packet)
    {
        if (_commsNet.PreprocessPacketToServer(packet))
        {
            return;
        }

        FikaGlobals.NetworkManager.SendVOIPData(packet.ToManaged(), DeliveryMethod.Sequenced);
    }
}
