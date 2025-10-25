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
            ClearEffects.CreateInstance,            // EGenericSubPacketType.ClearEffects = 4
            UpdateBackendData.CreateInstance,       // EGenericSubPacketType.UpdateBackendData = 5
            SecretExfilFound.CreateInstance,        // EGenericSubPacketType.SecretExfilFound = 6
            BorderZoneEvent.CreateInstance,         // EGenericSubPacketType.BorderZone = 7
            MineEvent.CreateInstance,                // EGenericSubPacketType.Mine = 8
            DisarmTripwire.CreateInstance,           // EGenericSubPacketType.DisarmTripwire = 9
            MuffledState.CreateInstance,              // EGenericSubPacketType.MuffledState = 10
            BtrSpawn.CreateInstance,                   // EGenericSubPacketType.SpawnBTR = 11
            CharacterSyncPacket.CreateInstance,        // EGenericSubPacketType.CharacterSync = 12
            InventoryPacket.CreateInstance,            // EGenericSubPacketType.InventoryOperation = 13
            OperationCallbackPacket.CreateInstance,     // EGenericSubPacketType.OperationCallback = 14
            PingPacket.CreateInstance,                  // EGenericSubPacketType.Ping = 15
            SendCharacterPacket.CreateInstance,         // EGenericSubPacketType.SendCharacterPacket = 16
            SyncableItemPacket.CreateInstance,          // EGenericSubPacketType.SyncableItem = 17
            SpawnAI.CreateInstance,                     // EGenericSubPacketType.SyncableItem = 18
        ];
    }
}
