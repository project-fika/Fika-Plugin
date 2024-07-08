using Comfort.Common;
using EFT;
using EFT.Console.Core;
using EFT.UI;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;

namespace Fika.Core.Console
{
    public class FikaCommands
    {
#if DEBUG
        [ConsoleCommand("bring", "", null, "Teleports all AI to yourself as the host", [])]
        public static void Bring()
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            if (coopGame == null)
            {
                ConsoleScreen.LogWarning("You are not in a game.");
                return;
            }

            if (coopGame.Status != GameStatus.Started)
            {
                ConsoleScreen.LogWarning("Game is not running.");
                return;
            }

            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                if (FikaBackendUtils.IsServer)
                {
                    int count = 0;
                    foreach (CoopPlayer player in coopHandler.Players.Values)
                    {
                        BifacialTransform myPosition = coopHandler.MyPlayer.Transform;
                        if (player.IsAI && player.HealthController.IsAlive)
                        {
                            count++;
                            player.Teleport(myPosition.Original.position + myPosition.Original.forward * 2);
                        }
                    }
                    ConsoleScreen.Log($"Teleported {count} AI to host.");
                }
                else
                {
                    ConsoleScreen.LogWarning("You are not the host");
                }
            }
            else
            {
                ConsoleScreen.LogWarning("Could not find CoopHandler.");
            }
        }

        [ConsoleCommand("god", "", null, "Set god mode on/off", [])]
        public static void God([ConsoleArgument(false, "true or false to toggle god mode")] bool state)
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            if (coopGame == null)
            {
                ConsoleScreen.LogWarning("You are not in a game.");
                return;
            }

            if (coopGame.Status != GameStatus.Started)
            {
                ConsoleScreen.LogWarning("Game is not running.");
                return;
            }

            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                int value = state ? 0 : 1;
                coopHandler.MyPlayer.ActiveHealthController.SetDamageCoeff(value);
                if (value == 0)
                {
                    ConsoleScreen.Log("God mode on");
                }
                else
                {
                    ConsoleScreen.Log("God mode off");
                }
            }
            else
            {
                ConsoleScreen.LogWarning("Could not find CoopHandler.");
            }
        }

        [ConsoleCommand("extract", "", null, "Extract from raid", [])]
        public static void Extract()
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            if (coopGame == null)
            {
                ConsoleScreen.LogWarning("You are not in a game.");
                return;
            }

            if (coopGame.Status != GameStatus.Started)
            {
                ConsoleScreen.LogWarning("Game is not running.");
                return;
            }

            CoopPlayer localPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;

            coopGame.Extract(localPlayer, null);
        }
#endif

        [ConsoleCommand("debug", "", null, "Toggle debug window", [])]
        public static void Debug(bool state)
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            if (coopGame == null)
            {
                ConsoleScreen.LogWarning("You are not in a game.");
                return;
            }

            if (coopGame.Status != GameStatus.Started)
            {
                ConsoleScreen.LogWarning("Game is not running.");
                return;
            }

            coopGame.ToggleDebug(state);
        }

        [ConsoleCommand("clear", "", null, "Clears the console output", [])]
        public static void Clear()
        {
            Singleton<PreloaderUI>.Instance.Console.Clear();
        }
    }
}
