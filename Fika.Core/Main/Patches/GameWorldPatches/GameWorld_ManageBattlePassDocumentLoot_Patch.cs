using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.World;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using static Fika.Core.Networking.Packets.World.RequestSubPackets;

namespace Fika.Core.Main.Patches.GameWorldPatches;

public sealed class GameWorld_ManageBattlePassDocumentLoot_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.ManageBattlePassDocumentLoot));
    }

    [PatchPrefix]
    public static bool Prefix(Player player)
    {
        if (FikaBackendUtils.IsServer)
        {
            return true;
        }

        if (player is not FikaPlayer fikaPlayer)
        {
            return false;
        }

        // Documents are cloned on spawn, so a client rolling its own would hold items the host has never seen
        // and could not resolve a pick up against. The host rolls and spawns them and sends them back instead.
        RequestPacket packet = new()
        {
            Type = ERequestSubPacketType.BattlePassDocuments,
            RequestSubPacket = new BattlePassDocumentsRequest
            {
                NetId = fikaPlayer.NetId,
                Remaining = player.Profile.GetBattlePassDocumentLimitRemainingForRaid()
            }
        };

        Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        return false;
    }
}
