using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;

namespace Fika.Core.Main.Components;

public class ItemPositionSyncer : MonoBehaviour
{
    private FikaServer _server;
    private FikaClient _client;
    private bool _isServer;
    private ObservedLootItem _lootItem;
    private LootSyncStruct _data;

    private Rigidbody Rigidbody
    {
        get
        {
            return _lootItem.RigidBody;
        }
    }

    public static void Create(GameObject gameObject, bool isServer, ObservedLootItem lootItem)
    {
        ItemPositionSyncer posSync = gameObject.AddComponent<ItemPositionSyncer>();
        posSync._isServer = isServer;
        if (isServer)
        {
            posSync._server = Singleton<FikaServer>.Instance;
        }
        else
        {
            posSync._client = Singleton<FikaClient>.Instance;
        }
        posSync._lootItem = lootItem;
        posSync._data = new()
        {
            Id = lootItem.GetNetId()
        };
    }

    public void Start()
    {
        if (_lootItem == null)
        {
            FikaGlobals.LogError("ItemPositionSyncer::Start: LootItem was null!");
            Destroy(this);
            return;
        }

        if (Rigidbody == null)
        {
            FikaGlobals.LogError("ItemPositionSyncer::Start: Rigidbody was null!");
            Destroy(this);
            return;
        }

        _data.Position = _lootItem.transform.position;
        _data.Rotation = _lootItem.transform.rotation;
        _data.Velocity = Rigidbody.velocity;
        _data.AngularVelocity = Rigidbody.angularVelocity;
        if (_isServer)
        {
            _server.FikaHostWorld.WorldPacket.LootSyncStructs.Add(_data);
            return;
        }

        _client.FikaClientWorld.WorldPacket.LootSyncStructs.Add(_data);
    }

    public void Update()
    {
        if (Rigidbody == null)
        {
            _data.Position = _lootItem.transform.position;
            _data.Rotation = _lootItem.transform.rotation;
            _data.Velocity = Vector3.zero;
            _data.AngularVelocity = Vector3.zero;
            _data.Done = true;
            if (_isServer)
            {
                _server.FikaHostWorld.WorldPacket.LootSyncStructs.Add(_data);
                Destroy(this);
                return;
            }

            _client.FikaClientWorld.WorldPacket.LootSyncStructs.Add(_data);
            Destroy(this);
            return;
        }
    }
}
