using EFT;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.Backend
{
    public class InraidQuestPacket : INetSerializable
    {
        public int NetId;
        public InraidQuestType Type;
        public List<FlatItemsDataClass[]> Items;
        public List<MongoID?> ItemIdsToRemove;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Type = (InraidQuestType)reader.GetByte();
            switch (Type)
            {
                case InraidQuestType.Finish:
                    {
                        int length = reader.GetInt();
                        Items = new(length);
                        for (int i = 0; i < length; i++)
                        {
                            Items.Add(JsonConvert.DeserializeObject<FlatItemsDataClass[]>(reader.GetString()));
                        }
                    }
                    break;
                case InraidQuestType.Handover:
                    {
                        if (reader.GetBool())
                        {
                            int length = reader.GetInt();
                            ItemIdsToRemove = new(length);
                            for (int i = 0; i < length; i++)
                            {
                                ItemIdsToRemove.Add(reader.GetMongoID());
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put((byte)Type);
            switch (Type)
            {
                case InraidQuestType.Finish:
                    writer.Put(Items.Count);
                    for (int i = 0; i < Items.Count; i++)
                    {
                        writer.Put(JsonConvert.SerializeObject(Items[i]));
                    }
                    break;
                case InraidQuestType.Handover:
                    if (ItemIdsToRemove != null)
                    {
                        writer.Put(true);
                        writer.Put(ItemIdsToRemove.Count);
                        for (int i = 0; i < ItemIdsToRemove.Count; i++)
                        {
                            writer.PutMongoID(ItemIdsToRemove[i]);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public enum InraidQuestType
        {
            Finish,
            Handover
        }
    }
}
