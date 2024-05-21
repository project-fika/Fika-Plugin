// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.ClientClasses
{
    internal class CoopClientKnifeController : EFT.Player.KnifeController
    {
        public CoopPlayer coopPlayer;

        private void Awake()
        {
            coopPlayer = GetComponent<CoopPlayer>();
        }

        public static CoopClientKnifeController Create(CoopPlayer player, KnifeComponent item)
        {
            return smethod_8<CoopClientKnifeController>(player, item);
        }

        public override void ExamineWeapon()
        {
            base.ExamineWeapon();

            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasKnifePacket = true,
                KnifePacket = new()
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
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    HasKnifePacket = true,
                    KnifePacket = new()
                    {
                        Kick = knifeKick
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
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    HasKnifePacket = true,
                    KnifePacket = new()
                    {
                        AltKick = alternateKnifeKick
                    }
                });
            }

            return alternateKnifeKick;
        }

        public override void BrakeCombo()
        {
            base.BrakeCombo();

            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasKnifePacket = true,
                KnifePacket = new()
                {
                    BreakCombo = true
                }
            });
        }
    }
}
