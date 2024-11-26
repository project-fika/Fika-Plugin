using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct LootSyncPacket : INetSerializable
	{
		public LootSyncStruct Data;

		public void Deserialize(NetDataReader reader)
		{
			Data = new()
			{
				Id = reader.GetInt(),
				Position = reader.GetVector3(),
				Rotation = reader.GetQuaternion(),
				Done = reader.GetBool()
			};
			if (!Data.Done)
			{
				Data.Velocity = reader.GetVector3();
				Data.AngularVelocity = reader.GetVector3();
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Data.Id);
			writer.Put(Data.Position);
			writer.Put(Data.Rotation);
			writer.Put(Data.Done);
			if (!Data.Done)
			{
				writer.Put(Data.Velocity);
				writer.Put(Data.AngularVelocity);
			}
		}
	}
}
