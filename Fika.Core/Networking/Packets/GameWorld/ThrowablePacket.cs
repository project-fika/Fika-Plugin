using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	internal struct ThrowablePacket : INetSerializable
	{
		public GStruct128 Data;

		public void Deserialize(NetDataReader reader)
		{
			Data = new()
			{
				Id = reader.GetInt(),
				Position = reader.GetVector3(),
				Rotation = reader.GetQuaternion(),
				CollisionNumber = reader.GetByte()
			};
			if (!reader.GetBool())
			{
				Data.Velocity = reader.GetVector3();
				Data.AngularVelocity = reader.GetVector3();
			}
			else
			{
				Data.Done = true;
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Data.Id);
			writer.Put(Data.Position);
			writer.Put(Data.Rotation);
			writer.Put(Data.CollisionNumber);
			writer.Put(Data.Done);
			if (!Data.Done)
			{
				writer.Put(Data.Velocity);
				writer.Put(Data.AngularVelocity);
			}
		}
	}
}
