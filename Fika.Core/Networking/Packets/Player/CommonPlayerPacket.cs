using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPackets;

namespace Fika.Core.Networking
{
    public struct CommonPlayerPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public EPhraseTrigger Phrase = EPhraseTrigger.PhraseNone;
		public int PhraseIndex;
		public bool HasWorldInteractionPacket = false;
		public WorldInteractionPacket WorldInteractionPacket;
		public bool HasContainerInteractionPacket = false;
		public ContainerInteractionPacket ContainerInteractionPacket;
		public bool HasProceedPacket = false;
		public ProceedPacket ProceedPacket;
		public bool HasHeadLightsPacket = false;
		public HeadLightsPacket HeadLightsPacket;
		public bool HasInventoryChanged = false;
		public bool SetInventoryOpen;
		public bool HasDrop = false;
		public DropPacket DropPacket;
		public bool HasStationaryPacket = false;
		public StationaryPacket StationaryPacket;
		public bool HasVaultPacket = false;
		public VaultPacket VaultPacket;
		public EInteraction Interaction = EInteraction.None;
		public bool HasMountingPacket = false;
		public MountingPacket MountingPacket;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Phrase = (EPhraseTrigger)reader.GetInt();
			if (Phrase != EPhraseTrigger.PhraseNone)
			{
				PhraseIndex = reader.GetInt();
			}
			HasWorldInteractionPacket = reader.GetBool();
			if (HasWorldInteractionPacket)
			{
				WorldInteractionPacket = reader.GetWorldInteractionPacket();
			}
			HasContainerInteractionPacket = reader.GetBool();
			if (HasContainerInteractionPacket)
			{
				ContainerInteractionPacket = reader.GetContainerInteractionPacket();
			}
			HasProceedPacket = reader.GetBool();
			if (HasProceedPacket)
			{
				ProceedPacket = reader.GetProceedPacket();
			}
			HasHeadLightsPacket = reader.GetBool();
			if (HasHeadLightsPacket)
			{
				HeadLightsPacket = reader.GetHeadLightsPacket();
			}
			HasInventoryChanged = reader.GetBool();
			if (HasInventoryChanged)
			{
				SetInventoryOpen = reader.GetBool();
			}
			HasDrop = reader.GetBool();
			if (HasDrop)
			{
				DropPacket = reader.GetDropPacket();
			}
			HasStationaryPacket = reader.GetBool();
			if (HasStationaryPacket)
			{
				StationaryPacket = reader.GetStationaryPacket();
			}
			HasVaultPacket = reader.GetBool();
			if (HasVaultPacket)
			{
				VaultPacket = reader.GetVaultPacket();
			}
			Interaction = (EInteraction)reader.GetByte();
			HasMountingPacket = reader.GetBool();
			if (HasMountingPacket)
			{
				MountingPacket = reader.GetMountingPacket();
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put((int)Phrase);
			if (Phrase != EPhraseTrigger.PhraseNone)
			{
				writer.Put(PhraseIndex);
			}
			writer.Put(HasWorldInteractionPacket);
			if (HasWorldInteractionPacket)
			{
				writer.PutWorldInteractionPacket(WorldInteractionPacket);
			}
			writer.Put(HasContainerInteractionPacket);
			if (HasContainerInteractionPacket)
			{
				writer.PutContainerInteractionPacket(ContainerInteractionPacket);
			}
			writer.Put(HasProceedPacket);
			if (HasProceedPacket)
			{
				writer.PutProceedPacket(ProceedPacket);
			}
			writer.Put(HasHeadLightsPacket);
			if (HasHeadLightsPacket)
			{
				writer.PutHeadLightsPacket(HeadLightsPacket);
			}
			writer.Put(HasInventoryChanged);
			if (HasInventoryChanged)
			{
				writer.Put(SetInventoryOpen);
			}
			writer.Put(HasDrop);
			if (HasDrop)
			{
				writer.PutDropPacket(DropPacket);
			}
			writer.Put(HasStationaryPacket);
			if (HasStationaryPacket)
			{
				writer.PutStationaryPacket(StationaryPacket);
			}
			writer.Put(HasVaultPacket);
			if (HasVaultPacket)
			{
				writer.PutVaultPacket(VaultPacket);
			}
			writer.Put((byte)Interaction);
			writer.Put(HasMountingPacket);
			if (HasMountingPacket)
			{
				writer.PutMountingPacket(MountingPacket);
			}
		}
	}
}
