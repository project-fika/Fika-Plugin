using Aki.Custom.Airdrops.Models;
using Aki.Custom.Airdrops.Utils;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.AkiSupport.Airdrops;
using Fika.Core.AkiSupport.Airdrops.Models;
using Fika.Core.AkiSupport.Airdrops.Utils;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections;
using UnityEngine;

namespace Aki.Custom.Airdrops
{
    /// <summary>
    /// Created by: SPT-Aki team
    /// Link: https://dev.sp-tarkov.com/SPT-AKI/Modules/src/branch/master/project/Aki.Custom/Airdrops/AirdropsManager.cs
    /// Modified by Lacyway and nexus4880: Uses BSG code to serialize/deserialize data from host to clients
    /// </summary>
    public class FikaAirdropsManager : MonoBehaviour
    {
        private FikaAirdropPlane airdropPlane;
        private FikaAirdropBox AirdropBox;
        private FikaItemFactoryUtil factory;
        public bool isFlareDrop;
        public FikaAirdropParametersModel AirdropParameters { get; set; }
        private ManualLogSource Logger { get; set; }
        float distanceTravelled = 0;
        public bool ClientPlaneSpawned;
        public bool ClientLootBuilt = false;
        public static int ContainerCount = 0;

        // Client fields
        private string ContainerId;
        private Item rootItem;

        protected void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("FikaAirdropsManager");
            Logger.LogInfo(isFlareDrop ? "Initializing from flare..." : "Initializing...");
            if (Singleton<FikaAirdropsManager>.Instance != null)
            {
                Logger.LogWarning("Another manager already exists, destroying old...");
                if (airdropPlane != null)
                {
                    Destroy(airdropPlane.gameObject);
                }
                if (AirdropBox != null)
                {
                    Destroy(AirdropBox.gameObject);
                }
                Destroy(Singleton<FikaAirdropsManager>.Instance);
            }
            Singleton<FikaAirdropsManager>.Create(this);
        }

        protected void OnDestroy()
        {
            Logger.LogWarning("Destroying AirdropsManager");
        }

        protected async void Start()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;

            if (gameWorld == null)
            {
                Logger.LogError("gameWorld is NULL");
                Destroy(this);
            }

            string location = gameWorld.MainPlayer.Location;
            if (location.Contains("factory") || location.Contains("laboratory") || location.Contains("Sandbox"))
            {
                Destroy(this);
                return;
            }

            // If this is not the server, then this manager will have to wait for the packet to initialize stuff.
            if (MatchmakerAcceptPatches.IsClient)
            {
                return;
            }

            // The server will generate stuff ready for the packet
            AirdropParameters = FikaAirdropUtil.InitAirdropParams(gameWorld, isFlareDrop);

            if (!AirdropParameters.AirdropAvailable)
            {
                Logger.LogInfo("Airdrop is not available, destroying manager...");

                GenericPacket packet = new()
                {
                    NetId = 0,
                    PacketType = EPackageType.RemoveAirdropManager
                };

                Singleton<FikaServer>.Instance.SendDataToAll(new(), ref packet, DeliveryMethod.ReliableOrdered);

                Destroy(this);
                return;
            }

            try
            {
                airdropPlane = await FikaAirdropPlane.Init(
                    AirdropParameters.RandomAirdropPoint,
                    AirdropParameters.DropHeight,
                    AirdropParameters.Config.PlaneVolume,
                    AirdropParameters.Config.PlaneSpeed);
                AirdropBox = await FikaAirdropBox.Init(AirdropParameters.Config.CrateFallSpeed);
                factory = new FikaItemFactoryUtil();
            }
            catch
            {
                Logger.LogError("[AKI-AIRDROPS]: Unable to create plane or crate, airdrop won't occur");
                Destroy(this);
                throw;
            }

            SetDistanceToDrop();

            BuildLootContainer(AirdropParameters.Config);
        }

        public void SendParamsToClients()
        {
            if (!MatchmakerAcceptPatches.IsServer)
            {
                return;
            }

            Logger.LogInfo("Sending Airdrop Params");
            AirdropPacket airdropPacket = new()
            {
                Config = AirdropParameters.Config,
                AirdropAvailable = AirdropParameters.AirdropAvailable,
                PlaneSpawned = AirdropParameters.PlaneSpawned,
                BoxSpawned = AirdropParameters.BoxSpawned,
                DistanceTraveled = AirdropParameters.DistanceTraveled,
                DistanceToTravel = AirdropParameters.DistanceToTravel,
                DistanceToDrop = AirdropParameters.DistanceToDrop,
                Timer = AirdropParameters.Timer,
                DropHeight = AirdropParameters.DropHeight,
                TimeToStart = AirdropParameters.TimeToStart,
                BoxPoint = AirdropParameters.RandomAirdropPoint,
                SpawnPoint = airdropPlane.newPosition,
                LookPoint = airdropPlane.newRotation
            };
            NetDataWriter writer = new();
            Singleton<FikaServer>.Instance.SendDataToAll(writer, ref airdropPacket, DeliveryMethod.ReliableOrdered);
        }

        protected async void FixedUpdate()
        {
            if (AirdropParameters == null || AirdropParameters.Config == null)
            {
                return;
            }

            // If we are a client. Wait until the server has sent all the data.
            if (MatchmakerAcceptPatches.IsClient && rootItem == null)
            {
                return;
            }

            // If we have all the parameters sent from the Server. Lets build the plane, box, container and loot
            if (MatchmakerAcceptPatches.IsClient && !ClientLootBuilt)
            {
                ClientLootBuilt = true;

                Logger.LogInfo("Client::Building Plane, Box, Factory and Loot.");

                airdropPlane = await FikaAirdropPlane.Init(AirdropParameters.SpawnPoint, AirdropParameters.DropHeight, AirdropParameters.Config.PlaneVolume,
                    AirdropParameters.Config.PlaneSpeed, true, AirdropParameters.LookPoint);

                AirdropBox = await FikaAirdropBox.Init(AirdropParameters.Config.CrateFallSpeed);
                factory = new FikaItemFactoryUtil();

                factory.BuildClientContainer(AirdropBox.Container, rootItem);

                if (AirdropBox.Container != null && CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    if (!string.IsNullOrEmpty(ContainerId))
                    {
                        AirdropBox.Container.Id = ContainerId;
                        coopHandler.ListOfInteractiveObjects.Add(ContainerId, AirdropBox.Container);
                        Logger.LogInfo($"Adding AirdropBox {AirdropBox.Container.Id} to interactive objects.");
                    }
                    else
                    {
                        Logger.LogError("ContainerId received from server was empty.");
                    }
                }

            }

            if (!ClientLootBuilt)
            {
                return;
            }

            if (airdropPlane == null || AirdropBox == null || factory == null)
            {
                return;
            }

            if (MatchmakerAcceptPatches.IsServer || MatchmakerAcceptPatches.IsSinglePlayer)
            {
                AirdropParameters.Timer += 0.02f;

                if (AirdropParameters.Timer >= AirdropParameters.TimeToStart && !AirdropParameters.PlaneSpawned)
                {
                    SendParamsToClients();
                    StartPlane();
                }

                if (!AirdropParameters.PlaneSpawned)
                {
                    return;
                }
            }
            else
            {
                AirdropParameters.Timer += 0.02f;

                if (!ClientPlaneSpawned)
                {
                    ClientPlaneSpawned = true;
                    StartPlane();
                }
            }

            if (distanceTravelled >= AirdropParameters.DistanceToDrop && !AirdropParameters.BoxSpawned)
            {
                StartBox();
            }

            if (distanceTravelled < AirdropParameters.DistanceToTravel)
            {
                distanceTravelled += Time.deltaTime * AirdropParameters.Config.PlaneSpeed;
                float distanceToDrop = AirdropParameters.DistanceToDrop - distanceTravelled;
                airdropPlane.ManualUpdate(distanceToDrop);
            }
            else
            {
                Destroy(airdropPlane.gameObject);
                Destroy(this);
            }
        }

        private void StartPlane()
        {
            airdropPlane.gameObject.SetActive(true);
            AirdropParameters.PlaneSpawned = true;
        }

        private void StartBox()
        {
            AirdropParameters.BoxSpawned = true;
            Vector3 pointPos = AirdropParameters.RandomAirdropPoint;
            Vector3 dropPos = new(pointPos.x, AirdropParameters.DropHeight, pointPos.z);
            AirdropBox.gameObject.SetActive(true);
            AirdropBox.StartCoroutine(AirdropBox.DropCrate(dropPos));
        }

        private void BuildLootContainer(FikaAirdropConfigModel config)
        {
            if (MatchmakerAcceptPatches.IsClient)
            {
                return;
            }

            FikaAirdropLootResultModel lootData = factory.GetLoot();

            if (lootData == null)
            {
                throw new Exception("Airdrops. Tried to BuildLootContainer without any Loot.");
            }

            factory.BuildContainer(AirdropBox.Container, config, lootData.DropType);
            factory.AddLoot(AirdropBox.Container, lootData);

            if (AirdropBox.Container != null && CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                if (coopHandler.GetInteractiveObject(AirdropBox.Container.Id, out _))
                {
                    Logger.LogInfo("Existing crate already exists, setting value to " + ContainerCount);
                    AirdropBox.Container.Id = AirdropBox.Container.Id + $"_{ContainerCount}";
                    ContainerCount++;
                }
                coopHandler.ListOfInteractiveObjects.Add(AirdropBox.Container.Id, AirdropBox.Container);
                Logger.LogInfo($"Adding AirdropBox {AirdropBox.Container.Id} to interactive objects.");
            }

            // Get the lootData. Send to clients.
            if (MatchmakerAcceptPatches.IsServer)
            {
                StartCoroutine(SendLootToClients(isFlareDrop));
            }
        }

        public void ReceiveBuildLootContainer(AirdropLootPacket packet)
        {
            Logger.LogInfo("Received loot container parameters");
            rootItem = packet.RootItem;
            ContainerId = packet.ContainerId;
        }

        private void SetDistanceToDrop()
        {
            AirdropParameters.DistanceToDrop = Vector3.Distance(new Vector3(AirdropParameters.RandomAirdropPoint.x, AirdropParameters.DropHeight, AirdropParameters.RandomAirdropPoint.z),
                airdropPlane.transform.position);
        }

        private IEnumerator SendLootToClients(bool isFlare = false)
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            while (coopGame.Status != GameStatus.Started)
            {
                yield return null;
            }

            if (!isFlare)
            {
                while (!ClientLootBuilt)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(5);
            }
            else
            {
                while (!AirdropParameters.PlaneSpawned && !ClientLootBuilt)
                {
                    yield return null;
                }
            }

            Logger.LogInfo("Sending Airdrop Loot to clients.");

            Item rootItem = AirdropBox.Container.ItemOwner.RootItem;

            AirdropLootPacket lootPacket = new()
            {
                RootItem = rootItem,
                ContainerId = AirdropBox.Container.Id
            };

            NetDataWriter writer = new();
            Singleton<FikaServer>.Instance.SendDataToAll(writer, ref lootPacket, DeliveryMethod.ReliableOrdered);

            yield break;
        }
    }
}