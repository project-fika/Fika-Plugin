using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using System;
using System.Threading;

namespace Fika.Core.Networking.Pooling;

internal sealed class CommonSubPacketPoolManager : BasePacketPoolManager<ECommonSubPacketType, IPoolSubPacket>
{
    private static readonly Lazy<CommonSubPacketPoolManager> _instance = new(() => new CommonSubPacketPoolManager(), LazyThreadSafetyMode.None);
    public static CommonSubPacketPoolManager Instance
    {
        get
        {
            return _instance.Value;
        }
    }

    public static void Release()
    {
        _instance.Value.ClearPool();
    }

    private CommonSubPacketPoolManager()
    {
        _subPacketFactories =
        [
            PhrasePacket.CreateInstance,           // ECommonSubPacketType.Phrase = 0
            WorldInteractionPacket.CreateInstance, // ECommonSubPacketType.WorldInteraction = 1
            ContainerInteractionPacket.CreateInstance, // ECommonSubPacketType.ContainerInteraction = 2
            ProceedPacket.CreateInstance,           // ECommonSubPacketType.Proceed = 3
            HeadLightsPacket.CreateInstance,        // ECommonSubPacketType.HeadLights = 4
            InventoryChangedPacket.CreateInstance,  // ECommonSubPacketType.InventoryChanged = 5
            DropPacket.CreateInstance,               // ECommonSubPacketType.Drop = 6
            StationaryPacket.CreateInstance,         // ECommonSubPacketType.Stationary = 7
            VaultPacket.CreateInstance,               // ECommonSubPacketType.Vault = 8
            InteractionPacket.CreateInstance,         // ECommonSubPacketType.Interaction = 9
            MountingPacket.CreateInstance,            // ECommonSubPacketType.Mounting = 10
            DamagePacket.CreateInstance,              // ECommonSubPacketType.Damage = 11
            ArmorDamagePacket.CreateInstance,         // ECommonSubPacketType.ArmorDamage = 12
            HealthSyncPacket.CreateInstance,          // ECommonSubPacketType.HealthSync = 13
            UsableItemPacket.CreateInstance           // ECommonSubPacketType.UsableItem = 14
        ];
    }
}
