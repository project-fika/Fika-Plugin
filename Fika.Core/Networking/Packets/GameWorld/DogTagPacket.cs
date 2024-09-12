using EFT;
using LiteNetLib.Utils;
using System;

namespace Fika.Core.Networking.Packets.GameWorld
{
	public struct DogTagPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public string AccountId;
		public string ProfileId;
		public string Nickname;
		public string KillerAccountId;
		public string KillerProfileId;
		public string KillerName;
		public EPlayerSide Side;
		public int Level;
		public DateTime Time;
		public string WeaponName;
		public string GroupId;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			AccountId = reader.GetString();
			ProfileId = reader.GetString();
			Nickname = reader.GetString();
			KillerAccountId = reader.GetString();
			KillerProfileId = reader.GetString();
			KillerName = reader.GetString();
			Side = (EPlayerSide)reader.GetByte();
			Level = reader.GetInt();
			Time = reader.GetDateTime();
			WeaponName = reader.GetString();
			GroupId = reader.GetString();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(AccountId);
			writer.Put(ProfileId);
			writer.Put(Nickname);
			writer.Put(KillerAccountId);
			writer.Put(KillerProfileId);
			writer.Put(KillerName);
			writer.Put((byte)Side);
			writer.Put(Level);
			writer.Put(Time);
			writer.Put(WeaponName);
			writer.Put(GroupId);
		}
	}
}
