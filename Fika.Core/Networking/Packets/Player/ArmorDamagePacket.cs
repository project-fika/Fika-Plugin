using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ArmorDamagePacket : IQueuePacket
    {
        public int NetId { get; set; }
        public string[] ItemIds;
        public float[] Durabilities;

        public void Execute(CoopPlayer player)
        {
            player.HandleArmorDamagePacket(ref this);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            ItemIds = reader.GetStringArray();
            Durabilities = reader.GetFloatArray();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutArray(ItemIds);
            writer.PutArray(Durabilities);
        }
    }
}
