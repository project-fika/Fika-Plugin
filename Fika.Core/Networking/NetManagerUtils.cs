using BepInEx.Logging;
using Comfort.Common;
using Dissonance.Networking;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.UI.Custom;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Networking
{
    public static class NetManagerUtils
    {
        public static GameObject FikaGameObject;

        private static readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("NetManagerUtils");
        private static CancellationTokenSource _pingTokenSource;

        public static void CreateFikaGameObject()
        {
            FikaGameObject = new GameObject("FikaGameObject");
            UnityEngine.Object.DontDestroyOnLoad(FikaGameObject);
            _logger.LogInfo("FikaGameObject has been created!");
        }

        public static void CreateNetManager(bool isServer)
        {
            // Required for VOIP
            TrafficCounters.Reset();
            FikaChatUIScript.IsActive = false;

            if (FikaGameObject == null)
            {
                CreateFikaGameObject();
            }

            if (isServer)
            {
                FikaServer server = FikaGameObject.AddComponent<FikaServer>();
                Singleton<FikaServer>.Create(server);
                _logger.LogInfo("FikaServer has started!");
                Singleton<IFikaNetworkManager>.Create(server);
                return;
            }

            FikaClient client = FikaGameObject.AddComponent<FikaClient>();
            Singleton<FikaClient>.Create(client);
            _logger.LogInfo("FikaClient has started!");
            Singleton<IFikaNetworkManager>.Create(client);

        }

        public static void CreatePingingClient()
        {
            if (FikaGameObject == null)
            {
                CreateFikaGameObject();
            }

            FikaPingingClient pingingClient = FikaGameObject.AddComponent<FikaPingingClient>();
            Singleton<FikaPingingClient>.Create(pingingClient);
            _logger.LogInfo("FikaPingingClient has started!");
        }

        public static void DestroyNetManager(bool isServer)
        {
            if (FikaBackendUtils.IsTransit)
            {
                Singleton<IFikaNetworkManager>.Instance.CoopHandler.CleanUpForTransit();
                if (isServer)
                {
                    FikaServer server = Singleton<FikaServer>.Instance;
                    server.RaidInitialized = false;
                    server.ReadyClients = 0;
                    return;
                }

                FikaClient client = Singleton<FikaClient>.Instance;
                client.HostReady = false;
                client.HostLoaded = false;
                client.ReadyClients = 0;
                return;
            }

            FikaBackendUtils.MatchingType = EMatchmakerType.Single;

            if (FikaGameObject != null)
            {
                GCManager gcManager = FikaGameObject.GetComponent<GCManager>();
                if (gcManager != null)
                {
                    UnityEngine.Object.Destroy(gcManager);
                }

                if (isServer)
                {
                    FikaServer server = Singleton<FikaServer>.Instance;
                    if (!Singleton<IFikaNetworkManager>.TryRelease(server))
                    {
                        _logger.LogError("Unable to release Server from Singleton!");
                    }
                    try
                    {
                        server.PrintStatistics();
                        server.NetServer.Stop();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("DestroyNetManager: " + ex.Message);
                    }
                    Singleton<FikaServer>.TryRelease(server);
                    UnityEngine.Object.Destroy(server);
                    _logger.LogInfo("Destroyed FikaServer");
                    return;
                }

                FikaClient client = Singleton<FikaClient>.Instance;
                if (!Singleton<IFikaNetworkManager>.TryRelease(client))
                {
                    _logger.LogError("Unable to release Client from Singleton!");
                }
                try
                {
                    client.PrintStatistics();
                    client.NetClient.Stop();
                }
                catch (Exception ex)
                {
                    _logger.LogError("DestroyNetManager: " + ex.Message);
                }
                Singleton<FikaClient>.TryRelease(client);
                UnityEngine.Object.Destroy(client);
                _logger.LogInfo("Destroyed FikaClient");
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
                UnityEngine.Object.Destroy(pingingClient);
                _logger.LogInfo("Destroyed FikaPingingClient");
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
                        server.Init();
                    }
                    FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerCreatedEvent(server));
                    return Task.CompletedTask;
                }

                FikaClient client = Singleton<FikaClient>.Instance;
                if (!client.Started)
                {
                    client.Init();
                }
                FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerCreatedEvent(client));
                return Task.CompletedTask;
            }

            _logger.LogError("InitNetManager: FikaGameObject was null!");
            throw new NullReferenceException("FikaGameObject was null");
        }

        public static Task SetupGameVariables(CoopPlayer coopPlayer)
        {
            _logger.LogInfo("Setting up game variables...");
            Singleton<IFikaNetworkManager>.Instance.SetupGameVariables(coopPlayer);
            return Task.CompletedTask;
        }

        public static void StartPinger()
        {
#if DEBUG
            _logger.LogInfo("Starting pinger");
#endif
            _pingTokenSource = new();
            _ = Task.Run(() => PingBackend(_pingTokenSource.Token));
        }

        public static void StopPinger()
        {
#if DEBUG
            _logger.LogInfo("Stopping pinger");
#endif
            _pingTokenSource.Cancel();
        }

        private static async Task PingBackend(CancellationToken token)
        {
            PingRequest pingRequest = new();
            while (true)
            {
                if (token.IsCancellationRequested)
                {
#if DEBUG
                    _logger.LogInfo("Pinger stopped successfully");
#endif
                    return;
                }

                await FikaRequestHandler.UpdatePing(pingRequest);
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        public static Task CreateCoopHandler()
        {
            if (FikaBackendUtils.IsTransit)
            {
                return Task.CompletedTask;
            }

            _logger.LogInfo("Creating CoopHandler...");
            IFikaNetworkManager networkManager = Singleton<IFikaNetworkManager>.Instance;
            if (networkManager != null)
            {
                if (FikaGameObject != null)
                {
                    CoopHandler coopHandler = FikaGameObject.AddComponent<CoopHandler>();
                    networkManager.CoopHandler = coopHandler;

                    if (!string.IsNullOrEmpty(FikaBackendUtils.GroupId))
                    {
                        coopHandler.ServerId = FikaBackendUtils.GroupId;
                        return Task.CompletedTask;
                    }

                    UnityEngine.Object.Destroy(coopHandler);
                    _logger.LogError("No ServerId found, deleting CoopHandler!");
                    throw new MissingReferenceException("No Server Id found");
                }
            }

            return Task.FromException(new NullReferenceException("CreateCoopHandler: IFikaNetworkManager or FikaGameObject was null"));
        }
    }
}