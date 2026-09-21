using System;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using Dissonance.Networking;
using Fika.Core.Bundles;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.UI.Custom;

namespace Fika.Core.Networking;

public static class NetManagerUtils
{
    public static GameObject FikaGameObject;

    private static readonly ManualLogSource _logger = Logger.CreateLogSource("NetManagerUtils");
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

        CreateLoadingScreenUI();

        if (isServer)
        {
            var server = FikaGameObject.AddComponent<FikaServer>();
            Singleton<FikaServer>.Create(server);
            _logger.LogInfo("FikaServer has created!");
            FikaGlobals.NetworkManager = server;
            return;
        }

        var client = FikaGameObject.AddComponent<FikaClient>();
        Singleton<FikaClient>.Create(client);
        _logger.LogInfo("FikaClient has created!");
        FikaGlobals.NetworkManager = client;
    }

    public static void CreateLoadingScreenUI()
    {
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.gameObject.SetActive(true);
            return;
        }

        var loadingPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.LoadingScreenUI);
        var loadingUi = GameObject.Instantiate(loadingPrefab, FikaGameObject.transform);
        LoadingScreenUI.Instance = LoadingScreenUI.Create(loadingUi);
    }

    /// <summary>
    /// Disables the <see cref="LoadingScreenUI"/> and clears all data in it
    /// </summary>
    public static void DisableLoadingScreenUI()
    {
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.ClearData();
            LoadingScreenUI.Instance.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Destroys the <see cref="LoadingScreenUI"/>
    /// </summary>
    public static void DestroyLoadingScreenUI()
    {
        if (LoadingScreenUI.Instance != null)
        {
            GameObject.Destroy(LoadingScreenUI.Instance);
            LoadingScreenUI.Instance = null;
        }
    }

    public static Task<FikaPingingClient> CreatePingingClient()
    {
        if (FikaGameObject == null)
        {
            CreateFikaGameObject();
        }

        var pingingClient = new FikaPingingClient();
        _logger.LogInfo("FikaPingingClient has started!");
        return Task.FromResult(pingingClient);
    }

    public static void DestroyNetManager(bool isServer)
    {
        NetworkUtils.ResetReaderAndWriter();

        if (FikaBackendUtils.IsTransit)
        {
            CreateLoadingScreenUI();
            LoadingScreenUI.Instance.ReInitAfterTransit();

            FikaGlobals.NetworkManager.CoopHandler.CleanUpForTransit();
            if (isServer)
            {
                var server = Singleton<FikaServer>.Instance;
                server.HostReady = false;
                server.RaidInitialized = false;
                server.ReadyClients = 0;
                server.LocationReceived = false;
                return;
            }

            var client = Singleton<FikaClient>.Instance;
            client.HostReady = false;
            client.HostLoaded = false;
            client.ReadyClients = 0;

            return;
        }

        DestroyLoadingScreenUI();

        FikaBackendUtils.ClientType = EClientType.None;

        if (FikaGameObject != null)
        {
            if (isServer)
            {
                var server = Singleton<FikaServer>.Instance;
                FikaGlobals.NetworkManager = null;
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

            var client = Singleton<FikaClient>.Instance;
            FikaGlobals.NetworkManager = null;
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

    public static async Task InitNetManager(bool isServer)
    {
        if (FikaGameObject != null)
        {
            if (isServer)
            {
                var server = Singleton<FikaServer>.Instance;
                if (!server.Started)
                {
                    await server.Init();
                }
                FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerCreatedEvent(server));
                return;
            }

            var client = Singleton<FikaClient>.Instance;
            if (!client.Started)
            {
                await client.Init();
            }
            FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerCreatedEvent(client));
            return;
        }

        _logger.LogError("InitNetManager: FikaGameObject was null!");
        throw new NullReferenceException("FikaGameObject was null");
    }

    public static Task SetupGameVariables(FikaPlayer fikaPlayer)
    {
        _logger.LogInfo("Setting up game variables...");
        FikaGlobals.NetworkManager.SetupGameVariables(fikaPlayer);
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
        var networkManager = FikaGlobals.NetworkManager;
        if (networkManager != null)
        {
            if (FikaGameObject != null)
            {
                var coopHandler = FikaGameObject.AddComponent<CoopHandler>();
                networkManager.CoopHandler = coopHandler;

                if (!string.IsNullOrEmpty(FikaBackendUtils.GroupId))
                {
                    coopHandler.ServerId = FikaBackendUtils.GroupId;
                    return Task.CompletedTask;
                }

                UnityEngine.Object.Destroy(coopHandler);
                _logger.LogError("No ServerId found, deleting CoopHandler!");
                throw new InvalidOperationException("No Server Id found");
            }
        }

        return Task.FromException(new NullReferenceException("CreateCoopHandler: IFikaNetworkManager or FikaGameObject was null"));
    }
}