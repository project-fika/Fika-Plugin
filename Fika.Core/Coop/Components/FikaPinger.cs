using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    public class FikaPinger : MonoBehaviour
    {
        private Coroutine pingRoutine;

        public void StartPingRoutine()
        {
            pingRoutine = StartCoroutine(PingServer());
        }

        public void StopPingRoutine()
        {
            if (pingRoutine != null)
            {
                StopCoroutine(pingRoutine);
                pingRoutine = null;
            }
        }

        private IEnumerator PingServer()
        {
            PingRequest pingRequest = new();

            while (true)
            {
                Task pingTask = FikaRequestHandler.UpdatePing(pingRequest);
                while (!pingTask.IsCompleted)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(30);
            }
        }

        private void OnDestroy()
        {
            StopPingRoutine();
        }
    }
}
