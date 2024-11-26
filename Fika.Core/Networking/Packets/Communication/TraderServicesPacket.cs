using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Networking
{
	public struct TraderServicesPacket(int NetId) : INetSerializable
	{
		public int NetId = NetId;
		public bool IsRequest = false;
		public string TraderId;
		public List<TraderServicesClass> Services = [];

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			IsRequest = reader.GetBool();
			if (IsRequest)
			{
				TraderId = reader.GetString();
				return;
			}
			int num = reader.GetInt();
			if (num > 0)
			{
				Services = new(num);
				for (int i = 0; i < num; i++)
				{
					TraderServicesClass traderServicesClass = reader.GetTraderService();
					Services.Add(traderServicesClass);
				}
			}

		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(IsRequest);
			if (IsRequest)
			{
				writer.Put(TraderId);
				return;
			}
			int amount = Services.Count;
			writer.Put(amount);
			if (amount > 0)
			{
				for (int i = 0; i < Services.Count; i++)
				{
					writer.PutTraderService(Services[i]);
				}
			}
		}
	}
}
