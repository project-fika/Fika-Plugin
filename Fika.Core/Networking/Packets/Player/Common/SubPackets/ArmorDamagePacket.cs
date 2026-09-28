using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct ArmorDamagePacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.ArmorDamage;

    public ArmorDamagePacket(MongoID itemId, float amount)
    {
        ItemId = itemId;
        Durability = amount;
    }

    public ArmorDamagePacket(NetDataReader reader)
    {
        ItemId = reader.GetMongoID();
        Durability = reader.GetFloat();
    }

    public readonly MongoID ItemId;
    public readonly float Durability;

    public readonly void Execute(FikaPlayer player)
    {
        player.HandleArmorDamagePacket(this);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(ItemId);
        writer.Put(Durability);
    }
}
