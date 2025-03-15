using UnityEngine;
using UnityEngine.Scripting;

namespace Fika.Core.Coop.Components
{
    public class GCManager : MonoBehaviour
    {
        private float counter;
        private float threshold;

        protected void Awake()
        {
            counter = 0f;
            threshold = 20f;
        }

        protected void Update()
        {
            counter += Time.deltaTime;
            if (counter > threshold)
            {
                counter = 0f;
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                GarbageCollector.CollectIncremental(10000000);
                GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
            }
        }
    }
}
