using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.HostClasses;

namespace Fika.Core.Main.Components;

internal class CorpsePositionSyncer : MonoBehaviour
{
    private Corpse _corpse;
    private RagdollPacketStruct _data;
    private FikaHostWorld _world;
    private int _counter;

    public static void Create(GameObject gameObject, Corpse corpse, int netId)
    {
        CorpsePositionSyncer corpsePositionSyncer = gameObject.AddComponent<CorpsePositionSyncer>();
        corpsePositionSyncer._corpse = corpse;
        corpsePositionSyncer._world = (FikaHostWorld)Singleton<GameWorld>.Instance.World_0;
        corpsePositionSyncer._counter = 0;
        corpsePositionSyncer._data = new()
        {
            Id = netId
        };
    }

    public void Start()
    {
        if (_corpse == null)
        {
            FikaPlugin.Instance.FikaLogger.LogError("CorpsePositionSyncer::Start: Corpse was null!");
            Destroy(this);
            return;
        }

        if (!_corpse.HasRagdoll)
        {
            FikaPlugin.Instance.FikaLogger.LogError("CorpsePositionSyncer::Start: Ragdoll was null!");
            Destroy(this);
            return;
        }
    }

    public void FixedUpdate()
    {
        if (_corpse.Ragdoll.Bool_2)
        {
            _data.Position = _corpse.TrackableTransform.position;
            _data.TransformSyncs = _corpse.TransformSyncs;
            _data.Done = true;
            _world.WorldPacket.RagdollPackets.Add(_data);
            Destroy(this);
            return;
        }

        _counter++;
        if (_counter % 4 == 0)
        {
            _counter = 0;
            _data.Position = _corpse.TrackableTransform.position;
            _world.WorldPacket.RagdollPackets.Add(_data);
        }
    }
}
