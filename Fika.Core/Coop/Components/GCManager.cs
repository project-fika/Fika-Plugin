using UnityEngine;
using UnityEngine.Scripting;

namespace Fika.Core.Coop.Components
{
    public class GCManager : MonoBehaviour
    {
        private float counter;

        protected void Awake()
        {
            counter = 0f;
        }

        protected void Update()
        {
            counter += Time.deltaTime;
            if (counter > 10f)
            {
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogWarning("Running GC");
#endif
                counter = 0f;
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                GarbageCollector.CollectIncremental(10000000);
                GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
            }
        }
    }
}
