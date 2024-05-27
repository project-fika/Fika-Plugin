using BepInEx.Logging;
using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Utils
{
    public static class NetManagerUtils
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("NetManagerUtils");
        public static GameObject FikaGameObject;

        public static async Task CreateNetManager(bool isServer)
        {
            if (FikaGameObject == null)
            {
                FikaGameObject = new GameObject("FikaGameObject");
                Object.DontDestroyOnLoad(FikaGameObject);
                logger.LogInfo("FikaGameObject has been created!");
            }

            if (isServer)
            {
                FikaServer server = FikaGameObject.AddComponent<FikaServer>();

                while (!server.ServerReady)
                {
                    await Task.Delay(100);
                }
                logger.LogInfo("FikaServer has started!");
            }
            else
            {
                FikaClient client = FikaGameObject.AddComponent<FikaClient>();

                while (!client.ClientReady)
                {
                    await Task.Delay(100);
                }
                logger.LogInfo("FikaClient has started!");
            }
        }

        public static void DestroyNetManager(bool isServer)
        {
            if (FikaGameObject != null)
            {
                if (isServer)
                {
                    Singleton<FikaServer>.Instance.NetServer.Stop();
                    Singleton<FikaServer>.TryRelease(Singleton<FikaServer>.Instance);
                    logger.LogInfo("Destroyed FikaServer");
                }
                else
                {
                    Singleton<FikaClient>.Instance.NetClient.Stop();
                    Singleton<FikaClient>.TryRelease(Singleton<FikaClient>.Instance);
                    logger.LogInfo("Destroyed FikaClient");
                }
            }
        }

        public static Task SetupGameVariables(bool isServer, CoopPlayer coopPlayer)
        {
            if (isServer)
            {
                Singleton<FikaServer>.Instance.SetupGameVariables(coopPlayer);
            }
            else
            {
                Singleton<FikaClient>.Instance.SetupGameVariables(coopPlayer);
            }

            return Task.CompletedTask;
        }
    }
}