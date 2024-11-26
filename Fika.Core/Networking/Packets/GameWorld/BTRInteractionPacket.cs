using EFT;
using EFT.Vehicle;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct BTRInteractionPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public bool IsResponse = false;
		public EBtrInteractionStatus Status;
		public PlayerInteractPacket Data;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			IsResponse = reader.GetBool();
			if (IsResponse)
			{
				Status = (EBtrInteractionStatus)reader.GetByte();
			}
			Data = new()
			{
				HasInteraction = reader.GetBool(),
				InteractionType = (EInteractionType)reader.GetByte(),
				SideId = reader.GetByte(),
				SlotId = reader.GetByte(),
				Fast = reader.GetBool()
			};
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(IsResponse);
			if (IsResponse)
			{
				writer.Put((byte)Status);
			}
			writer.Put(Data.HasInteraction);
			writer.Put((byte)Data.InteractionType);
			writer.Put(Data.SideId);
			writer.Put(Data.SlotId);
			writer.Put(Data.Fast);
		}
	}
}
