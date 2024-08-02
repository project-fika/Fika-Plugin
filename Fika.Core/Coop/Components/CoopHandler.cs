using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Coop.BTR;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    /// <summary>
    /// CoopHandler is the User 1-2-1 communication to the Server. This can be seen as an extension component to CoopGame.
    /// </summary>
    public class CoopHandler : MonoBehaviour
    {
        #region Fields/Properties        
        public CoopGame LocalGameInstance { get; internal set; }
        public string ServerId { get; set; } = null;
        public Dictionary<int, CoopPlayer> Players = [];
        public List<CoopPlayer> HumanPlayers = [];
        public int AmountOfHumans = 1;
        public List<int> ExtractedPlayers = [];
        ManualLogSource Logger;
        public CoopPlayer MyPlayer => (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        public List<string> queuedProfileIds = [];
        private Queue<SpawnObject> spawnQueue = new(50);

        public class SpawnObject(Profile profile, Vector3 position, bool isAlive, bool isAI, int netId)
        {
            public Profile Profile { get; set; } = profile;
            public Vector3 Position { get; set; } = position;
            public bool IsAlive { get; set; } = isAlive;
            public bool IsAI { get; set; } = isAI;
            public int NetId { get; set; } = netId;
        }

        public bool RunAsyncTasks { get; set; } = true;

        internal FikaBTRManager_Client clientBTR = null;
        internal FikaBTRManager_Host serverBTR = null;

        internal static GameObject CoopHandlerParent;

        #endregion

        #region Public Voids

        public static CoopHandler GetCoopHandler()
        {
            if (CoopHandlerParent == null)
            {
                return null;
            }

            CoopHandler coopHandler = CoopHandler.CoopHandlerParent.GetComponent<CoopHandler>();
            if (coopHandler != null)
            {
                return coopHandler;
            }

            return null;
        }

        public static bool TryGetCoopHandler(out CoopHandler coopHandler)
        {
            coopHandler = GetCoopHandler();
            return coopHandler != null;
        }

        public static string GetServerId()
        {
            CoopHandler coopGC = GetCoopHandler();
            if (coopGC == null)
            {
                return FikaBackendUtils.GetGroupId();
            }

            return coopGC.ServerId;
        }
        #endregion

        #region Unity Component Methods

        /// <summary>
        /// Unity Component Awake Method
        /// </summary>
        protected void Awake()
        {
            // ----------------------------------------------------
            // Create a BepInEx Logger for CoopHandler
            Logger = BepInEx.Logging.Logger.CreateLogSource("CoopHandler");
        }

        /// <summary>
        /// Unity Component Start Method
        /// </summary>
        protected void Start()
        {
            if (FikaBackendUtils.IsClient)
            {
                _ = Task.Run(ReadFromServerCharactersLoop);
            }

            if (FikaBackendUtils.IsServer)
            {
                Singleton<GameWorld>.Instance.World_0.RegisterNetworkInteractionObjects(null);
            }
        }

        protected void OnDestroy()
        {
            Players.Clear();
            HumanPlayers.Clear();

            RunAsyncTasks = false;
        }

        private bool requestQuitGame = false;

        /// <summary>
        /// The state your character or game is in to Quit.
        /// </summary>
        public enum EQuitState
        {
            NONE = -1,
            YouAreDead,
            YouHaveExtracted
        }

        public EQuitState GetQuitState()
        {
            EQuitState quitState = EQuitState.NONE;

            if (!Singleton<IFikaGame>.Instantiated)
            {
                return quitState;
            }

            IFikaGame coopGame = Singleton<IFikaGame>.Instance;
            if (coopGame == null)
            {
                return quitState;
            }

            if (Players == null)
            {
                return quitState;
            }

            if (coopGame.ExtractedPlayers == null)
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
                quitState = EQuitState.YouAreDead;
            }

            // Extractions
            if (coopGame.ExtractedPlayers.Contains(MyPlayer.NetId))
            {
                quitState = EQuitState.YouHaveExtracted;
            }

            return quitState;
        }

        /// <summary>
        /// This handles the ways of exiting the active game session
        /// </summary>
        void ProcessQuitting()
        {
            EQuitState quitState = GetQuitState();

            if (FikaPlugin.ExtractKey.Value.IsDown() && quitState != EQuitState.NONE && !requestQuitGame)
            {
                //Log to both the in-game console as well as into the BepInEx logfile
                ConsoleScreen.Log($"{FikaPlugin.ExtractKey.Value} pressed, attempting to extract!");
                Logger.LogInfo($"{FikaPlugin.ExtractKey.Value} pressed, attempting to extract!");

                requestQuitGame = true;
                CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

                // If you are the server / host
                if (FikaBackendUtils.IsServer)
                {
                    // A host needs to wait for the team to extract or die!
                    if ((Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount > 0) && quitState != EQuitState.NONE)
                    {
                        NotificationManagerClass.DisplayWarningNotification("HOSTING: You cannot exit the game until all clients have disconnected.");
                        requestQuitGame = false;
                        return;
                    }
                    else if (Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0
                        && Singleton<FikaServer>.Instance.timeSinceLastPeerDisconnected > DateTime.Now.AddSeconds(-5)
                        && Singleton<FikaServer>.Instance.hasHadPeer)
                    {
                        NotificationManagerClass.DisplayWarningNotification($"HOSTING: Please wait at least 5 seconds after the last peer disconnected before quitting.");
                        requestQuitGame = false;
                        return;
                    }
                    else
                    {
                        coopGame.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.MyExitStatus, MyPlayer.ActiveHealthController.IsAlive ? coopGame.MyExitLocation : null, 0);
                    }
                }
                else
                {
                    coopGame.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.MyExitStatus, MyPlayer.ActiveHealthController.IsAlive ? coopGame.MyExitLocation : null, 0);
                }
                return;
            }
        }

        protected private void Update()
        {
            if (LocalGameInstance == null)
            {
                return;
            }

            if (spawnQueue.Count > 0)
            {
                SpawnPlayer(spawnQueue.Dequeue());
            }

            ProcessQuitting();
        }

        #endregion

        private async Task ReadFromServerCharactersLoop()
        {
            while (RunAsyncTasks)
            {
                CoopGame coopGame = LocalGameInstance;
                int waitTime = 2500;
                if (coopGame.Status == GameStatus.Started)
                {
                    waitTime = 15000;
                }
                await Task.Delay(waitTime);

                if (Players == null)
                {
                    continue;
                }

                ReadFromServerCharacters();
            }
        }

        private void ReadFromServerCharacters()
        {
            AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId);

            if (Players.Count > 0)
            {
                requestPacket.HasCharacters = true;
                requestPacket.Characters = [.. Players.Values.Select(p => p.ProfileId), .. queuedProfileIds];
            }

            NetDataWriter writer = Singleton<FikaClient>.Instance.Writer;
            if (writer != null)
            {
                writer.Reset();
                Singleton<FikaClient>.Instance.SendData(writer, ref requestPacket, DeliveryMethod.ReliableOrdered);
            }
        }

        private async void SpawnPlayer(SpawnObject spawnObject)
        {
            if (spawnObject.Profile == null)
            {
                Logger.LogError("SpawnPlayer: Profile was null!");
                queuedProfileIds.Remove(spawnObject.Profile.ProfileId);
                return;
            }

            foreach (IPlayer player in Singleton<GameWorld>.Instance.RegisteredPlayers)
            {
                if (player.ProfileId == spawnObject.Profile.ProfileId)
                {
                    return;
                }
            }

            foreach (IPlayer player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.ProfileId == spawnObject.Profile.ProfileId)
                {
                    return;
                }
            }

            int playerId = LocalGameInstance.method_12();

            IEnumerable<ResourceKey> allPrefabPaths = spawnObject.Profile.GetAllPrefabPaths();
            if (allPrefabPaths.Count() == 0)
            {
                Logger.LogError($"SpawnPlayer::{spawnObject.Profile.Info.Nickname}::PrefabPaths are empty!");
                return;
            }

            await Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid,
                PoolManager.AssemblyType.Local, allPrefabPaths.ToArray(), JobPriority.General).ContinueWith(x =>
                {
                    if (x.IsCompleted)
                    {
#if DEBUG
                        Logger.LogInfo($"SpawnPlayer::{spawnObject.Profile.Info.Nickname}::Load Complete");
#endif
                    }
                    else if (x.IsFaulted)
                    {
                        Logger.LogError($"SpawnPlayer::{spawnObject.Profile.Info.Nickname}::Load Failed");
                    }
                    else if (x.IsCanceled)
                    {
                        Logger.LogError($"SpawnPlayer::{spawnObject.Profile.Info.Nickname}::Load Cancelled");
                    }
                });

            ObservedCoopPlayer otherPlayer = SpawnObservedPlayer(spawnObject.Profile, spawnObject.Position, playerId, spawnObject.IsAI, spawnObject.NetId);

            if (!spawnObject.IsAlive)
            {
                // TODO: Spawn them as corpses?
                // Run 'OnDead'
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
                        Logger.LogInfo("Starting AddClientToBotEnemies routine.");
#endif
                        StartCoroutine(AddClientToBotEnemies(botController, otherPlayer));
                    }
                    else
                    {
                        Logger.LogError("botController was null when trying to add player to enemies!");
                    }
                }
                else
                {
                    Logger.LogError("LocalGameInstance was null when trying to add player to enemies!");
                }
            }

            queuedProfileIds.Remove(spawnObject.Profile.ProfileId);
        }

        public void QueueProfile(Profile profile, Vector3 position, int netId, bool isAlive = true, bool isAI = false)
        {
            foreach (IPlayer player in Singleton<GameWorld>.Instance.RegisteredPlayers)
            {
                if (player.ProfileId == profile.ProfileId)
                {
                    return;
                }
            }

            foreach (IPlayer player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.ProfileId == profile.ProfileId)
                {
                    return;
                }
            }

            if (queuedProfileIds.Contains(profile.ProfileId))
            {
                return;
            }

            queuedProfileIds.Add(profile.ProfileId);
#if DEBUG
            Logger.LogInfo($"Queueing profile: {profile.Nickname}, {profile.ProfileId}");
#endif
            spawnQueue.Enqueue(new SpawnObject(profile, position, isAlive, isAI, netId));
        }

        private ObservedCoopPlayer SpawnObservedPlayer(Profile profile, Vector3 position, int playerId, bool isAI, int netId)
        {
            bool isDedicatedProfile = !isAI && profile.Info.MainProfileNickname.Contains("dedicated_");

            ObservedCoopPlayer otherPlayer = ObservedCoopPlayer.CreateObservedPlayer(netId, position,
                Quaternion.identity, "Player", isAI == true ? "Bot_" : $"Player_{profile.Nickname}_",
                EPointOfView.ThirdPerson, profile, isAI, EUpdateQueue.Update, Player.EUpdateMode.Manual,
                Player.EUpdateMode.Auto, BackendConfigAbstractClass.Config.CharacterController.ObservedPlayerMode,
                () => Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseSensitivity,
                () => Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseAimingSensitivity,
                GClass1457.Default).Result;

            if (otherPlayer == null)
            {
                return null;
            }

            otherPlayer.NetId = netId;
#if DEBUG
            Logger.LogInfo($"SpawnObservedPlayer: {profile.Nickname} spawning with NetId {netId}");
#endif
            if (!isAI)
            {
                AmountOfHumans++;
            }

            if (!Players.ContainsKey(netId))
            {
                Players.Add(netId, otherPlayer);
            }
            else
            {
                Logger.LogError($"Trying to add {otherPlayer.Profile.Nickname} to list of players but it was already there!");
            }

            if (!isAI && !isDedicatedProfile && !HumanPlayers.Contains(otherPlayer))
            {
                HumanPlayers.Add(otherPlayer);
            }

            if (!Singleton<GameWorld>.Instance.RegisteredPlayers.Any(x => x.Profile.ProfileId == profile.ProfileId))
            {
                Singleton<GameWorld>.Instance.RegisteredPlayers.Add(otherPlayer);
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

            if (isAI)
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

                // We still want DogTags to be 'FiR'
                Item item = otherPlayer.Inventory.Equipment.GetSlot(EquipmentSlot.Dogtag).ContainedItem;
                if (item != null)
                {
                    item.SpawnedInSession = true;
                }
            }

            otherPlayer.InitObservedPlayer(isDedicatedProfile);

#if DEBUG
            Logger.LogInfo($"CreateLocalPlayer::{profile.Info.Nickname}::Spawned.");
#endif

            SetWeaponInHandsOfNewPlayer(otherPlayer);

            return otherPlayer;
        }

        private IEnumerator AddClientToBotEnemies(BotsController botController, LocalPlayer playerToAdd)
        {
            CoopGame coopGame = LocalGameInstance;

            Logger.LogInfo($"AddClientToBotEnemies: " + playerToAdd.Profile.Nickname);

            while (coopGame.Status != GameStatus.Running && !botController.IsEnable)
            {
                yield return null;
            }

            while (coopGame.BotsController.BotSpawner == null)
            {
                yield return null;
            }

#if DEBUG
            Logger.LogInfo($"Adding Client {playerToAdd.Profile.Nickname} to enemy list");
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
                Logger.LogInfo($"Verified that {playerToAdd.Profile.Nickname} was added to the enemy list.");
#endif
            }
            else
            {
                Logger.LogError($"Failed to add {playerToAdd.Profile.Nickname} to the enemy list.");
            }
        }

        /// <summary>
        /// Attempts to set up the New Player with the current weapon after spawning
        /// </summary>
        /// <param name="player">The player to set the item on</param>
        public void SetWeaponInHandsOfNewPlayer(Player player)
        {
            EquipmentClass equipment = player.Profile.Inventory.Equipment;
            if (equipment == null)
            {
                Logger.LogError($"SetWeaponInHandsOfNewPlayer: {player.Profile.Nickname}, {player.Profile.ProfileId} has no Equipment!");
            }
            Item item = null;

            if (equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem != null)
            {
                item = equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem;
            }

            if (item == null && equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem != null)
            {
                item = equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem;
            }

            if (item == null && equipment.GetSlot(EquipmentSlot.Holster).ContainedItem != null)
            {
                item = equipment.GetSlot(EquipmentSlot.Holster).ContainedItem;
            }

            if (item == null && equipment.GetSlot(EquipmentSlot.Scabbard).ContainedItem != null)
            {
                item = equipment.GetSlot(EquipmentSlot.Scabbard).ContainedItem;
            }

            player.SetItemInHands(item, (IResult) =>
            {
                if (IResult.Failed == true)
                {
                    Logger.LogError($"SetWeaponInHandsOfNewPlayer: Unable to set item {item} in hands for {player.Profile.Nickname}, {player.Profile.ProfileId}");
                }
            });
        }
    }
}