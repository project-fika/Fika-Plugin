// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;
using UnityEngine;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct AllCharacterRequestPacket(string profileId) : INetSerializable
    {
        public bool IsRequest = true;
        public string ProfileId = profileId;
        public bool HasCharacters = false;
        public string[] Characters;
        public PlayerInfoPacket PlayerInfo;
        public bool IsAlive = true;
        public bool IsAI = false;
        public Vector3 Position;
        public int NetId;

        public void Deserialize(NetDataReader reader)
        {
            IsRequest = reader.GetBool();
            ProfileId = reader.GetString();
            HasCharacters = reader.GetBool();
            if (HasCharacters)
            {
                Characters = reader.GetStringArray();
            }
            if (!IsRequest)
            {
                PlayerInfo = PlayerInfoPacket.Deserialize(reader);
            }
            IsAlive = reader.GetBool();
            IsAI = reader.GetBool();
            Position = reader.GetVector3();
            NetId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(IsRequest);
            writer.Put(ProfileId);
            writer.Put(HasCharacters);
            if (HasCharacters)
            {
                writer.PutArray(Characters);
            }
            if (!IsRequest)
            {
                PlayerInfoPacket.Serialize(writer, PlayerInfo);
            }
            writer.Put(IsAlive);
            writer.Put(IsAI);
            writer.Put(Position);
            writer.Put(NetId);
        }
    }
}
