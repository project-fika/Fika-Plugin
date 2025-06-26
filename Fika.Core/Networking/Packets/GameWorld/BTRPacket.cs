using LiteNetLib.Utils;

namespace Fika.Core.Networking;
public struct BTRPacket : INetSerializable
{
    public BTRDataPacketStruct Data;

    public void Deserialize(NetDataReader reader)
    {
        ref BTRDataPacketStruct data = ref Data;

        data.position = reader.GetVector3();
        data.BtrBotId = reader.GetInt();
        data.MoveSpeed = reader.GetFloat();
        data.moveDirection = reader.GetByte();
        data.timeToEndPause = reader.GetFloat();
        data.currentSpeed = reader.GetFloat();
        data.RightSlot1State = reader.GetByte();
        data.RightSlot0State = reader.GetByte();
        data.RightSideState = reader.GetByte();
        data.LeftSlot1State = reader.GetByte();
        data.LeftSlot0State = reader.GetByte();
        data.LeftSideState = reader.GetByte();
        data.RouteState = reader.GetByte();
        data.State = reader.GetByte();
        data.gunsBlockRotation = reader.GetFloat();
        data.turretRotation = reader.GetFloat();
        data.rotation = reader.GetQuaternion();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutVector3(Data.position);
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
        writer.PutQuaternion(Data.rotation);
    }
}