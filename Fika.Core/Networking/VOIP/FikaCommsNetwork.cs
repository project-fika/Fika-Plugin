using Comfort.Common;
using Dissonance;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking;
using Fika.Core.Main.Utils;
using System;
using System.Collections.Generic;

namespace Fika.Core.Networking.VOIP
{
    public class FikaCommsNetwork : BaseCommsNetwork<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer, Unit, Unit>
    {
        private readonly ConcurrentPool<byte[]> _loopbackBuffers = new(8, () => new byte[1024]);
        private readonly List<ArraySegment<byte>> _loopbackQueue = [];

        protected override FikaVOIPClient CreateClient(Unit connectionParameters)
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

        protected override FikaVOIPServer CreateServer(Unit connectionParameters)
        {
            FikaVOIPServer server = new(this);
            Singleton<FikaServer>.Instance.VOIPServer = server;
            return server;
        }

        public bool PreprocessPacketToClient(ArraySegment<byte> packet, FikaVOIPPeer peer)
        {
            if (Server == null)
            {
                FikaGlobals.LogError("Server packet processing running, but this peer is not a server");
                return true;
            }
            if (Client == null)
            {
                return false;
            }
            if (!peer.Peer.IsLocal)
            {
                return false;
            }
            if (Client != null)
            {
                _loopbackQueue.Add(packet.CopyToSegment(_loopbackBuffers.Get(), 0));
            }
            return true;
        }

        public bool PreprocessPacketToServer(ArraySegment<byte> packet)
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
            Server.NetworkReceivedPacket(new(new LocalPeer()), packet);
            return true;
        }

        protected override void Update()
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
                _loopbackQueue.Clear();
            }
            for (int i = 0; i < _loopbackQueue.Count; i++)
            {
                ArraySegment<byte> segment = _loopbackQueue[i];
                if (Client != null)
                {
                    Client.NetworkReceivedPacket(segment);
                }
                _loopbackBuffers.Put(segment.Array);
            }
            base.Update();
        }
    }
}
