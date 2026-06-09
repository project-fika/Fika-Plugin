using System;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;
using Fika.Core.Main.BaseClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientInventoryController : BaseInventoryController
{
    public FikaPlayer FikaPlayer { get; }

    public override bool HasDiscardLimits
    {
        get
        {
            return false;
        }
    }
    private readonly Player _player;
    private readonly ClientInventoryOperationHandlerPool _clientInventoryOperationHandlerPool;

    public ClientInventoryController(Player player, Profile profile, bool examined) : base(player, profile, examined)
    {
        _player = player;
        FikaPlayer = (FikaPlayer)player;
        MongoID_0 = MongoID.Generate(true);
        PlayerSearchController = new PlayerSearchControllerClass(profile, this);
        _clientInventoryOperationHandlerPool = new ClientInventoryOperationHandlerPool(8, ClientInventoryOperationHandler.CreateInstance);
    }

    public override IPlayerSearchController PlayerSearchController { get; }

    public override void GetTraderServicesDataFromServer(string traderId)
    {
        if (FikaBackendUtils.IsClient)
        {
            RequestPacket request = new()
            {
                Type = ERequestSubPacketType.TraderServices,
                RequestSubPacket = new RequestSubPackets.TraderServicesRequest()
                {
                    NetId = FikaPlayer.NetId,
                    TraderId = traderId
                }
            };

            Singleton<IFikaNetworkManager>.Instance.SendData(ref request, DeliveryMethod.ReliableOrdered);
            return;
        }

        FikaPlayer.UpdateTradersServiceData(traderId).HandleExceptions();
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

    /// <summary>
    /// Gets an inventory handler
    /// </summary>
    /// <returns>A pooled handler</returns>
    public ClientInventoryOperationHandler GetHandler()
    {
        return _clientInventoryOperationHandlerPool.Get();
    }

    /// <summary>
    /// Returns a handler
    /// </summary>
    /// <param name="handler">The handler to return</param>
    public void ReturnHandler(ClientInventoryOperationHandler handler)
    {
        _clientInventoryOperationHandlerPool.ReturnHandler(handler);
    }

    private void RunClientOperation(BaseInventoryOperationClass operation, Callback callback)
    {
        if (!vmethod_0(operation))
        {
            operation.Dispose();
            callback?.Fail("LOCAL: hands controller can't perform this operation");
            return;
        }

        // Do not replicate picking up quest items, throws an error on the other clients            
        if (operation is MoveOperationClass moveOperation)
        {
            var lootedItem = moveOperation.Item;
            if (lootedItem.QuestItem)
            {
                if (FikaPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController
                    && sharedQuestController.ContainsAcceptedType("FindItem")
                    && !sharedQuestController.CheckForTemplateId(lootedItem.TemplateId))
                {
                    sharedQuestController.AddLootedTemplateId(lootedItem.TemplateId);

                    // We use templateId because each client gets a unique itemId
                    QuestItemPacket questPacket = new()
                    {
                        Nickname = FikaPlayer.Profile.Info.MainProfileNickname,
                        ItemId = lootedItem.TemplateId
                    };
                    FikaPlayer.PacketSender.NetworkManager.SendData(ref questPacket, DeliveryMethod.ReliableOrdered, true);
                }
                base.vmethod_1(operation, callback);
                return;
            }
        }

        // Do not replicate stashing quest items
        if (operation is RemoveOperationClass discardOperation && discardOperation.Item.QuestItem)
        {
            base.vmethod_1(operation, callback);
            return;
        }

        // Do not replicate search operations
        if (operation is SearchContentOperationResultClass or GClass3496) // search for "DialogController not available"
        {
            base.vmethod_1(operation, callback);
            return;
        }

        var handler = _clientInventoryOperationHandlerPool.Get();
        handler.Set(this, operation, callback);
        var operationNum = AddOperationCallback(operation, handler.ServerStatusDelegate);
        FikaPlayer.PacketSender.NetworkManager.SendGenericPacket(EGenericSubPacketType.InventoryOperation,
                InventoryPacket.FromValue(FikaPlayer.NetId, operation));
#if DEBUG
        ConsoleScreen.Log($"InvOperation: {operation.GetType().Name}, Id: {operation.Id}");
#endif
    }

    public override bool HasCultistAmulet(out CultistAmuletItemClass amulet)
    {
        amulet = null;
        using var enumerator = Inventory.GetItemsInSlots([EquipmentSlot.Pockets])
            .GetEnumerator();

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

    public ushort AddOperationCallback(BaseInventoryOperationClass operation, Action<ServerOperationStatus> callback)
    {
        var id = operation.Id;
        FikaPlayer.OperationCallbacks.Add(id, callback);
        return id;
    }

    public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
    {
        return new SearchContentOperationResultClass(method_12(), this, PlayerSearchController, Profile, item);
    }

    public readonly struct ServerOperationStatus(EOperationStatus status, string error)
    {
        public readonly EOperationStatus Status = status;
        public readonly string Error = error;
    }

    public void ClearPool()
    {
        _clientInventoryOperationHandlerPool.Dispose();
    }
}