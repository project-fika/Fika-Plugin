using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public class ArmorDamagePacket : IPoolSubPacket
{
    private ArmorDamagePacket() { }

    public static ArmorDamagePacket CreateInstance()
    {
        return new();
    }

    public static ArmorDamagePacket FromValue(MongoID itemId, float amount)
    {
        ArmorDamagePacket packet = CommonSubPacketPoolManager.Instance.GetPacket<ArmorDamagePacket>(ECommonSubPacketType.ArmorDamage);
        packet.ItemId = itemId;
        packet.Durability = amount;
        return packet;
    }

    public MongoID ItemId;
    public float Durability;

    public void Execute(FikaPlayer player = null)
    {
        player.HandleArmorDamagePacket(this);
    }

    public void Deserialize(NetDataReader reader)
    {
        ItemId = reader.GetMongoID();
        Durability = reader.GetFloat();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutMongoID(ItemId);
        writer.Put(Durability);
    }

    public void Dispose()
    {
        ItemId = default;
        Durability = 0f;
    }
}
