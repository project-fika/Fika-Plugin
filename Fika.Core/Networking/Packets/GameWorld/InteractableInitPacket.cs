using ComponentAce.Compression.Libs.zlib;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public struct InteractableInitPacket(bool isRequest) : INetSerializable
	{
		public bool IsRequest = isRequest;
		public byte[] RawData;
		public Dictionary<string, int> Interactables;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			if (!IsRequest)
			{
				RawData = reader.GetByteArray();
				Interactables = SimpleZlib.Decompress(RawData, null).ParseJsonTo<Dictionary<string, int>>();
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			if (!IsRequest)
			{
				byte[] data = SimpleZlib.CompressToBytes(Interactables.ToJson([]), 6);
				writer.PutByteArray(data);
			}
		}
	}
}
