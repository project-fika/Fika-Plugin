using LiteNetLib.Utils;

namespace Fika.Core.Networking;
public struct BTRPacket : INetSerializable
{
	public BTRDataPacket Data;

	public void Deserialize(NetDataReader reader)
	{
		Data = new BTRDataPacket
		{
			position = reader.GetVector3(),
			BtrBotId = reader.GetInt(),
			MoveSpeed = reader.GetFloat(),
			moveDirection = reader.GetByte(),
			timeToEndPause = reader.GetFloat(),
			currentSpeed = reader.GetFloat(),
			RightSlot1State = reader.GetByte(),
			RightSlot0State = reader.GetByte(),
			RightSideState = reader.GetByte(),
			LeftSlot1State = reader.GetByte(),
			LeftSlot0State = reader.GetByte(),
			LeftSideState = reader.GetByte(),
			RouteState = reader.GetByte(),
			State = reader.GetByte(),
			gunsBlockRotation = reader.GetFloat(),
			turretRotation = reader.GetFloat(),
			rotation = reader.GetQuaternion()
		};
	}

	public void Serialize(NetDataWriter writer)
	{
		writer.Put(Data.position);
		writer.Put(Data.BtrBotId);
		writer.Put(Data.MoveSpeed);
		writer.Put(Data.moveDirection);
		writer.Put(Data.timeToEndPause);
		writer.Put(Data.currentSpeed);
		writer.Put(Data.RightSlot1State);
		writer.Put(Data.RightSlot0State);
		writer.Put(Data.RightSideState);
		writer.Put(Data.LeftSlot1State);
		writer.Put(Data.LeftSlot0State);
		writer.Put(Data.LeftSideState);
		writer.Put(Data.RouteState);
		writer.Put(Data.State);
		writer.Put(Data.gunsBlockRotation);
		writer.Put(Data.turretRotation);
		writer.Put(Data.rotation);
	}
}