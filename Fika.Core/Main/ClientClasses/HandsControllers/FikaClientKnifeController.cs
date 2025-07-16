// © 2025 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.FirearmController;
using static Fika.Core.Networking.Packets.FirearmController.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Main.ClientClasses.HandsControllers
{
    public class FikaClientKnifeController : EFT.Player.KnifeController
    {
        protected FikaPlayer _coopPlayer;

        public static FikaClientKnifeController Create(FikaPlayer player, KnifeComponent item)
        {
            FikaClientKnifeController controller = smethod_9<FikaClientKnifeController>(player, item);
            controller._coopPlayer = player;
            return controller;
        }

        public override void ExamineWeapon()
        {
            base.ExamineWeapon();

            WeaponPacket packet = new()
            {
                NetId = _coopPlayer.NetId,
                Type = EFirearmSubPacketType.Knife,
                SubPacket = new KnifePacket()
                {
                    Examine = true
                }
            };
            _coopPlayer.PacketSender.SendPacket(ref packet);
        }

        public override bool MakeKnifeKick()
        {
            bool knifeKick = base.MakeKnifeKick();

            if (knifeKick)
            {
                WeaponPacket packet = new()
                {
                    NetId = _coopPlayer.NetId,
                    Type = EFirearmSubPacketType.Knife,
                    SubPacket = new KnifePacket()
                    {
                        Kick = true
                    }
                };
                _coopPlayer.PacketSender.SendPacket(ref packet);
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
                    NetId = _coopPlayer.NetId,
                    Type = EFirearmSubPacketType.Knife,
                    SubPacket = new KnifePacket()
                    {
                        AltKick = true
                    }
                };
                _coopPlayer.PacketSender.SendPacket(ref packet);
            }

            return alternateKnifeKick;
        }

        public override void BrakeCombo()
        {
            base.BrakeCombo();

            WeaponPacket packet = new()
            {
                NetId = _coopPlayer.NetId,
                Type = EFirearmSubPacketType.Knife,
                SubPacket = new KnifePacket()
                {
                    BreakCombo = true
                }
            };
            _coopPlayer.PacketSender.SendPacket(ref packet);
        }
    }
}
