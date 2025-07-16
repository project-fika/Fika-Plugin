// © 2025 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Main.ClientClasses
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

            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Knife,
                SubPacket = new KnifePacket()
                {
                    Examine = true
                }
            };
            player.PacketSender.SendPacket(ref packet);
        }

        public override bool MakeKnifeKick()
        {
            bool knifeKick = base.MakeKnifeKick();

            if (knifeKick)
            {
                WeaponPacket packet = new()
                {
                    NetId = player.NetId,
                    Type = EFirearmSubPacketType.Knife,
                    SubPacket = new KnifePacket()
                    {
                        Kick = true
                    }
                };
                player.PacketSender.SendPacket(ref packet);
            }

            return knifeKick;
        }

        public override bool MakeAlternativeKick()
        {
            bool alternateKnifeKick = base.MakeAlternativeKick();

            if (alternateKnifeKick)
            {
                WeaponPacket packet = new()
                {
                    NetId = player.NetId,
                    Type = EFirearmSubPacketType.Knife,
                    SubPacket = new KnifePacket()
                    {
                        AltKick = true
                    }
                };
                player.PacketSender.SendPacket(ref packet);
            }

            return alternateKnifeKick;
        }

        public override void BrakeCombo()
        {
            base.BrakeCombo();

            WeaponPacket packet = new()
            {
                NetId = player.NetId,
                Type = EFirearmSubPacketType.Knife,
                SubPacket = new KnifePacket()
                {
                    BreakCombo = true
                }
            };
            player.PacketSender.SendPacket(ref packet);
        }
    }
}
