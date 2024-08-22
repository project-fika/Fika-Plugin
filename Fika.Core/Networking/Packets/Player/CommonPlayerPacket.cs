using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

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
				WorldInteractionPacket = WorldInteractionPacket.Deserialize(reader);
			}
			HasContainerInteractionPacket = reader.GetBool();
			if (HasContainerInteractionPacket)
			{
				ContainerInteractionPacket = ContainerInteractionPacket.Deserialize(reader);
			}
			HasProceedPacket = reader.GetBool();
			if (HasProceedPacket)
			{
				ProceedPacket = ProceedPacket.Deserialize(reader);
			}
			HasHeadLightsPacket = reader.GetBool();
			if (HasHeadLightsPacket)
			{
				HeadLightsPacket = HeadLightsPacket.Deserialize(reader);
			}
			HasInventoryChanged = reader.GetBool();
			if (HasInventoryChanged)
			{
				SetInventoryOpen = reader.GetBool();
			}
			HasDrop = reader.GetBool();
			if (HasDrop)
			{
				DropPacket = DropPacket.Deserialize(reader);
			}
			HasStationaryPacket = reader.GetBool();
			if (HasStationaryPacket)
			{
				StationaryPacket = StationaryPacket.Deserialize(reader);
			}
			HasVaultPacket = reader.GetBool();
			if (HasVaultPacket)
			{
				VaultPacket = VaultPacket.Deserialize(reader);
			}
			Interaction = (EInteraction)reader.GetByte();
			HasMountingPacket = reader.GetBool();
			if (HasMountingPacket)
			{
				MountingPacket = MountingPacket.Deserialize(reader);
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
				WorldInteractionPacket.Serialize(writer, WorldInteractionPacket);
			}
			writer.Put(HasContainerInteractionPacket);
			if (HasContainerInteractionPacket)
			{
				ContainerInteractionPacket.Serialize(writer, ContainerInteractionPacket);
			}
			writer.Put(HasProceedPacket);
			if (HasProceedPacket)
			{
				ProceedPacket.Serialize(writer, ProceedPacket);
			}
			writer.Put(HasHeadLightsPacket);
			if (HasHeadLightsPacket)
			{
				HeadLightsPacket.Serialize(writer, HeadLightsPacket);
			}
			writer.Put(HasInventoryChanged);
			if (HasInventoryChanged)
			{
				writer.Put(SetInventoryOpen);
			}
			writer.Put(HasDrop);
			if (HasDrop)
			{
				DropPacket.Serialize(writer, DropPacket);
			}
			writer.Put(HasStationaryPacket);
			if (HasStationaryPacket)
			{
				StationaryPacket.Serialize(writer, StationaryPacket);
			}
			writer.Put(HasVaultPacket);
			if (HasVaultPacket)
			{
				VaultPacket.Serialize(writer, VaultPacket);
			}
			writer.Put((byte)Interaction);
			writer.Put(HasMountingPacket);
			if (HasMountingPacket)
			{
				MountingPacket.Serialize(writer, MountingPacket);
			}
		}
	}
}
