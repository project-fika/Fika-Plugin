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
        protected FikaPlayer _fikaPlayer;

        public static FikaClientKnifeController Create(FikaPlayer player, KnifeComponent item)
        {
            FikaClientKnifeController controller = smethod_9<FikaClientKnifeController>(player, item);
            controller._fikaPlayer = player;
            return controller;
        }

        public override void ExamineWeapon()
        {
            base.ExamineWeapon();

            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Knife,
                SubPacket = KnifePacket.FromValue(true, false, false, false)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }

        public override bool MakeKnifeKick()
        {
            bool knifeKick = base.MakeKnifeKick();

            if (knifeKick)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.Knife,
                    SubPacket = KnifePacket.FromValue(false, true, false, false)
                };
                _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
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
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.Knife,
                    SubPacket = KnifePacket.FromValue(false, false, true, false)
                };
                _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
            }

            return alternateKnifeKick;
        }

        public override void BrakeCombo()
        {
            base.BrakeCombo();

            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Knife,
                SubPacket = KnifePacket.FromValue(false, false, false, true)
            };
            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
    }
}
