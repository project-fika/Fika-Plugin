using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;
using Fika.Core.Main.ClientClasses;
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
using static Fika.Core.Main.ClientClasses.ClientInventoryController;

namespace Fika.Core.Main.HostClasses;

public sealed class FikaHostInventoryController : Player.PlayerOwnerInventoryController
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

    public FikaHostInventoryController(Player player, Profile profile, bool examined) : base(player, profile, examined)
    {
        _player = player;
        _fikaPlayer = (FikaPlayer)player;
        _searchController = new PlayerSearchControllerClass(profile, this);
        _logger = BepInEx.Logging.Logger.CreateLogSource(nameof(FikaHostInventoryController));
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
            Singleton<FikaClient>.Instance.SendData(ref request, DeliveryMethod.ReliableOrdered);
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
        RunHostOperation(operation, callback);
    }

    private void RunHostOperation(BaseInventoryOperationClass operation, Callback callback)
    {
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
                        QuestItemPacket packet = new()
                        {
                            Nickname = _fikaPlayer.Profile.Info.MainProfileNickname,
                            ItemId = lootedItem.TemplateId,
                        };
                        _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
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

#if DEBUG
        ConsoleScreen.Log($"InvOperation: {operation.GetType().Name}, Id: {operation.Id}");
#endif
        // Check for GClass increments, TraderServices
        if (operation is GClass3360)
        {
            base.vmethod_1(operation, callback);
            return;
        }

        HostInventoryOperationHandler handler = new(this, operation, callback);
        if (vmethod_0(handler.operation))
        {
            handler.operation.method_1(handler.HandleResult);

            EFTWriterClass eftWriter = new();
            eftWriter.WritePolymorph(operation.ToDescriptor());
            InventoryPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                CallbackId = operation.Id,
                OperationBytes = eftWriter.ToArray()
            };

            _fikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            return;
        }
        handler.operation.Dispose();
        handler.callback?.Fail($"Can't execute {handler.operation}", 1);
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

    private class HostInventoryOperationHandler(FikaHostInventoryController inventoryController, BaseInventoryOperationClass operation, Callback callback)
    {
        public readonly FikaHostInventoryController inventoryController = inventoryController;
        public BaseInventoryOperationClass operation = operation;
        public readonly Callback callback = callback;

        public void HandleResult(IResult result)
        {
            if (!result.Succeed)
            {
                inventoryController._logger.LogError($"[{Time.frameCount}][{inventoryController.Name}] {inventoryController.ID} - Local operation failed: {operation.Id} - {operation}\r\nError: {result.Error}");
            }
            callback?.Invoke(result);
        }
    }
}