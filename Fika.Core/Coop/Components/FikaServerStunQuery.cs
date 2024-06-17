using System.Collections;
using UnityEngine;
using Fika.Core.Networking.NatPunch;
using Comfort.Common;
using Fika.Core.Networking;

namespace Fika.Core.Coop.Components
{
    public class FikaServerStunQuery : MonoBehaviour
    {
        private Coroutine stunQueryRoutine;

        public void StartServerStunQueryRoutine()
        {
            stunQueryRoutine = StartCoroutine(StunQuery());
        }

        public void StopServerStunQueryRoutine()
        {
            if (stunQueryRoutine != null)
            {
                StopCoroutine(stunQueryRoutine);
                stunQueryRoutine = null;
            }
        }

        private IEnumerator StunQuery()
        {
            while (true)
            {
                var fikaServer = Singleton<FikaServer>.Instance;

                while (fikaServer == null || fikaServer.NetServer == null || fikaServer.NatPunchServer == null)
                    yield return null;

                fikaServer.NetServer.Stop();

                var localPort = 0;
                var currentStunIpEndPoint = fikaServer.NatPunchServer.StunIpEndpoint;

                if (currentStunIpEndPoint != null)
                    localPort = currentStunIpEndPoint.Local.Port;

                var stunIpEndPoint = NatPunchUtils.CreateStunEndPoint(localPort);

                fikaServer.NatPunchServer.StunIpEndpoint = stunIpEndPoint;

                fikaServer.NetServer.Start(stunIpEndPoint.Local.Port);

                yield return new WaitForSeconds(60);
            }
        }

        private void OnDestroy()
        {
            StopServerStunQueryRoutine();
        }
    }
}
