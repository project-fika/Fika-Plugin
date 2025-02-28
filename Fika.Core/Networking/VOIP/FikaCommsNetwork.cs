using Comfort.Common;
using Dissonance;
using Dissonance.Datastructures;
using Dissonance.Networking;
using Fika.Core.Coop.Utils;
using System;
using System.Collections.Generic;

namespace Fika.Core.Networking.VOIP
{
    // DissonanceSetupScene
    // Class1599.TranslateCommand() -> voipController.ToggleTalk();
    class FikaCommsNetwork : BaseCommsNetwork<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer, Unit, Unit>
    {
        private readonly List<ArraySegment<byte>> _loopbackQueue = [];
        private readonly ConcurrentPool<byte[]> _loopbackBuffers = new(8, () => new byte[1024]);

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
                    RunAsHost(Unit.None, Unit.None);
                }
            }
            else if (Mode != NetworkMode.None)
            {
                Stop();
                _loopbackQueue.Clear();
            }

            for (int i = 0; i < _loopbackQueue.Count; i++)
            {
                FikaVOIPClient client = Client;
                client?.NetworkReceivedPacket(_loopbackQueue[i]);
                _loopbackBuffers.Put(_loopbackQueue[i].Array);
            }
            _loopbackQueue.Clear();
            base.Update();
        }
    }
}
