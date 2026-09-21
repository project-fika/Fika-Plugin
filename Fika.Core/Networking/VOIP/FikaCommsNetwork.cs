using System;
using Comfort.Common;
using Dissonance;
using Dissonance.Integrations.MirrorIgnorance;
using Dissonance.Networking;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.VOIP;

public class FikaCommsNetwork : MirrorIgnoranceCommsNetwork
{
    private static readonly Action<BaseCommsNetwork<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn, Unit, Unit>> _baseUpdate =
        typeof(BaseCommsNetwork<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn, Unit, Unit>).GetMethod(nameof(Update))
            .CreateBaseCall<Action<BaseCommsNetwork<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn, Unit, Unit>>>();

    public FikaCommsNetwork(IntPtr pointer) : base(pointer)
    {
    }

    public override MirrorIgnoranceClient CreateClient(Unit connectionParameters)
    {
        FikaVOIPClient client = new(this);
        if (FikaBackendUtils.IsClient)
        {
            Singleton<FikaClient>.Instance.VOIPClient = client;
        }
        else
        {
            Singleton<FikaServer>.Instance.VOIPClient = client;
        }
        return client;
    }

    public override MirrorIgnoranceServer CreateServer(Unit connectionParameters)
    {
        FikaVOIPServer server = new(this);
        Singleton<FikaServer>.Instance.VOIPServer = server;
        return server;
    }

    public override void Initialize()
    {
    }

    // Host loopback, the local client's packets reach the server without the network
    public bool PreprocessPacketToServer(Il2CppSystem.ArraySegment<byte> packet)
    {
        if (Client == null)
        {
            FikaGlobals.LogError("Client packet processing running, but this peer is not a client");
            return true;
        }
        if (Server == null)
        {
            return false;
        }
        Server.NetworkReceivedPacket(FikaVOIPPeers.Local, packet);
        return true;
    }

    public override void Update()
    {
        if (IsInitialized)
        {
            if (FikaBackendUtils.IsClient && !Mode.IsClientEnabled())
            {
                RunAsClient(Unit.None);
            }
            else if (FikaBackendUtils.IsServer && !Mode.IsServerEnabled())
            {
                if (FikaBackendUtils.IsHeadless)
                {
                    RunAsDedicatedServer(Unit.None);
                }
                else
                {
                    RunAsHost(Unit.None, Unit.None);
                }
            }
        }
        else if (Mode != NetworkMode.None)
        {
            Stop();
        }

        _baseUpdate(this);
    }
}
