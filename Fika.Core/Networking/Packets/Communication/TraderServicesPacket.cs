using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public struct TraderServicesPacket(int NetId) : INetSerializable
	{
		public int NetId = NetId;
		public List<TraderServicesClass> Services;

		public void Deserialize(NetDataReader reader)
		{
			int num = reader.GetInt();
			if (num > 0)
			{
				Services = new(num);
				byte[] data = reader.GetByteArray();
				GClass1158 eftReader = new(data);
				for (int i = 0; i < num; i++)
				{
					TraderServicesClass traderServicesClass = eftReader.ReadPolymorph<TraderServicesClass>();
					Services.Add(traderServicesClass);
				}
			}

		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Services.Count);
			GClass1162 eftWriter = new();
			for (int i = 0; i < Services.Count; i++)
			{
				eftWriter.WritePolymorph(Services[i]);
			}
		}
	}
}
