using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.HostClasses;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    internal class CorpsePositionSyncer : MonoBehaviour
    {
        private Corpse corpse;
        private RagdollPacketStruct data;
        private FikaHostWorld world;
        private int counter;

        public static void Create(GameObject gameObject, Corpse corpse, int netId)
        {
            CorpsePositionSyncer corpsePositionSyncer = gameObject.AddComponent<CorpsePositionSyncer>();
            corpsePositionSyncer.corpse = corpse;
            corpsePositionSyncer.world = (FikaHostWorld)Singleton<GameWorld>.Instance.World_0;
            corpsePositionSyncer.counter = 0;
            corpsePositionSyncer.data = new()
            {
                Id = netId
            };
        }

        public void Start()
        {
            if (corpse == null)
            {
                FikaPlugin.Instance.FikaLogger.LogError("CorpsePositionSyncer::Start: Corpse was null!");
                Destroy(this);
            }

            if (!corpse.HasRagdoll)
            {
                FikaPlugin.Instance.FikaLogger.LogError("CorpsePositionSyncer::Start: Ragdoll was null!");
                Destroy(this);
            }
        }

        public void FixedUpdate()
        {
            if (corpse.Ragdoll.Bool_2)
            {
                data.Position = corpse.TrackableTransform.position;
                data.TransformSyncs = corpse.TransformSyncs;
                data.Done = true;
                world.WorldPacket.RagdollPackets.Add(data);
                Destroy(this);
                return;
            }

            counter++;
            if (counter % 4 == 0)
            {
                counter = 0;
                data.Position = corpse.TrackableTransform.position;
                world.WorldPacket.RagdollPackets.Add(data);
            }
        }
    }
}
