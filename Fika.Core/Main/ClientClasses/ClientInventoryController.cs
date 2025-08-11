using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.World;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Main.ClientClasses
{
    public sealed class ClientInventoryController : Player.PlayerOwnerInventoryController
    {
        public override bool HasDiscardLimits
        {
            get
            {
                return false;
            }
        }
        private readonly ManualLogSource _logger;
        private readonly Player _player;
        private readonly FikaPlayer _fikaPlayer;
        private readonly IPlayerSearchController _searchController;

        public ClientInventoryController(Player player, Profile profile, bool examined) : base(player, profile, examined)
        {
            this._player = player;
            _fikaPlayer = (FikaPlayer)player;
            MongoID_0 = MongoID.Generate(true);
            _searchController = new PlayerSearchControllerClass(profile, this);
            _logger = BepInEx.Logging.Logger.CreateLogSource(nameof(ClientInventoryController));
        }

        public override IPlayerSearchController PlayerSearchController
        {
            get
            {
                return _searchController;
            }
        }

        public override void GetTraderServicesDataFromServer(string traderId)
        {
            if (FikaBackendUtils.IsClient)
            {
                RequestPacket request = new()
                {
                    Type = ERequestSubPacketType.TraderServices,
                    RequestSubPacket = new RequestSubPackets.TraderServicesRequest()
                    {
                        NetId = _fikaPlayer.NetId,
                        TraderId = traderId
                    }
                };

                Singleton<IFikaNetworkManager>.Instance.SendData(ref request, DeliveryMethod.ReliableOrdered);
                return;
            }

            _fikaPlayer.UpdateTradersServiceData(traderId).HandleExceptions();
        }

        public override void CallMalfunctionRepaired(Weapon weapon)
        {
            if (Singleton<SharedGameSettingsClass>.Instance.Game.Settings.MalfunctionVisability)
            {
                MonoBehaviourSingleton<PreloaderUI>.Instance.MalfunctionGlow.ShowGlow(BattleUIMalfunctionGlow.EGlowType.Repaired, true, method_41());
            }
        }

        public override void vmethod_1(BaseInventoryOperationClass operation, Callback callback)
        {
            HandleOperation(operation, callback).HandleExceptions();
        }

        private async Task HandleOperation(BaseInventoryOperationClass operation, Callback callback)
        {
            if (_player.HealthController.IsAlive)
            {
                await Task.Yield();
            }
            RunClientOperation(operation, callback);
        }

        private void RunClientOperation(BaseInventoryOperationClass operation, Callback callback)
        {
            if (!vmethod_0(operation))
            {
                operation.Dispose();
                callback.Fail("LOCAL: hands controller can't perform this operation");
                return;
            }

            // Do not replicate picking up quest items, throws an error on the other clients            
            if (operation is MoveOperationClass moveOperation)
            {
                Item lootedItem = moveOperation.Item;
                if (lootedItem.QuestItem)
                {
                    if (_fikaPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController && sharedQuestController.ContainsAcceptedType("PlaceBeacon"))
                    {
                        if (!sharedQuestController.CheckForTemplateId(lootedItem.TemplateId))
                        {
                            sharedQuestController.AddLootedTemplateId(lootedItem.TemplateId);

                            // We use templateId because each client gets a unique itemId
                            QuestItemPacket questPacket = new()
                            {
                                Nickname = _fikaPlayer.Profile.Info.MainProfileNickname,
                                ItemId = lootedItem.TemplateId
                            };
                            _fikaPlayer.PacketSender.NetworkManager.SendData(ref questPacket, DeliveryMethod.ReliableOrdered, true);
                        }
                    }
                    base.vmethod_1(operation, callback);
                    return;
                }
            }

            // Do not replicate stashing quest items
            if (operation is RemoveOperationClass discardOperation)
            {
                if (discardOperation.Item.QuestItem)
                {
                    base.vmethod_1(operation, callback);
                    return;
                }
            }

            // Do not replicate search operations
            if (operation is SearchContentOperationResultClass)
            {
                base.vmethod_1(operation, callback);
                return;
            }

            EFTWriterClass eftWriter = new();
            ClientInventoryOperationHandler handler = new()
            {
                Operation = operation,
                Callback = callback,
                InventoryController = this
            };

            uint operationNum = AddOperationCallback(operation, handler.ReceiveStatusFromServer);
            eftWriter.WritePolymorph(operation.ToDescriptor());
            InventoryPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                CallbackId = operationNum,
                OperationBytes = eftWriter.ToArray()
            };

#if DEBUG
            ConsoleScreen.Log($"InvOperation: {operation.GetType().Name}, Id: {operation.Id}");
#endif

            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public override bool HasCultistAmulet(out CultistAmuletItemClass amulet)
        {
            amulet = null;
            using IEnumerator<Item> enumerator = Inventory.GetItemsInSlots([EquipmentSlot.Pockets]).GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is CultistAmuletItemClass cultistAmuletClass)
                {
                    amulet = cultistAmuletClass;
                    return true;
                }
            }
            return false;
        }

        private uint AddOperationCallback(BaseInventoryOperationClass operation, Action<ServerOperationStatus> callback)
        {
            ushort id = operation.Id;
            _fikaPlayer.OperationCallbacks.Add(id, callback);
            return id;
        }

        public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
        {
            return new SearchContentOperationResultClass(method_12(), this, PlayerSearchController, Profile, item);
        }

        private class ClientInventoryOperationHandler
        {
            public BaseInventoryOperationClass Operation;
            public Callback Callback;
            public ClientInventoryController InventoryController;
            public IResult OperationResult = null;
            public ServerOperationStatus ServerStatus = default;

            public void ReceiveStatusFromServer(ServerOperationStatus serverStatus)
            {
                ServerStatus = serverStatus;
                switch (serverStatus.Status)
                {
                    case EOperationStatus.Started:
                        Operation.method_0(ExecuteResult);
                        return;
                    case EOperationStatus.Succeeded:
                        HandleFinalResult(SuccessfulResult.New);
                        return;
                    case EOperationStatus.Failed:
                        InventoryController._logger.LogError($"{InventoryController.ID} - Client operation rejected by server: {Operation.Id} - {Operation}\r\nReason: {serverStatus.Error}");
                        HandleFinalResult(new FailedResult(serverStatus.Error));
                        break;
                    default:
                        InventoryController._logger.LogError("ReceiveStatusFromServer: Status was missing?");
                        break;
                }
            }

            private void ExecuteResult(IResult executeResult)
            {
                if (!executeResult.Succeed)
                {
                    InventoryController._logger.LogError($"{InventoryController.ID} - Client operation critical failure: {Operation.Id} server status: {"SERVERRESULT"} - {Operation}\r\nError: {executeResult.Error}");
                }
                HandleFinalResult(executeResult);
            }

            private void HandleFinalResult(IResult result)
            {
                IResult result2 = OperationResult;
                if (result2 == null || !result2.Failed)
                {
                    OperationResult = result;
                }
                EOperationStatus serverStatus = ServerStatus.Status;
                if (!serverStatus.Finished())
                {
                    return;
                }
                EOperationStatus localStatus = Operation.Status;
                if (localStatus.InProgress())
                {
                    if (Operation is GInterface424 ginterface)
                    {
                        ginterface.Terminate();
                    }
                    return;
                }
                Operation.Dispose();
                if (serverStatus != localStatus)
                {
                    if (localStatus.Finished())
                    {
                        InventoryController._logger.LogError($"{InventoryController.ID} - Operation critical failure - status mismatch: {Operation.Id} server status: {serverStatus} client status: {localStatus} - {Operation}");
                    }
                }
                Callback?.Invoke(OperationResult);
            }
        }

        public readonly struct ServerOperationStatus(EOperationStatus status, string error)
        {
            public readonly EOperationStatus Status = status;
            public readonly string Error = error;
        }
    }
}