using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct ArmorDamagePacket
{
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

    public readonly void Execute(FikaPlayer player = null)
    {
        player.HandleArmorDamagePacket(this);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(ItemId);
        writer.Put(Durability);
    }
}
