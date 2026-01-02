using System;
using Comfort.Common;
using EFT;
using EFT.Console.Core;
using EFT.UI;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Debug;
using HarmonyLib;
using static Fika.Core.Networking.Packets.Debug.CommandPacket;

namespace Fika.Core.ConsoleCommands;

public class FikaCommands
{
#if DEBUG
    [ConsoleCommand("bring", "", null, "Teleports all AI to yourself as the host", [])]
    public static void Bring()
    {
        if (!CheckForGame())
        {
            return;
        }

        if (CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            if (!FikaBackendUtils.IsServer)
            {
                var myId = Singleton<IFikaNetworkManager>.Instance.NetId;
                CommandPacket commandPacket = new(ECommandType.Bring)
                {
                    NetId = myId
                };

                Singleton<FikaClient>.Instance.SendData(ref commandPacket, DeliveryMethod.ReliableOrdered);
                return;
            }

            var count = 0;
            var targetPosition = coopHandler.MyPlayer.Transform;
            foreach (var player in coopHandler.Players.Values)
            {
                if (player.IsAI && player.HealthController.IsAlive)
                {
                    count++;
                    player.Teleport(targetPosition.Original.position + targetPosition.Original.forward * 2);
                }
            }

            LogInfo($"Teleported {count} AI to requester.");

        }
        else
        {
            LogWarning("Could not find CoopHandler.");
        }
    }

    /// <summary>
    /// Used during replication
    /// </summary>
    /// <param name="netId"></param>
    public static void BringReplicated(int netId)
    {
        LogInfo($"Received bring request from {netId}");

        if (!CheckForGame())
        {
            return;
        }

        if (CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            var count = 0;
            BifacialTransform targetPosition;
            if (coopHandler.Players.TryGetValue(netId, out var target))
            {
                targetPosition = target.Transform;
            }
            else
            {
                LogError($"Could not find player with netId {netId}");
                return;
            }

            foreach (var player in coopHandler.Players.Values)
            {
                if (player.IsAI && player.HealthController.IsAlive)
                {
                    count++;
                    player.Teleport(targetPosition.Original.position + targetPosition.Original.forward * 2);
                }
            }

            var output = $"Teleported {count} AI to requester.";
            LogInfo(output);
        }
        else
        {
            LogWarning("Could not find CoopHandler.");
        }
    }

    [ConsoleCommand("god", "", null, "Set god mode on/off", [])]
    public static void God([ConsoleArgument(false, "true or false to toggle god mode")] bool state)
    {
        if (!CheckForGame())
        {
            return;
        }

        if (CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            var value = state ? 0 : 1;
            coopHandler.MyPlayer.ActiveHealthController.SetDamageCoeff(value);
            if (value == 0)
            {
                LogInfo("God mode on");
            }
            else
            {
                LogInfo("God mode off");
            }
        }
        else
        {
            LogWarning("Could not find CoopHandler.");
        }
    }

    [ConsoleCommand("extract", "", null, "Extract from raid", [])]
    public static void Extract()
    {
        var game = Singleton<IFikaGame>.Instance;
        if (game == null)
        {
            LogWarning("You are not in a game.");
            return;
        }

        if (game is not CoopGame coopGame)
        {
            LogError("Game mode was not CoopGame");
            return;
        }

        if (coopGame.Status != GameStatus.Started)
        {
            LogWarning("Game is not running.");
            return;
        }

        var localPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;

        coopGame.Extract(localPlayer, null);
    }

    [ConsoleCommand("despawnAllAi", "", null, "Despawns all AI bots", [])]
    public static void DespawnAllAI()
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame != null)
        {
            if (!FikaBackendUtils.IsServer)
            {
                CommandPacket commandPacket = new(ECommandType.DespawnAI);

                Singleton<FikaClient>.Instance.SendData(ref commandPacket, DeliveryMethod.ReliableOrdered);
                return;
            }

            if (CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                foreach (var bot in fikaGame.GameController.Bots.Values)
                {
                    if (bot.AIData.BotOwner == null)
                    {
                        continue;
                    }

                    LogInfo($"Despawning: {bot.Profile.Nickname}");

                    (fikaGame.GameController as HostGameController).DespawnBot(coopHandler, bot);
                }
                return;
            }

            LogError("Could not find CoopHandler!");
        }
    }

    [ConsoleCommand("stoptimer", "", null, "Stops the game timer", [])]
    public static void StopTimer()
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null || fikaGame is not CoopGame coopGame)
        {
            LogError("Game was null or not a CoopGame");
            return;
        }

        if (coopGame != null)
        {
            if (coopGame.GameTimer.Status == GameTimerClass.EGameTimerStatus.Stopped)
            {
                LogError("GameTimer is already stopped at: " + coopGame.GameTimer.PastTime.ToString());
                return;
            }
            coopGame.GameTimer.TryStop();
            if (coopGame.GameTimer.Status == GameTimerClass.EGameTimerStatus.Stopped)
            {
                LogInfo("GameTimer stopped at: " + coopGame.GameTimer.PastTime.ToString());
            }
        }
    }

    [ConsoleCommand("goToBTR", "", null, "Teleports you to the BTR if active", [])]
    public static void GoToBTR()
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null || fikaGame is not CoopGame coopGame)
        {
            LogError("Game was null or not a CoopGame");
            return;
        }

        if (coopGame != null)
        {
            var gameWorld = coopGame.GameWorld_0;
            if (gameWorld != null)
            {
                if (gameWorld.BtrController != null)
                {
                    var btrTransform = Traverse.Create(gameWorld.BtrController.BtrView).Field<Transform>("_cachedTransform").Value;
                    if (btrTransform != null)
                    {
                        var myPlayer = gameWorld.MainPlayer;
                        if (myPlayer != null)
                        {
                            myPlayer.Teleport(btrTransform.position + (Vector3.forward * 3));
                        }
                    }
                }
                else
                {
                    LogWarning("There is no BTRController active!");
                }
            }
        }
    }

    [ConsoleCommand("spawnItem", "", null, "Spawns an item from a templateId")]
    public static void SpawnItem([ConsoleArgument("", "The templateId to spawn an item from")] string templateId,
        [ConsoleArgument(1, "The amount to spawn if the item can stack")] int amount = 1)
    {
        if (!CheckForGame())
        {
            return;
        }

        var gameWorld = Singleton<GameWorld>.Instance;
        var player = (FikaPlayer)gameWorld.MainPlayer;
        if (!player.HealthController.IsAlive)
        {
            LogError("You cannot spawn an item while dead!");
            return;
        }

        var itemFactory = Singleton<ItemFactoryClass>.Instance;
        if (itemFactory == null)
        {
            LogError("ItemFactory was null!");
            return;
        }

        var item = itemFactory.GetPresetItem(templateId);
        if (amount > 1 && item.StackMaxSize > 1)
        {
            item.StackObjectsCount = Mathf.Clamp(amount, 1, item.StackMaxSize);
        }
        else
        {
            item.StackObjectsCount = 1;
        }
        FikaGlobals.SpawnItemInWorld(item, player);

        SpawnItemPacket packet = new()
        {
            NetId = player.NetId,
            Item = item
        };

        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Based on SSH's TarkyMenu command
    /// </summary>
    /// <param name="wildSpawnType"></param>
    /// <param name="number"></param>
    [ConsoleCommand("spawnNPC", "", null, "Spawn NPC with specified WildSpawnType")]
    public static void SpawnNPC([ConsoleArgument("assault", "The WildSpawnType to spawn (use help for a list)")] string wildSpawnType, [ConsoleArgument(1, "The amount of AI to spawn")] int amount)
    {
        if (string.IsNullOrEmpty(wildSpawnType) || wildSpawnType.ToLower() == "help")
        {
            foreach (WildSpawnType availableSpawnType in Enum.GetValues(typeof(WildSpawnType)))
            {
                LogInfo(availableSpawnType.ToString());
            }
            LogInfo("- Available WildSpawnType options below -");
            return;
        }

        if (amount <= 0)
        {
            LogInfo($"Invalid number: {amount}. Please enter a valid, positive integer.");
            return;
        }

        if (!Enum.TryParse(wildSpawnType, true, out WildSpawnType selectedSpawnType))
        {
            LogInfo($"Invalid WildSpawnType: {wildSpawnType}");
            return;
        }

        if (!CheckForGame())
        {
            return;
        }

        if (!FikaBackendUtils.IsServer)
        {
            CommandPacket commandPacket = new(ECommandType.SpawnAI)
            {
                SpawnType = wildSpawnType,
                SpawnAmount = amount
            };

            Singleton<FikaClient>.Instance.SendData(ref commandPacket, DeliveryMethod.ReliableOrdered);
            return;
        }

        BotWaveDataClass newBotData = new()
        {
            BotsCount = amount,
            Side = EPlayerSide.Savage,
            SpawnAreaName = "",
            Time = 0f,
            WildSpawnType = selectedSpawnType,
            IsPlayers = false,
            Difficulty = BotDifficulty.easy,
            ChanceGroup = 100f,
            WithCheckMinMax = false
        };

        var botController = (Singleton<IFikaGame>.Instance.GameController as HostGameController).BotsController;
        if (botController == null)
        {
            LogError("BotsController was null!");
            return;
        }

        botController.BotSpawner.ActivateBotsByWave(newBotData);
        LogInfo($"SpawnNPC completed, requested {amount} of {wildSpawnType}");
    }

    [ConsoleCommand("spawnAirdrop", "", null, "Spawns an airdrop")]
    public static void SpawnAirdrop()
    {
        if (!CheckForGame())
        {
            return;
        }

        if (!FikaBackendUtils.IsServer)
        {
            CommandPacket commandPacket = new(ECommandType.SpawnAirdrop);

            Singleton<FikaClient>.Instance.SendData(ref commandPacket, DeliveryMethod.ReliableOrdered);
            return;
        }

        var gameWorld = (FikaHostGameWorld)Singleton<GameWorld>.Instance;
        if (gameWorld == null)
        {
            LogError("GameWorld does not exist or you are a client!");
            return;
        }

        var serverAirdropManager = gameWorld.ClientSynchronizableObjectLogicProcessor.ServerAirdropManager;
        if (serverAirdropManager == null)
        {
            LogError("ServerAirdropManager was null!");
            return;
        }

        if (!serverAirdropManager.Boolean_0)
        {
            LogError("Airdrops are disabled!");
            return;
        }

        var dropPoints = serverAirdropManager.List_2;
        if (dropPoints != null && dropPoints.Count > 0)
        {
            var templateId = serverAirdropManager.String_0;
            serverAirdropManager.method_5(serverAirdropManager.Single_0);
            gameWorld.InitAirdrop(templateId, true, serverAirdropManager.method_6());
            serverAirdropManager.String_0 = null;
            dropPoints.Clear();
            LogInfo("Started airdrop");
            return;
        }

        serverAirdropManager.method_5(serverAirdropManager.Single_0);
        gameWorld.InitAirdrop();
        LogInfo("Started airdrop");
    }
#endif

    [ConsoleCommand("debug", "", null, "Toggle debug window", [])]
    public static void Debug(bool state)
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null || fikaGame is not CoopGame coopGame)
        {
            LogError("Game was null or not a CoopGame");
            return;
        }

        if (coopGame.Status != GameStatus.Started)
        {
            LogWarning("Game is not running.");
            return;
        }

        coopGame.ToggleDebug(state);
    }

    [ConsoleCommand("openAdminUI", "", null, "Opens the Admin UI as the raid host", [])]
    public static void OpenAdminUI()
    {
        if (!CheckForGame())
        {
            return;
        }

        if (!FikaBackendUtils.IsServer)
        {
            LogWarning("You are not the host!");
            return;
        }

        Singleton<PreloaderUI>.Instance.Console.Close();
        Singleton<FikaServer>.Instance.ToggleAdminUI();
    }

    [ConsoleCommand("clear", "", null, "Clears the console output", [])]
    public static void Clear()
    {
        Singleton<PreloaderUI>.Instance.Console.Clear();
    }

    private static bool CheckForGame()
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null)
        {
            LogError("Game was null");
            return false;
        }

        if (fikaGame.GameController.GameInstance.Status != GameStatus.Started)
        {
            LogWarning("Game is not running.");
            return false;
        }

        return true;
    }

    private static void LogInfo(string message)
    {
        ConsoleScreen.Log(message);
        FikaGlobals.LogInfo(message);
    }

    private static void LogWarning(string message)
    {
        ConsoleScreen.LogWarning(message);
        FikaGlobals.LogInfo(message);
    }

    private static void LogError(string message)
    {
        ConsoleScreen.LogError(message);
        FikaGlobals.LogError(message);
    }
}
