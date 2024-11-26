// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ClientClasses
{
	public sealed class CoopBotHealthController(Profile.ProfileHealthClass healthInfo, Player player, InventoryController inventoryController, SkillManager skillManager, bool aiHealth)
		: GControl4(healthInfo, player, inventoryController, skillManager, aiHealth)
	{
		private readonly CoopBot coopBot = (CoopBot)player;
		public override bool _sendNetworkSyncPackets
		{
			get
			{
				return true;
			}
		}

		public override void SendNetworkSyncPacket(NetworkHealthSyncPacketStruct packet)
		{
			if (packet.SyncType == NetworkHealthSyncPacketStruct.ESyncType.IsAlive && !packet.Data.IsAlive.IsAlive)
			{
				coopBot.PacketSender.HealthSyncPackets.Enqueue(coopBot.SetupCorpseSyncPacket(packet));
				return;
			}

			coopBot.PacketSender.HealthSyncPackets.Enqueue(new(coopBot.NetId)
			{
				Packet = packet
			});
		}
	}
}
