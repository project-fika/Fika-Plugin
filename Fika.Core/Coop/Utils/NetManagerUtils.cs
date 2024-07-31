using BepInEx.Logging;
using Comfort.Common;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
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
            GameObject.DontDestroyOnLoad(FikaGameObject);
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

        /// <summary>
        /// Sends a <see cref="INetSerializable"/> reliable unordered packet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet"></param>
        public static void SendPacket<T>(ref T packet) where T : INetSerializable
        {
            if (FikaBackendUtils.IsServer)
            {
                FikaServer server = Singleton<FikaServer>.Instance;
                if (server != null)
                {
                    server.Writer.Reset();
                    server.SendDataToAll(server.Writer, ref packet, DeliveryMethod.ReliableUnordered);
                    return;
                }
            }

            FikaClient client = Singleton<FikaClient>.Instance;
            if (client != null)
            {
                client.Writer.Reset();
                client.SendData(client.Writer, ref packet, DeliveryMethod.ReliableUnordered);
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
                    FikaServer server = Singleton<FikaServer>.Instance;
                    try
                    {
                        server.NetServer.Stop();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("DestroyNetManager: " + ex.Message);
                    }
                    Singleton<FikaServer>.TryRelease(server);
                    GameObject.Destroy(server);
                    logger.LogInfo("Destroyed FikaServer");
                }
                else
                {
                    FikaClient client = Singleton<FikaClient>.Instance;
                    try
                    {
                        client.NetClient.Stop();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("DestroyNetManager: " + ex.Message);
                    }
                    Singleton<FikaClient>.TryRelease(client);
                    GameObject.Destroy(client);
                    logger.LogInfo("Destroyed FikaClient");
                }
            }
        }

        public static void DestroyPingingClient()
        {
            if (FikaGameObject != null)
            {
                FikaPingingClient pingingClient = Singleton<FikaPingingClient>.Instance;
                pingingClient.StopKeepAliveRoutine();
                pingingClient.NetClient.Stop();
                Singleton<FikaPingingClient>.TryRelease(pingingClient);
                GameObject.Destroy(pingingClient);
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
            throw new NullReferenceException("FikaGameObject was null");
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
                    GameObject.Destroy(fikaPinger);
                }
                else
                {
                    logger.LogError("StopPinger: Could not find FikaPinger!");
                }
            }
        }
    }
}