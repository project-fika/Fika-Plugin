﻿using EFT;
using EFT.Game.Spawning;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;
using System.Reflection;
using Il2CppPlayers = Il2CppSystem.Collections.Generic.IEnumerable<EFT.IPlayer>;

namespace Fika.Core.Main.Patches;

public class PlayersCollectionExtension_ExceptAI_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayersCollectionExtension)
            .GetMethod(nameof(PlayersCollectionExtension.ExceptAI));
    }

    [PatchPrefix]
    public static bool Prefix(Il2CppPlayers persons, ref Il2CppPlayers __result)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        if (persons == null)
        {
            return true;
        }

        var humanPlayers = FikaGlobals.NetworkManager.CoopHandler.HumanPlayers;
        Il2CppSystem.Collections.Generic.List<IPlayer> players = new(humanPlayers.Count);
        
        for (var i = 0; i < humanPlayers.Count; i++)
        {
            players.Add(humanPlayers[i]);
        }

        __result = players;
        return false;
    }
}
