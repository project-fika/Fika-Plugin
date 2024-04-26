using LiteNetLib.Utils;
using UnityEngine;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct SendCharacterPacket(PlayerInfoPacket playerInfoPacket, bool isAlive, bool isAi, Vector3 position) : INetSerializable
    {
        public PlayerInfoPacket PlayerInfo = playerInfoPacket;
        public bool IsAlive = isAlive;
        public bool IsAI = isAi;
        public Vector3 Position = position;

        public void Deserialize(NetDataReader reader)
        {
            PlayerInfo = PlayerInfoPacket.Deserialize(reader);
            IsAlive = reader.GetBool();
            IsAI = reader.GetBool();
            Position = reader.GetVector3();
        }

        public void Serialize(NetDataWriter writer)
        {
            PlayerInfoPacket.Serialize(writer, PlayerInfo);
            writer.Put(IsAlive);
            writer.Put(IsAI);
            writer.Put(Position);
        }
    }
}
