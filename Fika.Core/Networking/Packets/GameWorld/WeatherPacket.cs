// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
	public struct WeatherPacket : INetSerializable
	{
		public bool IsRequest;
		public bool HasData;
		public ESeason Season;
		public Vector3 SpringSnowFactor;
		public int Amount;
		public WeatherClass[] WeatherClasses;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			HasData = reader.GetBool();
			if (HasData)
			{
				Season = (ESeason)reader.GetByte();
				SpringSnowFactor = reader.GetVector3();
				Amount = reader.GetInt();
				WeatherClasses = new WeatherClass[Amount];
				for (int i = 0; i < Amount; i++)
				{
					WeatherClasses[i] = reader.GetWeatherClass();
				}
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			writer.Put(HasData);
			if (HasData)
			{
				writer.Put((byte)Season);
				writer.Put(SpringSnowFactor);
				writer.Put(Amount);
				for (int i = 0; i < Amount; i++)
				{
					writer.PutWeatherClass(WeatherClasses[i]);
				}
			}
		}
	}
}
