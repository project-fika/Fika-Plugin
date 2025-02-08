// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class PacketReceiver : MonoBehaviour
    {
        private CoopPlayer player;
        private ObservedCoopPlayer observedPlayer;
        public FikaServer Server { get; private set; }
        public FikaClient Client { get; private set; }
        internal Queue<IQueuePacket> PacketQueue;
        internal Queue<IQueuePacket> ObservedPacketQueue;
        private Queue<BaseInventoryOperationClass> inventoryOperations;

        protected void Awake()
        {
            player = GetComponent<CoopPlayer>();
            if (!player.IsYourPlayer)
            {
                observedPlayer = GetComponent<ObservedCoopPlayer>();
            }
            PacketQueue = new();
            ObservedPacketQueue = new();
            inventoryOperations = new();
        }

        protected void Start()
        {
            if (FikaBackendUtils.IsServer)
            {
                Server = Singleton<FikaServer>.Instance;
            }
            else
            {
                Client = Singleton<FikaClient>.Instance;
            }
        }

        protected void OnDestroy()
        {
            PacketQueue.Clear();
            inventoryOperations.Clear();
        }

        protected void LateUpdate()
        {
            if (observedPlayer != null)
            {
                int healthSyncPackets = ObservedPacketQueue.Count;
                for (int i = 0; i < healthSyncPackets; i++)
                {
                    ObservedPacketQueue.Dequeue().Execute(observedPlayer);
                }
            }

            if (player == null)
            {
                return;
            }

            int packetAmount = PacketQueue.Count;
            for (int i = 0; i < packetAmount; i++)
            {
                PacketQueue.Dequeue().Execute(player);
            }
            int inventoryOps = inventoryOperations.Count;
            if (inventoryOps > 0)
            {
                if (inventoryOperations.Peek().WaitingForForeignEvents())
                {
                    return;
                }
                inventoryOperations.Dequeue().method_1(HandleResult);
            }
        }

        public void ConvertInventoryPacket(InventoryPacket packet)
        {
            if (packet.OperationBytes.Length == 0)
            {
                FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Bytes were null!");
                return;
            }

            InventoryController controller = player.InventoryController;
            if (controller != null)
            {
                try
                {
                    if (controller is Interface16 networkController)
                    {
                        FikaReader eftReader = EFTSerializationManager.GetReader(packet.OperationBytes);
                        BaseDescriptorClass descriptor = eftReader.ReadPolymorph<BaseDescriptorClass>();
                        GStruct449 result = networkController.CreateOperationFromDescriptor(descriptor);
                        if (!result.Succeeded)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Unable to process descriptor from netId {packet.NetId}, error: {result.Error}");
                            return;
                        }

                        inventoryOperations.Enqueue(result.Value);
                    }
                }
                catch (Exception exception)
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Exception thrown: {exception}");
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("ConvertInventoryPacket: inventory was null!");
            }
        }

        private void HandleResult(IResult result)
        {
            if (result.Failed)
            {
                FikaPlugin.Instance.FikaLogger.LogError($"Error in operation: {result.Error}");
            }
        }
    }
}
