// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Coop.ClientClasses
{
	internal class CoopClientKnifeController : EFT.Player.KnifeController
	{
		protected CoopPlayer player;

		public static CoopClientKnifeController Create(CoopPlayer player, KnifeComponent item)
		{
			CoopClientKnifeController controller = smethod_9<CoopClientKnifeController>(player, item);
			controller.player = player;
			return controller;
		}

		public override void ExamineWeapon()
		{
			base.ExamineWeapon();

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Knife,
				SubPacket = new KnifePacket()
				{
					Examine = true
				}
			});
		}

		public override bool MakeKnifeKick()
		{
			bool knifeKick = base.MakeKnifeKick();

			if (knifeKick)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.Knife,
					SubPacket = new KnifePacket()
					{
						Kick = true
					}
				});
			}

			return knifeKick;
		}

		public override bool MakeAlternativeKick()
		{
			bool alternateKnifeKick = base.MakeAlternativeKick();

			if (alternateKnifeKick)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.Knife,
					SubPacket = new KnifePacket()
					{
						AltKick = true
					}
				});
			}

			return alternateKnifeKick;
		}

		public override void BrakeCombo()
		{
			base.BrakeCombo();

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Knife,
				SubPacket = new KnifePacket()
				{
					BreakCombo = true
				}
			});
		}
	}
}
