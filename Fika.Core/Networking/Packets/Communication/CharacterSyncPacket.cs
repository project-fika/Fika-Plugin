using Fika.Core.Main.Players;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.Communication
{
    public class CharacterSyncPacket : INetSerializable
    {
        public CharacterSyncPacket()
        {

        }

        public CharacterSyncPacket(Dictionary<int, FikaPlayer> players)
        {
            PlayerIds = new(players.Count);
            foreach ((int netid, _) in players)
            {
                PlayerIds.Add(netid);
            }
        }

        public List<int> PlayerIds;

        public void Deserialize(NetDataReader reader)
        {
            ushort amount = reader.GetUShort();
            if (amount > 0)
            {
                PlayerIds = new(amount);
                for (int i = 0; i < amount; i++)
                {
                    PlayerIds.Add(reader.GetInt());
                }
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((ushort)PlayerIds.Count);
            foreach (int netId in PlayerIds)
            {
                writer.Put(netId);
            }
        }
    }
}
