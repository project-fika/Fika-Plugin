using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public struct ThrowablePacket : INetSerializable
	{
		public int Count;
		public List<GStruct131> Data;

		public void Deserialize(NetDataReader reader)
		{
			Count = reader.GetInt();
			if (Count > 0)
			{
				Data = [];
				for (int i = 0; i < Count; i++)
				{
					Data.Add(reader.GetGrenadeStruct());
				}
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Count);
			if (Count > 0)
			{
				for (int i = 0; i < Count; i++)
				{
					writer.PutGrenadeStruct(Data[i]);
				}
			}
		}
	}
}
