using Comfort.Common;
using Dissonance;
using Dissonance.Networking;
using Fika.Core.Coop.Utils;

namespace Fika.Core.Networking.VOIP
{
    class FikaCommsNetwork : BaseCommsNetwork<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer, Unit, Unit>
    {
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
            base.Update();
        }
    }
}
