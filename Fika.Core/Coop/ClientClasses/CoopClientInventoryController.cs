using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using JetBrains.Annotations;
using System.IO;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class CoopClientInventoryController(Player player, Profile profile, bool examined) : Player.PlayerOwnerInventoryController(player, profile, examined)
    {
        public override bool HasDiscardLimits => false;

        ManualLogSource BepInLogger { get; set; } = BepInEx.Logging.Logger.CreateLogSource(nameof(CoopClientInventoryController));

        private readonly Player Player = player;
        private CoopPlayer CoopPlayer => (CoopPlayer)Player;

        public override void CallMalfunctionRepaired(Weapon weapon)
        {
            base.CallMalfunctionRepaired(weapon);
            if (!Player.IsAI && (bool)Singleton<SharedGameSettingsClass>.Instance.Game.Settings.MalfunctionVisability)
            {
                MonoBehaviourSingleton<PreloaderUI>.Instance.MalfunctionGlow.ShowGlow(BattleUIMalfunctionGlow.GlowType.Repaired, force: true, method_44());
            }
        }

        public override void Execute(GClass2837 operation, [CanBeNull] Callback callback)
        {
            if (MatchmakerAcceptPatches.IsServer)
            {
                HostInventoryOperationManager operationManager = new(this, operation, callback);
                if (vmethod_0(operationManager.operation))
                {
                    operationManager.operation.vmethod_0(operationManager.HandleResult);

                    // Do not replicate picking up quest items, throws an error on the other clients
                    if (operation is GClass2839 pickupOperation)
                    {
                        if (pickupOperation.Item.Template.QuestItem)
                        {
                            return;
                        }
                    }

                    InventoryPacket packet = new()
                    {
                        HasItemControllerExecutePacket = true
                    };

                    using MemoryStream memoryStream = new();
                    using BinaryWriter binaryWriter = new(memoryStream);
                    binaryWriter.WritePolymorph(GClass1632.FromInventoryOperation(operation, false));
                    byte[] opBytes = memoryStream.ToArray();
                    packet.ItemControllerExecutePacket = new()
                    {
                        CallbackId = operation.Id,
                        OperationBytes = opBytes,
                        InventoryId = ID
                    };

                    CoopPlayer.PacketSender?.InventoryPackets?.Enqueue(packet);

                    return;
                }
                operationManager.operation.Dispose();
                operationManager.callback?.Fail($"Can't execute {operationManager.operation}", 1);
            }
            else if (MatchmakerAcceptPatches.IsClient)
            {
                // Do not replicate picking up quest items, throws an error on the other clients
                if (operation is GClass2839 pickupOperation)
                {
                    if (pickupOperation.Item.Template.QuestItem)
                    {
                        base.Execute(operation, callback);
                        return;
                    }
                }

                InventoryPacket packet = new()
                {
                    HasItemControllerExecutePacket = true
                };

                ClientInventoryOperationManager clientOperationManager = new()
                {
                    operation = operation,
                    callback = callback,
                    inventoryController = this
                };
                clientOperationManager.callback ??= new Callback(ClientPlayer.Control0.Class1400.class1400_0.method_0);
                uint operationNum = AddOperationCallback(operation, new Callback<EOperationStatus>(clientOperationManager.HandleResult));

                using MemoryStream memoryStream = new();
                using BinaryWriter binaryWriter = new(memoryStream);
                binaryWriter.WritePolymorph(GClass1632.FromInventoryOperation(operation, false));
                byte[] opBytes = memoryStream.ToArray();
                packet.ItemControllerExecutePacket = new()
                {
                    CallbackId = operationNum,
                    OperationBytes = opBytes,
                    InventoryId = ID
                };

                CoopPlayer.PacketSender?.InventoryPackets?.Enqueue(packet);
            }
        }

        private uint AddOperationCallback(GClass2837 operation, Callback<EOperationStatus> callback)
        {
            ushort id = operation.Id;
            CoopPlayer.OperationCallbacks.Add(id, callback);
            return id;
        }

        private class HostInventoryOperationManager(CoopClientInventoryController inventoryController, GClass2837 operation, Callback callback)
        {
            public readonly CoopClientInventoryController inventoryController = inventoryController;
            public GClass2837 operation = operation;
            public readonly Callback callback = callback;

            public void HandleResult(IResult result)
            {
                if (!result.Succeed)
                {
                    inventoryController.Logger.LogError("[{0}][{5}] {1} - Local operation failed: {2} - {3}\r\nError: {4}",
                    [
                    Time.frameCount,
                    inventoryController.ID,
                    operation.Id,
                    operation,
                    result.Error,
                    inventoryController.Name
                    ]);
                }
                callback?.Invoke(result);
            }
        }

        private class ClientInventoryOperationManager
        {
            public EOperationStatus? serverOperationStatus;
            public EOperationStatus? localOperationStatus;
            public GClass2837 operation;
            public Callback callback;
            public CoopClientInventoryController inventoryController;

            public void HandleResult(Result<EOperationStatus> result)
            {
                ConsoleScreen.Log("Running OperationManager::HandleResult");
                ClientInventoryCallbackManager callbackManager = new()
                {
                    clientOperationManager = this,
                    result = result
                };
                if (callbackManager.result.Succeed && callbackManager.result.Value != EOperationStatus.Failed)
                {
                    EOperationStatus value = callbackManager.result.Value;
                    if (value == EOperationStatus.Started)
                    {
                        localOperationStatus = EOperationStatus.Started;
                        serverOperationStatus = EOperationStatus.Started;
                        operation.vmethod_0(new Callback(callbackManager.HandleResult), false);
                        return;
                    }
                    /*if (value == EOperationStatus.Finished)
                    {
                        serverOperationStatus = EOperationStatus.Finished;
                        localOperationStatus = EOperationStatus.Finished;
                        operation.Dispose();
                        callback.Succeed();
                        return;
                    }*/
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"{inventoryController.ID} - Client operation rejected by server: {operation.Id} - {operation}\r\nReason: {callbackManager.result.Error}");
                    serverOperationStatus = EOperationStatus.Failed;
                    localOperationStatus = EOperationStatus.Failed;
                    operation.Dispose();
                    callback.Invoke(callbackManager.result);
                }
            }
        }

        private class ClientInventoryCallbackManager
        {
            public Result<EOperationStatus> result;
            public ClientInventoryOperationManager clientOperationManager;

            public void HandleResult(IResult executeResult)
            {
                ConsoleScreen.Log("Running CallbackManager::HandleResult");
                if (!executeResult.Succeed)
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"{clientOperationManager.inventoryController.ID} - Client operation critical failure: {clientOperationManager.inventoryController.ID} - {clientOperationManager.operation}\r\nError: {executeResult.Error}");
                }
                clientOperationManager.operation.Dispose();
                clientOperationManager.callback.Invoke(result);
                return;
                /*clientOperationManager.localOperationStatus = EOperationStatus.Finished;
                EOperationStatus? serverOperationStatus = clientOperationManager.serverOperationStatus;
                EOperationStatus? eoperationStatus = clientOperationManager.localOperationStatus;
                if ((serverOperationStatus.GetValueOrDefault() == eoperationStatus.GetValueOrDefault()) & (serverOperationStatus != null == (eoperationStatus != null)))
                {
                    clientOperationManager.operation.Dispose();
                    clientOperationManager.callback.Invoke(result);
                    return;
                }*/
                /*if (clientOperationManager.serverOperationStatus != null)
                {
                    eoperationStatus = clientOperationManager.serverOperationStatus;
                    if ((eoperationStatus.GetValueOrDefault() == EOperationStatus.Failed) & (eoperationStatus != null))
                    {
                        clientOperationManager.operation.Dispose();
                        clientOperationManager.callback.Invoke(result);
                    }
                }*/
            }
        }
    }
}
