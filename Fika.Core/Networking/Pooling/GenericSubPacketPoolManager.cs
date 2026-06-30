using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;

namespace Fika.Core.Networking.Pooling;

internal sealed class GenericSubPacketPoolManager : BasePacketPoolManager<EGenericSubPacketType, IPoolSubPacket>
{
    public static GenericSubPacketPoolManager Instance { get; } = new();

    public static void Release()
    {
        Instance.ClearPool();
    }

    private GenericSubPacketPoolManager()
    {
        _subPacketFactories =
        [
            ClientExtract.CreateInstance,          // EGenericSubPacketType.ClientExtract = 0
            ClientConnected.CreateInstance,        // EGenericSubPacketType.ClientConnected = 1
            ClientDisconnected.CreateInstance,     // EGenericSubPacketType.ClientDisconnected = 2
            ExfilCountdown.CreateInstance,         // EGenericSubPacketType.ExfilCountdown = 3
            UpdateBackendData.CreateInstance,       // EGenericSubPacketType.UpdateBackendData = 4
            SecretExfilFound.CreateInstance,        // EGenericSubPacketType.SecretExfilFound = 5
            BorderZoneEvent.CreateInstance,         // EGenericSubPacketType.BorderZone = 6
            MineEvent.CreateInstance,                // EGenericSubPacketType.Mine = 7
            DisarmTripwire.CreateInstance,           // EGenericSubPacketType.DisarmTripwire = 8
            MuffledState.CreateInstance,              // EGenericSubPacketType.MuffledState = 9
            BtrSpawn.CreateInstance,                   // EGenericSubPacketType.SpawnBTR = 10
            CharacterSyncPacket.CreateInstance,        // EGenericSubPacketType.CharacterSync = 11
            InventoryPacket.CreateInstance,            // EGenericSubPacketType.InventoryOperation = 12
            OperationCallbackPacket.CreateInstance,     // EGenericSubPacketType.OperationCallback = 13
            PingPacket.CreateInstance,                  // EGenericSubPacketType.Ping = 14
            SendCharacterPacket.CreateInstance,         // EGenericSubPacketType.SendCharacterPacket = 15
            SyncableItemPacket.CreateInstance,          // EGenericSubPacketType.SyncableItem = 16
            SpawnAI.CreateInstance                     // EGenericSubPacketType.SyncableItem = 17
        ];
    }
}
