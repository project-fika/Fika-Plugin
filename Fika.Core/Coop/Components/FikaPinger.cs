using Fika.Core.Networking.Http;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    /// <summary>
    /// Used to ping the backend every 30 seconds to keep the session alive
    /// </summary>
    public class FikaPinger : MonoBehaviour
    {
        private Coroutine _pingRoutine;

        public void StartPingRoutine()
        {
            _pingRoutine = StartCoroutine(PingServer());
        }

        public void StopPingRoutine()
        {
            if (_pingRoutine != null)
            {
                StopCoroutine(_pingRoutine);
                _pingRoutine = null;
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
