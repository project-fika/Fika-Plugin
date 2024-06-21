using BepInEx.Logging;
using Comfort.Common;
using Fika.Core.Coop.Components;
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

        public static void CreateFikaGameObject()
        {
            FikaGameObject = new GameObject("FikaGameObject");
            Object.DontDestroyOnLoad(FikaGameObject);
            logger.LogInfo("FikaGameObject has been created!");
        }
        
        public static void CreateNetManager(bool isServer)
        {
            if (FikaGameObject == null)
            {
                CreateFikaGameObject();
            }

            if (isServer)
            {
                FikaServer server = FikaGameObject.AddComponent<FikaServer>();
                Singleton<FikaServer>.Create(server);
                logger.LogInfo("FikaServer has started!");
            }
            else
            {
                FikaClient client = FikaGameObject.AddComponent<FikaClient>();
                Singleton<FikaClient>.Create(client);
                logger.LogInfo("FikaClient has started!");
            }
        }

        public static void CreatePingingClient()
        {
            if (FikaGameObject == null)
            {
                CreateFikaGameObject();
            }

            FikaPingingClient pingingClient = FikaGameObject.AddComponent<FikaPingingClient>();
            Singleton<FikaPingingClient>.Create(pingingClient);
            logger.LogInfo("FikaPingingClient has started!");
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

        public static void DestroyPingingClient()
        {
            if(FikaGameObject != null)
            {
                Singleton<FikaPingingClient>.Instance.StopKeepAliveRoutine();
                Singleton<FikaPingingClient>.Instance.NetClient.Stop();
                Singleton<FikaPingingClient>.TryRelease(Singleton<FikaPingingClient>.Instance);
                logger.LogInfo("Destroyed FikaPingingClient");
            }
        }

        public static Task InitNetManager(bool isServer)
        {
            if (FikaGameObject != null)
            {
                if (isServer)
                {
                    FikaServer server = Singleton<FikaServer>.Instance;
                    if (!server.Started)
                    {
                        return server.Init();
                    }
                    return Task.CompletedTask;
                }
                else
                {
                    FikaClient client = Singleton<FikaClient>.Instance;
                    if (!client.Started)
                    {
                        client.Init();
                    }
                    return Task.CompletedTask;
                }
            }

            logger.LogError("InitNetManager: FikaGameObject was null!");
            return Task.CompletedTask;
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

        public static void StartPinger()
        {
            if (FikaGameObject != null)
            {
                FikaPinger fikaPinger = FikaGameObject.AddComponent<FikaPinger>();
                fikaPinger.StartPingRoutine();
            }
        }

        public static void StopPinger()
        {
            if (FikaGameObject != null)
            {
                FikaPinger fikaPinger = FikaGameObject.GetComponent<FikaPinger>();
                if (fikaPinger != null)
                {
                    Object.Destroy(fikaPinger);
                }
                else
                {
                    logger.LogError("StopPinger: Could not find FikaPinger!");
                }
            }
        }
    }
}