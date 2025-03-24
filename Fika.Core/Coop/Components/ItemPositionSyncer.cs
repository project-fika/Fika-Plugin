using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    public class ItemPositionSyncer : MonoBehaviour
    {
        private FikaServer server;
        private FikaClient client;
        private bool isServer;
        private ObservedLootItem lootItem;
        private LootSyncStruct data;
        private int counter;

        private Rigidbody Rigidbody
        {
            get
            {
                return lootItem.RigidBody;
            }
        }

        public static void Create(GameObject gameObject, bool isServer, ObservedLootItem lootItem)
        {
            ItemPositionSyncer posSync = gameObject.AddComponent<ItemPositionSyncer>();
            posSync.isServer = isServer;
            if (isServer)
            {
                posSync.server = Singleton<FikaServer>.Instance;
            }
            else
            {
                posSync.client = Singleton<FikaClient>.Instance;
            }
            posSync.lootItem = lootItem;
            posSync.counter = 0;
            posSync.data = new()
            {
                Id = lootItem.GetNetId()
            };
        }

        public void Start()
        {
            if (lootItem == null)
            {
                FikaGlobals.LogError("HostItemPositionSync::Start: LootItem was null!");
                Destroy(this);
            }

            if (Rigidbody == null)
            {
                FikaGlobals.LogError("HostItemPositionSync::Start: Rigidbody was null!");
                Destroy(this);
            }
        }

        public void FixedUpdate()
        {
            if (Rigidbody == null)
            {
                data.Position = lootItem.transform.position;
                data.Rotation = lootItem.transform.rotation;
                data.Velocity = Vector3.zero;
                data.AngularVelocity = Vector3.zero;
                data.Done = true;
                if (isServer)
                {
                    server.FikaHostWorld.WorldPacket.LootSyncStructs.Add(data);
                    Destroy(this);
                    return;
                }

                client.FikaClientWorld.WorldPacket.LootSyncStructs.Add(data);
                Destroy(this);
                return;
            }

            counter++;
            if (counter % 4 == 0)
            {
                counter = 0;
                data.Position = lootItem.transform.position;
                data.Rotation = lootItem.transform.rotation;
                data.Velocity = Rigidbody.velocity;
                data.AngularVelocity = Rigidbody.angularVelocity;
                if (isServer)
                {
                    server.FikaHostWorld.WorldPacket.LootSyncStructs.Add(data);
                    return;
                }

                client.FikaClientWorld.WorldPacket.LootSyncStructs.Add(data);
            }
        }
    }
}
