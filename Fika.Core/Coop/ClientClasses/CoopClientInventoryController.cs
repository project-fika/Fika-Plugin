using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using JetBrains.Annotations;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.IO;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientInventoryController(Player player, Profile profile, bool examined) : Player.PlayerOwnerInventoryController(player, profile, examined)
    {
        public override bool HasDiscardLimits => false;

        ManualLogSource BepInLogger { get; set; } = BepInEx.Logging.Logger.CreateLogSource(nameof(CoopClientInventoryController));

        private readonly Player Player = player;
        private CoopPlayer CoopPlayer => (CoopPlayer)Player;

        public override void CallMalfunctionRepaired(Weapon weapon)
        {
            base.CallMalfunctionRepaired(weapon);
            if (!Player.IsAI && (bool)Singleton<SharedGameSettingsClass>.Instance.Game.Settings.MalfunctionVisability)
            {
                MonoBehaviourSingleton<PreloaderUI>.Instance.MalfunctionGlow.ShowGlow(BattleUIMalfunctionGlow.GlowType.Repaired, force: true, method_44());
            }
        }

        public override void Execute(GClass2837 operation, [CanBeNull] Callback callback)
        {
            base.Execute(operation, callback);

            // Do not replicate picking up quest items, throws an error on the other clients
            if (operation is GClass2839 pickupOperation)
            {
                if (pickupOperation.Item.Template.QuestItem)
                {
                    return;
                }
            }

            InventoryPacket packet = new()
            {
                HasItemControllerExecutePacket = true
            };

            using MemoryStream memoryStream = new();
            using BinaryWriter binaryWriter = new(memoryStream);
            binaryWriter.WritePolymorph(GClass1632.FromInventoryOperation(operation, false));
            byte[] opBytes = memoryStream.ToArray();
            packet.ItemControllerExecutePacket = new()
            {
                CallbackId = operation.Id,
                OperationBytes = opBytes,
                InventoryId = ID
            };

            CoopPlayer.PacketSender?.InventoryPackets?.Enqueue(packet);
        }
    }
}
