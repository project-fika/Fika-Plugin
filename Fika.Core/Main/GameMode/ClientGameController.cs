using System;
using System.Collections;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Bots;
using EFT.Counters;
using EFT.Game.Spawning;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using EFT.UI;
using EFT.Weather;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.FreeCamera;
using Fika.Core.Main.Patches.Overrides;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using static LocationSettingsClass;

namespace Fika.Core.Main.GameMode;

public class ClientGameController(IFikaGame game, EUpdateQueue updateQueue, GameWorld gameWorld, ISession session)
    : BaseGameController(game, updateQueue, gameWorld, session)
{
    public bool ExfiltrationReceived { get; set; }
    public bool HasReceivedLoot { get; set; }
    public bool InteractablesInitialized { get; set; }

    public override IEnumerator WaitForHostInit(int timeBeforeDeployLocal)
    {
        Logger.LogInfo("Waiting for host init");
        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_HOST_FINISH_INIT.Localized());

        var client = Singleton<FikaClient>.Instance;
        WaitForEndOfFrame waitForEndOfFrame = new();
        do
        {
            yield return waitForEndOfFrame;
        } while (!client.HostReady);
        LootItems = null;

        Logger.LogInfo("Host init complete");
    }

    public override async Task WaitForHostToStart()
    {
        await base.WaitForHostToStart();

        GameObject startButton = null;
        if (FikaBackendUtils.IsHeadlessRequester || FikaPlugin.Instance.AnyoneCanStartRaid)
        {
            startButton = CreateStartButton() ?? throw new NullReferenceException("Start button could not be created!");
            if (FikaPlugin.Instance.Settings.DevMode.Value)
            {
                Logger.LogWarning("DevMode is enabled, skipping wait...");
                NotificationManagerClass.DisplayMessageNotification("DevMode enabled, starting automatically...", iconType: EFT.Communications.ENotificationIconType.Note);
                var fikaClient = Singleton<FikaClient>.Instance ?? throw new NullReferenceException("CreateStartButton::FikaClient was null!");
                InformationPacket devModePacket = new()
                {
                    RequestStart = true
                };
                fikaClient.SendData(ref devModePacket, DeliveryMethod.ReliableOrdered);
            }
        }
        var client = Singleton<FikaClient>.Instance;
        InformationPacket packet = new();
        client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        do
        {
            client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            await Task.Delay(250);
        }
        while (!RaidStarted);

        if (startButton != null)
        {
            GameObject.Destroy(startButton);
        }
    }

    public override async Task WaitForOtherPlayersToLoad()
    {
        float expectedPlayers = Singleton<IFikaNetworkManager>.Instance.PlayerAmount;
        if (FikaBackendUtils.IsHeadlessGame)
        {
            expectedPlayers--;
        }
#if DEBUG
        Logger.LogWarning("Client: Waiting for coopHandler.AmountOfHumans < expected players, expected: " + expectedPlayers);
#endif
        var client = Singleton<FikaClient>.Instance;
        do
        {
            await Task.Delay(100);
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)_coopHandler.AmountOfHumans / expectedPlayers);
        } while (_coopHandler.AmountOfHumans < expectedPlayers);

        InformationPacket packet = new()
        {
            ReadyPlayers = 1
        };

        client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
#if DEBUG
        Logger.LogWarning("Client: Waiting for client.ReadyClients < expected players, expected: " + expectedPlayers);
#endif
        if (FikaBackendUtils.IsHeadlessGame)
        {
            expectedPlayers++;
        }

        do
        {
            await Task.Delay(100);
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_OTHER_PLAYERS.Localized(), (float)client.ReadyClients / expectedPlayers);
        } while (client.ReadyClients < expectedPlayers);
    }

    public override async Task GenerateWeathers()
    {
        if (WeatherController.Instance != null)
        {
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_INIT_WEATHER.Localized());
            Logger.LogInfo("Generating and initializing weather...");
            await GetWeather();
            WeatherController.Instance.method_0(WeatherClasses);
        }
    }

    /// <summary>
    /// Gets the weather from the host
    /// </summary>
    /// <returns></returns>
    private async Task GetWeather()
    {
        RequestPacket packet = new()
        {
            Type = ERequestSubPacketType.Weather
        };

        var client = Singleton<FikaClient>.Instance;
        client.SendData(ref packet, DeliveryMethod.ReliableOrdered);

        while (!WeatherReady)
        {
            await Task.Delay(1000);
            if (!WeatherReady)
            {
                client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    public override IEnumerator CountdownScreen(Profile profile, string profileId)
    {
        yield return base.CountdownScreen(profile, profileId);
        _localPlayer.PacketSender.Init();
    }

    public override async Task ReceiveSpawnPoint(Profile profile)
    {
        var spawnTogether = RaidSettings.PlayersSpawnPlace == EPlayersSpawnPlace.SamePlace;
        if (!spawnTogether)
        {
            Logger.LogInfo("Using random spawn points!");
            NotificationManagerClass.DisplayMessageNotification(LocaleUtils.RANDOM_SPAWNPOINTS.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert);

            if (!IsServer)
            {
                CreateSpawnSystem(profile);
            }
            return;
        }

        if (!IsServer && spawnTogether)
        {
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_SPAWN_INFO.Localized());

            RequestPacket packet = new()
            {
                Type = ERequestSubPacketType.SpawnPoint
            };
            var client = Singleton<FikaClient>.Instance;

            do
            {
                client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
                await Task.Delay(1000);
                if (string.IsNullOrEmpty(InfiltrationPoint))
                {
                    await Task.Delay(2000);
                }
            } while (string.IsNullOrEmpty(InfiltrationPoint));

            Logger.LogInfo($"Retrieved infiltration point '{InfiltrationPoint}' from server");
        }
    }

    public override void CreateSpawnSystem(Profile profile)
    {
        _spawnPoints = SpawnPointManagerClass.CreateFromScene(new DateTime?(EFTDateTimeClass.LocalDateTimeFromUnixTime(Location.UnixDateTime)),
                                Location.SpawnPointParams);
        var spawnSafeDistance = (Location.SpawnSafeDistanceMeters > 0) ? Location.SpawnSafeDistanceMeters : 100;
        SpawnSettingsStruct settings = new(Location.MinDistToFreePoint, Location.MaxDistToFreePoint, Location.MaxBotPerZone, spawnSafeDistance, Location.NoGroupSpawn, Location.OneTimeSpawn);
        SpawnSystem = SpawnSystemCreatorClass.CreateSpawnSystem(settings, FikaGlobals.GetApplicationTime, Singleton<GameWorld>.Instance, null, _spawnPoints);
        _spawnPoint = SpawnSystem.SelectSpawnPoint(ESpawnCategory.Player, profile.Info.Side, null, null, null, null, profile.Id);
        InfiltrationPoint = string.IsNullOrEmpty(_spawnPoint.Infiltration) ? "MissingInfiltration" : _spawnPoint.Infiltration;
        ClientSpawnPosition = _spawnPoint.Position;
        ClientSpawnRotation = _spawnPoint.Rotation;
    }

    public async Task WaitForHostToLoad()
    {
        var client = Singleton<FikaClient>.Instance;

        Logger.LogInfo("Waiting for host to init");

        InformationPacket packet = new();
        do
        {
            _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_WAIT_FOR_HOST_INIT.Localized());
            client.SendData(ref packet, DeliveryMethod.ReliableOrdered);

            await Task.Delay(1000);
        } while (!client.HostLoaded);
    }

    public override async Task InitializeLoot(LocationSettingsClass.Location location)
    {
        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_LOOT.Localized());
        if (!FikaBackendUtils.IsReconnect)
        {
            await RetrieveLootFromServer(true, location);
        }
        else
        {
            await RetrieveLootFromServer(false, location);
        }
        location.Loot = LootItems;
    }

    private async Task RetrieveLootFromServer(bool register,
        LocationSettingsClass.Location location)
    {
        var client = Singleton<FikaClient>.Instance;
        WorldLootPacket packet = new()
        {
            Data = []
        };
        do
        {
            client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (!HasReceivedLoot && LootItems.Count < 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        } while (!HasReceivedLoot);

        if (register)
        {
            RegisterPlayerRequest request = new(0, location.Id, 0);
            await FikaRequestHandler.RegisterPlayer(request);
        }
    }

    public async Task InitExfils()
    {
        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_EXFIL_DATA.Localized());
        var client = Singleton<FikaClient>.Instance;
        RequestPacket request = new()
        {
            Type = ERequestSubPacketType.Exfiltration
        };

        do
        {
            client.SendData(ref request, DeliveryMethod.ReliableOrdered);
            await Task.Delay(1000);
        } while (!ExfiltrationReceived);
    }

    public async Task InitInteractables()
    {
        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_RETRIEVE_INTERACTABLES.Localized());
        var client = Singleton<FikaClient>.Instance;
        InteractableInitPacket packet = new(true);

        do
        {
            client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            await Task.Delay(1000);
            if (!InteractablesInitialized)
            {
                await Task.Delay(2000);
            }
        } while (!InteractablesInitialized);
    }

    public override void SetupEventsAndExfils(Player player)
    {
        if (_fikaGame is not CoopGame coopGame)
        {
            throw new NullReferenceException("Could not find CoopGame");
        }

        coopGame.GameTimer.Start(GameTime, SessionTime);
        coopGame.Spawn();

        var skills = coopGame.Profile_0.Skills.Skills;
        var skillsLength = skills.Length;
        for (var i = 0; i < skillsLength; i++)
        {
            skills[i].SetPointsEarnedInSession(0f, false);
        }

        coopGame.Profile_0.Info.EntryPoint = InfiltrationPoint;
        Logger.LogInfo("[CLIENT] SpawnPosition: " + ClientSpawnPosition + ", InfiltrationPoint: " + InfiltrationPoint);

        var exfilController = ExfiltrationControllerClass.Instance;
        var isScav = player.Side is EPlayerSide.Savage;
        ExfiltrationPoint[] exfilPoints;
        SecretExfiltrationPoint[] secretExfilPoints;
        ExfiltrationControllerClass.Instance.InitSecretExfils(player);

        if (isScav)
        {
            exfilController.ScavExfiltrationClaim(player.Position, player.ProfileId, player.Profile.FenceInfo.AvailableExitsCount);
            var mask = exfilController.GetScavExfiltrationMask(player.ProfileId);
            exfilPoints = exfilController.ScavExfiltrationClaim(mask, player.ProfileId);
            secretExfilPoints = ExfiltrationControllerClass.Instance.GetScavSecretExits();
        }
        else
        {
            exfilPoints = exfilController.EligiblePoints(coopGame.Profile_0);
            secretExfilPoints = ExfiltrationControllerClass.Instance.SecretEligiblePoints();
        }

        coopGame.GameUi.TimerPanel.SetTime(EFTDateTimeClass.UtcNow,
            coopGame.Profile_0.Info.Side, coopGame.GameTimer.EscapeTimeSeconds(),
            exfilPoints, secretExfilPoints);

        if (TransitControllerAbstractClass.Exist(out ClientTransitController transitController))
        {
            transitController.Init();
        }

        if (Location.EventTrapsData != null)
        {
            _gameWorld.SyncModule = new();
        }

        ExfilManager.Run(exfilPoints, secretExfilPoints);

        coopGame.Status = GameStatus.Started;

        ConsoleScreen.ApplyStartCommands();
    }

    public override void Extract(FikaPlayer player, ExfiltrationPoint exfiltrationPoint, TransitPoint transitPoint = null)
    {
        if (_fikaGame is not CoopGame coopGame)
        {
            throw new NullReferenceException("CoopGame was missing");
        }

        var preloaderUI = Singleton<PreloaderUI>.Instance;
        _localTriggerZones = [.. player.TriggerZones];

        player.ClientMovementContext.SetGravity(false);
        var position = player.Position;
        position.y += 500;
        player.Teleport(position);

        if (coopGame.ExitStatus == ExitStatus.MissingInAction)
        {
            NotificationManagerClass.DisplayMessageNotification(LocaleUtils.PLAYER_MIA.Localized(), iconType: EFT.Communications.ENotificationIconType.Alert, textColor: Color.red);
        }

        if (player.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
        {
            sharedQuestController.ToggleQuestSharing(false);
        }

        var matchEndConfig = Singleton<BackendConfigSettingsClass>.Instance.Experience.MatchEnd;
        if (player.Profile.EftStats.SessionCounters.GetAllInt([CounterTag.Exp]) < matchEndConfig.SurvivedExpRequirement && coopGame.PastTime < matchEndConfig.SurvivedTimeRequirement)
        {
            coopGame.ExitStatus = ExitStatus.Runner;
        }

        if (exfiltrationPoint != null)
        {
            exfiltrationPoint.Disable();

            if (exfiltrationPoint.HasRequirements && exfiltrationPoint.TransferItemRequirement != null
                && exfiltrationPoint.TransferItemRequirement.Met(player, exfiltrationPoint) && player.IsYourPlayer)
            {
                // Seems to already be handled by SPT so we only add it visibly
                player.Profile.EftStats.SessionCounters.AddDouble(0.2, [CounterTag.FenceStanding, EFenceStandingSource.ExitStanding]);
            }
        }

        if (player.Side == EPlayerSide.Savage)
        {
            // Seems to already be handled by SPT so we only add it visibly
            player.Profile.EftStats.SessionCounters.AddDouble(0.01, [CounterTag.FenceStanding, EFenceStandingSource.ExitStanding]);
        }

        var transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController != null && transitPoint != null)
        {
            if (transitController.alreadyTransits.TryGetValue(player.ProfileId, out var data))
            {
                coopGame.ExitStatus = ExitStatus.Transit;
                coopGame.ExitLocation = transitPoint.parameters.name;
                FikaBackendUtils.IsTransit = true;
            }
            else
            {
                Logger.LogError("Exit was transit, but could not find alreadyTransits data");
            }
        }

        if (_coopHandler != null)
        {
            try // This is to allow clients to extract if they lose connection
            {
                Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.ClientExtract,
                    ClientExtract.FromValue(player.NetId), true);
            }
            catch
            {

            }

            var fikaPlayer = player;
            coopGame.ExtractedPlayers.Add(fikaPlayer.NetId);
            _coopHandler.ExtractedPlayers.Add(fikaPlayer.NetId);
            _coopHandler.Players.Remove(fikaPlayer.NetId);

            preloaderUI.StartBlackScreenShow(2f, 2f, () => preloaderUI.FadeBlackScreen(2f, -2f));

            player.ActiveHealthController.SetDamageCoeff(0);
            player.ActiveHealthController.DamageMultiplier = 0;
            player.ActiveHealthController.DisableMetabolism();
            player.ActiveHealthController.PauseAllEffects();

            _extractRoutine = coopGame.StartCoroutine(ExtractRoutine(player, coopGame));

            // Prevents players from looting after extracting
            CurrentScreenSingletonClass.Instance.CloseAllScreensForced();

            // Destroys session timer
            if (TimeManager != null)
            {
                UnityEngine.Object.Destroy(TimeManager);
            }
            if (coopGame.GameUi.TimerPanel.enabled)
            {
                coopGame.GameUi.TimerPanel.Close();
            }

            if (FikaPlugin.Instance.Settings.AutoExtract.Value || FikaBackendUtils.IsTransit)
            {
                coopGame.Stop(_localPlayer.ProfileId, coopGame.ExitStatus, _localPlayer.ActiveHealthController.IsAlive ? coopGame.ExitLocation : null, 0);
            }
        }
        else
        {
            throw new NullReferenceException("Extract: CoopHandler was null!");
        }
    }

    public override async Task StartBotSystemsAndCountdown(BotControllerSettings controllerSettings, GameWorld gameWorld)
    {
        LoadingScreenUI.Instance.UpdateAndBroadcast(80f);

        if (Location.Id == "laboratory")
        {
            Logger.LogInfo("Location is 'Laboratory', skipping weather generation");
            Season = ESeason.Summer;
            OfflineRaidSettingsMenuPatch_Override.UseCustomWeather = false;
        }
        else
        {
            await GenerateWeathers();
        }

        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());

        gameWorld.RegisterRestrictableZones();
        gameWorld.RegisterBorderZones();

#if DEBUG
        Logger.LogWarning("Starting " + nameof(BaseGameController.WaitForOtherPlayersToLoad));
#endif
        await WaitForOtherPlayersToLoad();

        _abstractGame.SetMatchmakerStatus(LocaleUtils.UI_FINISHING_RAID_INIT.Localized());
        Logger.LogInfo("All players are loaded, continuing...");

        // Add FreeCamController to GameWorld GameObject
        var freeCamController = gameWorld.gameObject.AddComponent<FreeCameraController>();
        Singleton<FreeCameraController>.Create(freeCamController);

        await SetupRaidCode();

        LoadingScreenUI.Instance.UpdateAndBroadcast(85f);

        Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal = Math.Max(Singleton<BackendConfigSettingsClass>.Instance.TimeBeforeDeployLocal, 3);
    }
}
