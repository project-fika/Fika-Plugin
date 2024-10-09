// © 2024 Lacyway All Rights Reserved

using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	/// <summary>
	/// Packet used for many different things to reduce packet bloat
	/// </summary>
	/// <param name="packageType"></param>
	public class GenericPacket : INetSerializable
	{
		public int NetId;
		public EPackageType Type;

		/*public byte PlatformId;
		public float PlatformPosition;*/

		public string ExfilName;
		public float ExfilStartTime;

		public ETraderServiceType TraderServiceType;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Type = (EPackageType)reader.GetInt();
			switch (Type)
			{
				/*case EPackageType.TrainSync:
					PlatformId = reader.GetByte();
					PlatformPosition = reader.GetFloat();
					break;*/
				case EPackageType.ExfilCountdown:
					ExfilName = reader.GetString();
					ExfilStartTime = reader.GetFloat();
					break;
				case EPackageType.TraderServiceNotification:
					TraderServiceType = (ETraderServiceType)reader.GetInt();
					break;
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put((int)Type);
			switch (Type)
			{
				/*case EPackageType.TrainSync:
					writer.Put(PlatformId);
					writer.Put(PlatformPosition);
					break;*/
				case EPackageType.ExfilCountdown:
					writer.Put(ExfilName);
					writer.Put(ExfilStartTime);
					break;
				case EPackageType.TraderServiceNotification:
					writer.Put((int)TraderServiceType);
					break;
			}
		}
	}

	public enum EPackageType
	{
		ClientExtract,
		//TrainSync,
		ExfilCountdown,
		TraderServiceNotification,
		ClearEffects
	}
}
