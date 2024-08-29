// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic.Operations;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using JetBrains.Annotations;
using static EFT.Player;

namespace Fika.Core.Coop.BotClasses
{
	public class CoopBotInventoryController : PlayerInventoryController
	{
		private readonly CoopBot CoopBot;
		private readonly IPlayerSearchController searchController;

		public CoopBotInventoryController(Player player, Profile profile, bool examined, MongoID currentId, ushort nextOperationId) : base(player, profile, examined)
		{
			CoopBot = (CoopBot)player;
			mongoID_0 = currentId;
			ushort_0 = nextOperationId;

			if (!player.IsAI && !examined)
			{
				IPlayerSearchController playerSearchController = new GClass1867(profile, this);
				searchController = playerSearchController;
			}
			else
			{
				IPlayerSearchController playerSearchController = new GClass1873(profile);
				searchController = playerSearchController;
			}
		}

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		public override void vmethod_1(GClass3088 operation, [CanBeNull] Callback callback)
		{
			base.vmethod_1(operation, callback);

			InventoryPacket packet = new()
			{
				HasItemControllerExecutePacket = true
			};

			GClass1164 writer = new();
			writer.WritePolymorph(operation.ToDescriptor());
			packet.ItemControllerExecutePacket = new()
			{
				CallbackId = operation.Id,
				OperationBytes = writer.ToArray()
			};

			CoopBot.PacketSender.InventoryPackets.Enqueue(packet);
		}

		public override SearchContentOperation vmethod_2(SearchableItemClass item)
		{
			return new GClass3126(method_12(), this, PlayerSearchController, Profile, item);
		}
	}
}
