﻿using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Utils;
using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    /// <summary>
    /// CoopHandler handles most of the spawning logic in the raid, e.g. other players or AI. It also handles the extraction of the local player.
    /// </summary>
    public class CoopHandler : MonoBehaviour
    {
        #region Fields/Properties
        /// <summary>
        /// Reference to the local <see cref="CoopGame"/> instance
        /// </summary>
        public CoopGame LocalGameInstance { get; internal set; }
        /// <summary>
        /// The ID of the raid session
        /// </summary>
        public string ServerId { get; internal set; }
        /// <summary>
        /// Reference to the local <see cref="CoopPlayer"/> player
        /// </summary>
        public CoopPlayer MyPlayer { get; internal set; }
        /// <summary>
        /// If the <see cref="CoopHandler"/> should sync and spawn profiles during <see cref="Update"/>
        /// </summary>
        public bool ShouldSync { get; internal set; }

        /// <summary>
        /// Dictionary of key = <see cref="CoopPlayer.NetId"/>, value = <see cref="CoopPlayer"/>
        /// </summary>
        public Dictionary<int, CoopPlayer> Players { get; internal set; }
        /// <summary>
        /// All human players in the form of <see cref="CoopPlayer"/> (excluding headless if not playing as a headless client)
        /// </summary>
        public List<CoopPlayer> HumanPlayers { get; internal set; }
        /// <summary>
        /// The amount of human players (including headless)
        /// </summary>
        public int AmountOfHumans { get; internal set; }
        /// <summary>
        /// List of <see cref="CoopPlayer.NetId"/>s that have extracted
        /// </summary>
        public List<int> ExtractedPlayers { get; internal set; }

        private ManualLogSource logger;
        /// <summary>
        /// List of all queued players by <see cref="CoopPlayer.NetId"/>
        /// </summary>
        private List<int> queuedPlayers;
        /// <summary>
        /// Queue of <see cref="SpawnObject"/> containing all players to spawn
        /// </summary>
        private Queue<SpawnObject> spawnQueue;
        private bool isClient;
        private float charSyncCounter;
        private bool requestQuitGame;
        #endregion

        public static bool TryGetCoopHandler(out CoopHandler coopHandler)
        {
            coopHandler = null;
            IFikaNetworkManager networkManager = Singleton<IFikaNetworkManager>.Instance;
            if (networkManager != null)
            {
                coopHandler = networkManager.CoopHandler;
                return true;
            }

            return false;
        }

        public static string GetServerId()
        {
            IFikaNetworkManager networkManager = Singleton<IFikaNetworkManager>.Instance;
            if (networkManager != null && networkManager.CoopHandler != null)
            {
                return networkManager.CoopHandler.ServerId;
            }

            return FikaBackendUtils.GroupId;
        }

        public void CleanUpForTransit()
        {
            ShouldSync = false;
            Players.Clear();
            HumanPlayers.Clear();
            AmountOfHumans = 1;
            ExtractedPlayers.Clear();
            queuedPlayers.Clear();
            spawnQueue.Clear();
            LocalGameInstance = null;
            requestQuitGame = false;
            if (isClient)
            {
                Singleton<FikaClient>.Instance.FikaClientWorld = null;
            }
        }

        protected void Awake()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("CoopHandler");
            spawnQueue = new(50);
            queuedPlayers = [];
            Players = [];
            HumanPlayers = [];
            AmountOfHumans = 1;
            ExtractedPlayers = [];
            ShouldSync = false;
        }

        protected void Start()
        {
            if (FikaBackendUtils.IsClient)
            {
                isClient = true;
                charSyncCounter = 0f;
                return;
            }

            isClient = false;
        }

        protected void Update()
        {
            if (LocalGameInstance == null)
            {
                return;
            }

            if (ShouldSync)
            {
                if (spawnQueue.Count > 0)
                {
                    SpawnPlayer(spawnQueue.Dequeue());
                }

                if (!isClient)
                {
                    charSyncCounter += Time.unscaledDeltaTime;
                    int waitTime = LocalGameInstance.Status == GameStatus.Started ? 20 : 5;
                    if (charSyncCounter > waitTime)
                    {
                        charSyncCounter = 0f;
                        if (Players == null)
                        {
                            return;
                        }

                        SyncPlayersWithClients();
                    }
                }
            }

            ProcessQuitting();
        }

        private void SyncPlayersWithClients()
        {
            CharacterSyncPacket characterSyncPacket = new(Players);
            Singleton<FikaServer>.Instance.SendDataToAll(ref characterSyncPacket, DeliveryMethod.ReliableOrdered);
        }

        protected void OnDestroy()
        {
            Players.Clear();
            HumanPlayers.Clear();
        }

        public void ReInitInteractables()
        {
            Singleton<GameWorld>.Instance.World_0.method_0(null);
        }

        public EQuitState GetQuitState()
        {
            EQuitState quitState = EQuitState.None;

            if (LocalGameInstance == null)
            {
                return quitState;
            }

            if (MyPlayer == null)
            {
                return quitState;
            }

            // Check alive status
            if (!MyPlayer.HealthController.IsAlive)
            {
                quitState = EQuitState.Dead;
            }

            // Extractions
            if (LocalGameInstance.ExtractedPlayers.Contains(MyPlayer.NetId))
            {
                quitState = EQuitState.Extracted;
            }

            return quitState;
        }

        /// <summary>
        /// This handles the ways of exiting the active game session
        /// </summary>
        private void ProcessQuitting()
        {
            if (!FikaPlugin.ExtractKey.Value.IsDown())
            {
                return;
            }

            EQuitState quitState = GetQuitState();
            if (quitState != EQuitState.None && !requestQuitGame)
            {
                //Log to both the in-game console as well as into the BepInEx logfile
                ConsoleScreen.Log($"{FikaPlugin.ExtractKey.Value} pressed, attempting to extract!");
                logger.LogInfo($"{FikaPlugin.ExtractKey.Value} pressed, attempting to extract!");

                requestQuitGame = true;
                CoopGame coopGame = CoopGame.Instance;

                // If you are the server / host
                if (!isClient)
                {
                    if (coopGame.ExitStatus == ExitStatus.Transit && HumanPlayers.Count <= 1)
                    {
                        coopGame.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.ExitStatus, coopGame.ExitLocation, 0);
                        return;
                    }
                    // A host needs to wait for the team to extract or die!
                    if ((Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount > 0) && quitState != EQuitState.None)
                    {
                        NotificationManagerClass.DisplayWarningNotification(LocaleUtils.HOST_CANNOT_EXTRACT.Localized());
                        requestQuitGame = false;
                        return;
                    }
                    else if (Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0
                        && Singleton<FikaServer>.Instance.TimeSinceLastPeerDisconnected > DateTime.Now.AddSeconds(-5)
                        && Singleton<FikaServer>.Instance.HasHadPeer)
                    {
                        NotificationManagerClass.DisplayWarningNotification(LocaleUtils.HOST_WAIT_5_SECONDS.Localized());
                        requestQuitGame = false;
                        return;
                    }
                    else
                    {
                        coopGame.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.ExitStatus, MyPlayer.ActiveHealthController.IsAlive ? coopGame.ExitLocation : null, 0);
                    }
                }
                else
                {
                    coopGame.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.ExitStatus, MyPlayer.ActiveHealthController.IsAlive ? coopGame.ExitLocation : null, 0);
                }
                return;
            }
        }

        private async void SpawnPlayer(SpawnObject spawnObject)
        {
            if (spawnObject.Profile == null)
            {
                logger.LogError("SpawnPlayer: Profile was null!");
                queuedPlayers.Remove(spawnObject.NetId);
                return;
            }

            foreach (IPlayer player in Singleton<GameWorld>.Instance.AllPlayersEverExisted)
            {
                if (player.ProfileId == spawnObject.Profile.ProfileId)
                {
                    return;
                }
            }

            int playerId = LocalGameInstance.method_15();

            ResourceKey[] allPrefabPaths = spawnObject.Profile.GetAllPrefabPaths(!spawnObject.IsAI).ToArray();
            if (allPrefabPaths.Length == 0)
            {
                logger.LogError($"SpawnPlayer::{spawnObject.Profile.Info.Nickname}::PrefabPaths are empty!");
                return;
            }

            await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid,
                PoolManagerClass.AssemblyType.Local, allPrefabPaths, JobPriorityClass.Low).ContinueWith(x =>
                {
                    if (x.IsFaulted)
                    {
                        logger.LogError($"SpawnPlayer::{spawnObject.Profile.Info.Nickname}::Load Failed");
                    }
                    else if (x.IsCanceled)
                    {
                        logger.LogError($"SpawnPlayer::{spawnObject.Profile.Info.Nickname}::Load Cancelled");
                    }
                });

            ObservedCoopPlayer otherPlayer = SpawnObservedPlayer(spawnObject);

            if (!spawnObject.IsAlive)
            {
                otherPlayer.OnDead(EDamageType.Undefined);
                otherPlayer.NetworkHealthController.IsAlive = false;
            }

            if (FikaBackendUtils.IsServer)
            {
                if (LocalGameInstance != null)
                {
                    BotsController botController = LocalGameInstance.BotsController;
                    if (botController != null)
                    {
                        // Start Coroutine as botController might need a while to start sometimes...
#if DEBUG
                        logger.LogInfo("Starting AddClientToBotEnemies routine.");
#endif
                        StartCoroutine(AddClientToBotEnemies(botController, otherPlayer));
                    }
                    else
                    {
                        logger.LogError("botController was null when trying to add player to enemies!");
                    }
                }
                else
                {
                    logger.LogError("LocalGameInstance was null when trying to add player to enemies!");
                }
            }

            queuedPlayers.Remove(spawnObject.NetId);
        }

        public void QueueProfile(Profile profile, byte[] healthByteArray, Vector3 position, int netId, bool isAlive, bool isAI, MongoID firstId, ushort firstOperationId, bool isZombie,
            EHandsControllerType controllerType = EHandsControllerType.None, string itemId = null)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                return;
            }

            foreach (IPlayer player in gameWorld.AllPlayersEverExisted)
            {
                if (player.ProfileId == profile.ProfileId)
                {
                    return;
                }
            }

            if (queuedPlayers.Contains(netId))
            {
                return;
            }

            queuedPlayers.Add(netId);
#if DEBUG
            logger.LogInfo($"Queueing profile: {profile.Nickname}, {profile.ProfileId}");
#endif
            SpawnObject spawnObject = new(profile, position, isAlive, isAI, netId, firstId, firstOperationId, isZombie);
            if (controllerType != EHandsControllerType.None)
            {
                spawnObject.ControllerType = controllerType;
                if (!string.IsNullOrEmpty(itemId))
                {
                    spawnObject.ItemId = itemId;
                }
            }
            if (healthByteArray != null)
            {
                spawnObject.HealthBytes = healthByteArray;
            }
            spawnQueue.Enqueue(spawnObject);
        }

        private ObservedCoopPlayer SpawnObservedPlayer(SpawnObject spawnObject)
        {
            bool isAi = spawnObject.IsAI;
            Profile profile = spawnObject.Profile;
            Vector3 position = spawnObject.Position;
            int netId = spawnObject.NetId;
            MongoID firstId = spawnObject.CurrentId;
            ushort firstOperationId = spawnObject.FirstOperationId;
            bool isHeadlessProfile = !isAi && profile.IsHeadlessProfile();
            byte[] healthBytes = spawnObject.HealthBytes;
            bool isZombie = spawnObject.IsZombie;

            // Handle null bytes on players
            if (!isAi && (spawnObject.HealthBytes == null || spawnObject.HealthBytes.Length == 0))
            {
                healthBytes = profile.Health.SerializeHealthInfo();
            }

            // Check for GClass increments on filter
            ObservedCoopPlayer otherPlayer = ObservedCoopPlayer.CreateObservedPlayer(LocalGameInstance.GameWorld_0, netId, position, Quaternion.identity, "Player",
                isAi ? "Bot_" : $"Player_{profile.Nickname}_", EPointOfView.ThirdPerson, profile, healthBytes, isAi,
                EUpdateQueue.Update, Player.EUpdateMode.Manual, Player.EUpdateMode.Auto,
                BackendConfigAbstractClass.Config.CharacterController.ObservedPlayerMode, FikaGlobals.GetOtherPlayerSensitivity, FikaGlobals.GetOtherPlayerSensitivity,
                GClass1627.Default, firstId, firstOperationId, isZombie).Result;

            if (otherPlayer == null)
            {
                return null;
            }

            if (!isHeadlessProfile)
            {
                Singleton<IFikaNetworkManager>.Instance.ObservedCoopPlayers.Add(otherPlayer);
            }

            otherPlayer.NetId = netId;
#if DEBUG
            logger.LogInfo($"SpawnObservedPlayer: {profile.Nickname} spawning with NetId {netId}");
#endif
            if (!isAi)
            {
                AmountOfHumans++;
            }

            if (!Players.ContainsKey(netId))
            {
                Players.Add(netId, otherPlayer);
            }
            else
            {
                logger.LogError($"Trying to add {otherPlayer.Profile.Nickname} to list of players but it was already there!");
            }

            if (!isAi && !isHeadlessProfile && !HumanPlayers.Contains(otherPlayer))
            {
                HumanPlayers.Add(otherPlayer);
            }

            foreach (CoopPlayer player in Players.Values)
            {
                if (player is not ObservedCoopPlayer)
                {
                    continue;
                }

                Collider playerCollider = otherPlayer.GetCharacterControllerCommon().GetCollider();
                Collider otherCollider = player.GetCharacterControllerCommon().GetCollider();

                if (playerCollider != null && otherCollider != null)
                {
                    EFTPhysicsClass.IgnoreCollision(playerCollider, otherCollider);
                }
            }

            if (isAi)
            {
                if (profile.Info.Side is EPlayerSide.Bear or EPlayerSide.Usec)
                {
                    Item backpack = profile.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;
                    backpack?.GetAllItems()
                        .Where(i => i != backpack)
                        .ExecuteForEach(i => i.SpawnedInSession = true);

                    // We still want DogTags to be 'FiR'
                    Item item = otherPlayer.Inventory.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
                    if (item != null)
                    {
                        item.SpawnedInSession = true;
                    }
                }
            }
            else if (profile.Info.Side != EPlayerSide.Savage)// Make Player PMC items are all not 'FiR'
            {
                profile.SetSpawnedInSession(false);
            }

            otherPlayer.InitObservedPlayer(isHeadlessProfile);

#if DEBUG
            logger.LogInfo($"CreateLocalPlayer::{profile.Info.Nickname}::Spawned.");
#endif

            EHandsControllerType controllerType = spawnObject.ControllerType;
            string itemId = spawnObject.ItemId;
            bool isStationary = spawnObject.IsStationary;
            if (controllerType != EHandsControllerType.None)
            {
                if (controllerType != EHandsControllerType.Empty && string.IsNullOrEmpty(itemId))
                {
                    logger.LogError($"CreateLocalPlayer: ControllerType was not Empty but itemId was null! ControllerType: {controllerType}");
                }
                else
                {
                    otherPlayer.SpawnHandsController(controllerType, itemId, isStationary);
                }
            }
            return otherPlayer;
        }

        private IEnumerator AddClientToBotEnemies(BotsController botController, LocalPlayer playerToAdd)
        {
            CoopGame coopGame = LocalGameInstance;

            logger.LogInfo($"AddClientToBotEnemies: " + playerToAdd.Profile.Nickname);

            while (coopGame.Status != GameStatus.Running && !botController.IsEnable)
            {
                yield return null;
            }

            while (coopGame.BotsController.BotSpawner == null)
            {
                yield return null;
            }

#if DEBUG
            logger.LogInfo($"Adding Client {playerToAdd.Profile.Nickname} to enemy list");
#endif
            botController.AddActivePLayer(playerToAdd);

            bool found = false;

            for (int i = 0; i < botController.BotSpawner.PlayersCount; i++)
            {
                if (botController.BotSpawner.GetPlayer(i) == playerToAdd)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
#if DEBUG
                logger.LogInfo($"Verified that {playerToAdd.Profile.Nickname} was added to the enemy list.");
#endif
                yield break;
            }

            logger.LogError($"Failed to add {playerToAdd.Profile.Nickname} to the enemy list.");
        }

        public void CheckIds(List<int> playerIds, List<int> missingIds)
        {
            foreach (int netId in playerIds)
            {
                if (!queuedPlayers.Contains(netId) && !Players.ContainsKey(netId))
                {
                    missingIds.Add(netId);
                }
            }
        }

        /// <summary>
        /// The state your character or game is in to Quit.
        /// </summary>
        public enum EQuitState
        {
            None = -1,
            Dead,
            Extracted
        }

        public class SpawnObject(Profile profile, Vector3 position, bool isAlive, bool isAI, int netId, MongoID currentId, ushort firstOperationId, bool isZombie)
        {
            public Profile Profile = profile;
            public Vector3 Position = position;
            public bool IsAlive = isAlive;
            public bool IsAI = isAI;
            public int NetId = netId;
            public MongoID CurrentId = currentId;
            public ushort FirstOperationId = firstOperationId;
            public EHandsControllerType ControllerType;
            public string ItemId;
            public bool IsStationary;
            public byte[] HealthBytes;
            public bool IsZombie = isZombie;
        }
    }
}