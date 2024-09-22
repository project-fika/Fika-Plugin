using EFT.Airdrop;
using EFT.SynchronizableObjects;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct AirdropUpdatePacket : INetSerializable
	{
		public AirplaneDataPacketStruct Data;

		public void Deserialize(NetDataReader reader)
		{
			Data = new()
			{
				ObjectId = reader.GetInt(),
				Position = reader.GetVector3(),
				Rotation = reader.GetVector3(),
				ObjectType = (SynchronizableObjectType)reader.GetByte()
			};
			if (Data.ObjectType == SynchronizableObjectType.AirDrop)
			{
				Data.PacketData.AirdropDataPacket.SignalFire = reader.GetBool();
				Data.PacketData.AirdropDataPacket.FallingStage = (EAirdropFallingStage)reader.GetByte();
				Data.PacketData.AirdropDataPacket.AirdropType = (EAirdropType)reader.GetByte();
				Data.PacketData.AirdropDataPacket.UniqueId = reader.GetInt();
			}
			else
			{
				Data.PacketData.AirplaneDataPacket.AirplanePercent = reader.GetInt();
			}
			Data.Outdated = reader.GetBool();
			Data.IsStatic = reader.GetBool();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Data.ObjectId);
			writer.Put(Data.Position);
			writer.Put(Data.Rotation);
			writer.Put((byte)Data.ObjectType);
			if (Data.ObjectType == SynchronizableObjectType.AirDrop)
			{
				writer.Put(Data.PacketData.AirdropDataPacket.SignalFire);
				writer.Put((byte)Data.PacketData.AirdropDataPacket.FallingStage);
				writer.Put((byte)Data.PacketData.AirdropDataPacket.AirdropType);
				writer.Put(Data.PacketData.AirdropDataPacket.UniqueId);
			}
			else
			{
				writer.Put(Data.PacketData.AirplaneDataPacket.AirplanePercent);
			}
			writer.Put(Data.Outdated);
			writer.Put(Data.IsStatic);
		}
	}
}
