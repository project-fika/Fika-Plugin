// © 2024 Lacyway All Rights Reserved

using ComponentAce.Compression.Libs.zlib;
using Fika.Core.Coop.Airdrops.Models;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
	public struct AirdropPacket() : INetSerializable
	{
		public FikaAirdropConfigModel Config;
		public bool AirdropAvailable;
		public bool PlaneSpawned;
		public bool BoxSpawned;
		public float DistanceTraveled;
		public float DistanceToTravel;
		public float DistanceToDrop;
		public float Timer;
		public int DropHeight;
		public int TimeToStart;
		public Vector3 BoxPoint;
		public Vector3 SpawnPoint;
		public Vector3 LookPoint;

		public void Deserialize(NetDataReader reader)
		{
			byte[] configBytes = reader.GetByteArray();
			Config = SimpleZlib.Decompress(configBytes, null).ParseJsonTo<FikaAirdropConfigModel>();
			AirdropAvailable = reader.GetBool();
			PlaneSpawned = reader.GetBool();
			BoxSpawned = reader.GetBool();
			DistanceTraveled = reader.GetFloat();
			DistanceToTravel = reader.GetFloat();
			DistanceToDrop = reader.GetFloat();
			Timer = reader.GetFloat();
			DropHeight = reader.GetInt();
			TimeToStart = reader.GetInt();
			BoxPoint = reader.GetVector3();
			SpawnPoint = reader.GetVector3();
			LookPoint = reader.GetVector3();
		}

		public void Serialize(NetDataWriter writer)
		{
			byte[] configBytes = SimpleZlib.CompressToBytes(Config.ToJson(), 4, null);
			writer.PutByteArray(configBytes);
			writer.Put(AirdropAvailable);
			writer.Put(PlaneSpawned);
			writer.Put(BoxSpawned);
			writer.Put(DistanceTraveled);
			writer.Put(DistanceToTravel);
			writer.Put(DistanceToDrop);
			writer.Put(Timer);
			writer.Put(DropHeight);
			writer.Put(TimeToStart);
			writer.Put(BoxPoint);
			writer.Put(SpawnPoint);
			writer.Put(LookPoint);
		}
	}
}
