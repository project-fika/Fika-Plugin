using System.Runtime.InteropServices;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;

namespace Fika.Core.Networking.Packets.Player.Common;

public struct CommonPlayerPacket : INetSerializable
{
    public int NetId;
    public ECommonSubPacketType Type;
    public CommonSubPacketData SubPacket;

    public readonly void Execute(FikaPlayer player = null)
    {
        SubPacket.Execute(Type, player);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Type = reader.GetEnum<ECommonSubPacketType>();

        switch (Type)
        {
            case ECommonSubPacketType.Phrase:
                SubPacket.Phrase = reader.GetUnmanaged<PhrasePacket>();
                break;
            case ECommonSubPacketType.WorldInteraction:
                SubPacket.WorldInteraction = new WorldInteractionPacket(reader);
                break;
            case ECommonSubPacketType.ContainerInteraction:
                SubPacket.ContainerInteraction = new ContainerInteractionPacket(reader);
                break;
            case ECommonSubPacketType.Proceed:
                SubPacket.Proceed = new ProceedPacket(reader);
                break;
            case ECommonSubPacketType.HeadLights:
                SubPacket.HeadLights = new HeadLightsPacket(reader);
                break;
            case ECommonSubPacketType.InventoryChanged:
                SubPacket.InventoryChanged = reader.GetUnmanaged<InventoryChangedPacket>();
                break;
            case ECommonSubPacketType.Drop:
                SubPacket.Drop = reader.GetUnmanaged<DropPacket>();
                break;
            case ECommonSubPacketType.Stationary:
                SubPacket.Stationary = new StationaryPacket(reader);
                break;
            case ECommonSubPacketType.Vault:
                SubPacket.Vault = reader.GetUnmanaged<VaultPacket>();
                break;
            case ECommonSubPacketType.Interaction:
                SubPacket.Interaction = reader.GetUnmanaged<InteractionPacket>();
                break;
            case ECommonSubPacketType.Mounting:
                SubPacket.Mounting = new MountingPacket(reader);
                break;
            case ECommonSubPacketType.Damage:
                SubPacket.Damage = new DamagePacket(reader);
                break;
            case ECommonSubPacketType.ArmorDamage:
                SubPacket.ArmorDamage = new ArmorDamagePacket(reader);
                break;
            case ECommonSubPacketType.HealthSync:
                SubPacket.HealthSync = new HealthSyncPacket(reader);
                break;
            case ECommonSubPacketType.UsableItem:
                SubPacket.UsableItem = new UsableItemPacket(reader);
                break;
            case ECommonSubPacketType.DownedSync:
                SubPacket.DownedSync = reader.GetUnmanaged<DownedSyncPacket>();
                break;
            case ECommonSubPacketType.RevivedPlayer:
                SubPacket.RevivedPlayer = new RevivedPlayerPacket();
                break;
            case ECommonSubPacketType.RevivingPlayer:
                SubPacket.RevivingPlayer = new RevivingPlayerPacket(reader);
                break;
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutEnum(Type);

        switch (Type)
        {
            case ECommonSubPacketType.Phrase:
                writer.PutUnmanaged(SubPacket.Phrase);
                break;
            case ECommonSubPacketType.WorldInteraction:
                SubPacket.WorldInteraction.Serialize(writer);
                break;
            case ECommonSubPacketType.ContainerInteraction:
                SubPacket.ContainerInteraction.Serialize(writer);
                break;
            case ECommonSubPacketType.Proceed:
                SubPacket.Proceed.Serialize(writer);
                break;
            case ECommonSubPacketType.HeadLights:
                SubPacket.HeadLights.Serialize(writer);
                break;
            case ECommonSubPacketType.InventoryChanged:
                writer.PutUnmanaged(SubPacket.InventoryChanged);
                break;
            case ECommonSubPacketType.Drop:
                writer.PutUnmanaged(SubPacket.Drop);
                break;
            case ECommonSubPacketType.Stationary:
                SubPacket.Stationary.Serialize(writer);
                break;
            case ECommonSubPacketType.Vault:
                writer.PutUnmanaged(SubPacket.Vault);
                break;
            case ECommonSubPacketType.Interaction:
                writer.PutUnmanaged(SubPacket.Interaction);
                break;
            case ECommonSubPacketType.Mounting:
                SubPacket.Mounting.Serialize(writer);
                break;
            case ECommonSubPacketType.Damage:
                SubPacket.Damage.Serialize(writer);
                break;
            case ECommonSubPacketType.ArmorDamage:
                SubPacket.ArmorDamage.Serialize(writer);
                break;
            case ECommonSubPacketType.HealthSync:
                SubPacket.HealthSync.Serialize(writer);
                break;
            case ECommonSubPacketType.UsableItem:
                SubPacket.UsableItem.Serialize(writer);
                break;
            case ECommonSubPacketType.DownedSync:
                writer.PutUnmanaged(SubPacket.DownedSync);
                break;
            case ECommonSubPacketType.RevivedPlayer:
                break;
            case ECommonSubPacketType.RevivingPlayer:
                SubPacket.RevivingPlayer.Serialize(writer);
                break;
        }
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct CommonSubPacketData
{
    [FieldOffset(0)] public PhrasePacket Phrase;
    [FieldOffset(0)] public WorldInteractionPacket WorldInteraction;
    [FieldOffset(0)] public ContainerInteractionPacket ContainerInteraction;
    [FieldOffset(0)] public ProceedPacket Proceed;
    [FieldOffset(0)] public HeadLightsPacket HeadLights;
    [FieldOffset(0)] public InventoryChangedPacket InventoryChanged;
    [FieldOffset(0)] public DropPacket Drop;
    [FieldOffset(0)] public StationaryPacket Stationary;
    [FieldOffset(0)] public VaultPacket Vault;
    [FieldOffset(0)] public InteractionPacket Interaction;
    [FieldOffset(0)] public MountingPacket Mounting;
    [FieldOffset(0)] public DamagePacket Damage;
    [FieldOffset(0)] public ArmorDamagePacket ArmorDamage;
    [FieldOffset(0)] public HealthSyncPacket HealthSync;
    [FieldOffset(0)] public UsableItemPacket UsableItem;
    [FieldOffset(0)] public DownedSyncPacket DownedSync;
    [FieldOffset(0)] public RevivedPlayerPacket RevivedPlayer;
    [FieldOffset(0)] public RevivingPlayerPacket RevivingPlayer;

    public readonly void Execute(ECommonSubPacketType type, FikaPlayer player)
    {
        switch (type)
        {
            case ECommonSubPacketType.Phrase:
                Phrase.Execute(player);
                break;
            case ECommonSubPacketType.WorldInteraction:
                WorldInteraction.Execute(player);
                break;
            case ECommonSubPacketType.ContainerInteraction:
                ContainerInteraction.Execute(player);
                break;
            case ECommonSubPacketType.Proceed:
                Proceed.Execute(player);
                break;
            case ECommonSubPacketType.HeadLights:
                HeadLights.Execute(player);
                break;
            case ECommonSubPacketType.InventoryChanged:
                InventoryChanged.Execute(player);
                break;
            case ECommonSubPacketType.Drop:
                Drop.Execute(player);
                break;
            case ECommonSubPacketType.Stationary:
                Stationary.Execute(player);
                break;
            case ECommonSubPacketType.Vault:
                Vault.Execute(player);
                break;
            case ECommonSubPacketType.Interaction:
                Interaction.Execute(player);
                break;
            case ECommonSubPacketType.Mounting:
                Mounting.Execute(player);
                break;
            case ECommonSubPacketType.Damage:
                Damage.Execute(player);
                break;
            case ECommonSubPacketType.ArmorDamage:
                ArmorDamage.Execute(player);
                break;
            case ECommonSubPacketType.HealthSync:
                HealthSync.Execute(player);
                break;
            case ECommonSubPacketType.UsableItem:
                UsableItem.Execute(player);
                break;
            case ECommonSubPacketType.DownedSync:
                DownedSync.Execute(player);
                break;
            case ECommonSubPacketType.RevivedPlayer:
                RevivedPlayer.Execute(player);
                break;
            case ECommonSubPacketType.RevivingPlayer:
                RevivingPlayer.Execute(player);
                break;
        }
    }
}