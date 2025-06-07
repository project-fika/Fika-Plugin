using UnityEngine;
using UnityEngine.Scripting;

namespace Fika.Core.Coop.Components
{
    public class GCManager : MonoBehaviour
    {
        private float _counter;
        private float threshold;

        public GCManager(float threshold)
        {
            this.threshold = threshold;
        }

        protected void Awake()
        {
            _counter = 0f;
            threshold = 20f;
        }

        protected void Update()
        {
            _counter += Time.deltaTime;
            if (_counter > threshold)
            {
                _counter = 0f;
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                GarbageCollector.CollectIncremental(1000000);
                GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
            }
        }
    }
}
