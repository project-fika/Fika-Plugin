using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public class CommandPacket : INetSerializable
    {
        public CommandPacket()
        {
        }

        public CommandPacket(ECommandType type)
        {
            CommandType = type;
        }

        public ECommandType CommandType;

        public string SpawnType;
        public int SpawnAmount;

        public int NetId;

        public void Deserialize(NetDataReader reader)
        {
            CommandType = (ECommandType)reader.GetByte();

            switch (CommandType)
            {
                case ECommandType.SpawnAI:
                    SpawnType = reader.GetString();
                    SpawnAmount = reader.GetInt();
                    break;
                case ECommandType.DespawnAI:
                case ECommandType.Bring:
                    NetId = reader.GetInt();
                    break;
                case ECommandType.SpawnAirdrop:
                default:
                    break;
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((byte)CommandType);

            switch (CommandType)
            {
                case ECommandType.SpawnAI:
                    writer.Put(SpawnType);
                    writer.Put(SpawnAmount);
                    break;
                case ECommandType.DespawnAI:
                case ECommandType.Bring:
                    writer.Put(NetId);
                    break;
                case ECommandType.SpawnAirdrop:
                default:
                    break;
            }
        }

        public enum ECommandType
        {
            SpawnAI,
            DespawnAI,
            Bring,
            SpawnAirdrop
        }
    }
}
