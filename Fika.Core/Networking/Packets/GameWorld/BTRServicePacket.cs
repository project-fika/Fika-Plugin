using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct BTRServicePacket(string profileId) : INetSerializable
	{
		public string ProfileId;
		public ETraderServiceType TraderServiceType;
		public bool HasSubservice = false;
		public string SubserviceId;

		public void Deserialize(NetDataReader reader)
		{
			ProfileId = reader.GetString();
			TraderServiceType = (ETraderServiceType)reader.GetInt();
			HasSubservice = reader.GetBool();
			if (HasSubservice)
			{
				SubserviceId = reader.GetString();
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(profileId);
			writer.Put((int)TraderServiceType);
			writer.Put(HasSubservice);
			if (HasSubservice)
			{
				writer.Put(SubserviceId);
			}
		}
	}
}
