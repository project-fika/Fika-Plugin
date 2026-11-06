using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.World;
using System;

namespace Fika.Core.Main.Components;

internal class CorpsePositionSyncer : MonoBehaviour
{
    private const float SendInterval = 1f / 30f;

    public CorpsePositionSyncer(IntPtr pointer) : base(pointer)
    {
    }

    private Corpse _corpse;
    private CorpseSyncPacket _data;
    private FikaServer _server;
    private float _elapsed;

    public static void Create(GameObject gameObject, Corpse corpse, int netId)
    {
        var corpsePositionSyncer = gameObject.AddComponent<CorpsePositionSyncer>();
        corpsePositionSyncer._corpse = corpse;
        corpsePositionSyncer._server = Singleton<FikaServer>.Instance;
        corpsePositionSyncer._elapsed = SendInterval;
        corpsePositionSyncer._data = new()
        {
            Id = netId
        };
    }

    public void Start()
    {
        if (_corpse == null)
        {
            FikaGlobals.LogError("CorpsePositionSyncer::Start: Corpse was null!");
            Destroy(this);
            return;
        }

        if (!_corpse.HasRagdoll)
        {
            FikaGlobals.LogError("CorpsePositionSyncer::Start: Ragdoll was null!");
            Destroy(this);
        }
    }

    public void FixedUpdate()
    {
        if (_corpse == null || !_corpse.HasRagdoll)
        {
            return;
        }

        var done = _corpse.Ragdoll._isPhysicsDone;
        _elapsed += Time.fixedDeltaTime;
        if (!done && _elapsed < SendInterval)
        {
            return;
        }

        _elapsed -= SendInterval;

        try
        {
            Send(done);
        }
        catch (Exception ex)
        {
            FikaGlobals.LogError($"CorpsePositionSyncer::FixedUpdate: {ex.Message}");
            Destroy(this);
            return;
        }

        if (done)
        {
            Destroy(this);
        }
    }

    private void Send(bool done)
    {
        CorpseTransformSyncs transforms = new();
        _corpse.CaptureWorldTransformSyncs(ref transforms);

        _data.Position = _corpse.TrackableTransform.position;
        _data.TransformSyncs = transforms;
        _data.Done = done;

        CorpsePositionPacket packet = new()
        {
            Data = _data,
            RemoteTime = Time.time
        };

        _server.SendData(ref packet, done ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);
    }
}
