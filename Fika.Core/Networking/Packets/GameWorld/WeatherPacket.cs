// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
	public struct WeatherPacket : INetSerializable
	{
		public bool IsRequest;
		public bool HasData;
		public int Amount;
		public WeatherClass[] WeatherClasses;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			HasData = reader.GetBool();
			if (HasData)
			{
				Amount = reader.GetInt();
				WeatherClasses = new WeatherClass[Amount];
				for (int i = 0; i < Amount; i++)
				{
					WeatherClassPacket weatherClassPacket = WeatherClassPacket.Deserialize(reader);
					WeatherClasses[i] = new()
					{
						AtmospherePressure = weatherClassPacket.AtmospherePressure,
						Cloudness = weatherClassPacket.Cloudness,
						GlobalFogDensity = weatherClassPacket.GlobalFogDensity,
						GlobalFogHeight = weatherClassPacket.GlobalFogHeight,
						LyingWater = weatherClassPacket.LyingWater,
						MainWindDirection = weatherClassPacket.MainWindDirection,
						MainWindPosition = weatherClassPacket.MainWindPosition,
						Rain = weatherClassPacket.Rain,
						RainRandomness = weatherClassPacket.RainRandomness,
						ScaterringFogDensity = weatherClassPacket.ScaterringFogDensity,
						ScaterringFogHeight = weatherClassPacket.ScaterringFogDensity,
						Temperature = weatherClassPacket.Temperature,
						Time = weatherClassPacket.Time,
						TopWindDirection = weatherClassPacket.TopWindDirection,
						TopWindPosition = weatherClassPacket.TopWindPosition,
						Turbulence = weatherClassPacket.Turbulence,
						Wind = weatherClassPacket.Wind,
						WindDirection = weatherClassPacket.WindDirection
					};
				}
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			writer.Put(HasData);
			if (HasData)
			{
				writer.Put(Amount);
				for (int i = 0; i < Amount; i++)
				{
					WeatherClassPacket.Serialize(writer, WeatherClasses[i]);
				}
			}
		}
	}
}
