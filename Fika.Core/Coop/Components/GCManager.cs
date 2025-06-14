using UnityEngine;
using UnityEngine.Scripting;

namespace Fika.Core.Coop.Components
{
    public class GCManager : MonoBehaviour
    {
        private float _counter;
        private float _threshold;

        protected void Awake()
        {
            _counter = 0f;
            _threshold = 20f;
        }

        protected void Update()
        {
            _counter += Time.unscaledDeltaTime;
            if (_counter > _threshold)
            {
                _counter -= _threshold;
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                GarbageCollector.CollectIncremental(1000000);
                GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
            }
        }
    }
}
