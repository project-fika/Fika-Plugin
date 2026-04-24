using System.Threading.Tasks;
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
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Main.HostClasses;

public sealed class HostInventoryController : Player.PlayerOwnerInventoryController
{
    public FikaPlayer FikaPlayer { get; }

    public override bool HasDiscardLimits
    {
        get
        {
            return false;
        }
    }
    private readonly ManualLogSource _logger;
    private readonly Player _player;
    private readonly IPlayerSearchController _searchController;
    private readonly bool _instantLoad;
    private readonly HostInventoryOperationHandlerPool _hostInventoryOperationHandlerPool;

    public HostInventoryController(Player player, Profile profile, bool examined, bool instantLoad) : base(player, profile, examined)
    {
        _player = player;
        FikaPlayer = (FikaPlayer)player;
        _searchController = new PlayerSearchControllerClass(profile, this);
        _instantLoad = instantLoad;
        _hostInventoryOperationHandlerPool = new HostInventoryOperationHandlerPool(8, HostInventoryOperationHandler.CreateInstance);
        _logger = BepInEx.Logging.Logger.CreateLogSource(nameof(HostInventoryController));
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
                    NetId = FikaPlayer.NetId,
                    TraderId = traderId
                }
            };
            Singleton<FikaClient>.Instance.SendData(ref request, DeliveryMethod.ReliableOrdered);
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
        RunHostOperation(operation, callback);
    }

    public override Task<IResult> LoadMagazine(AmmoItemClass sourceAmmo, MagazineItemClass magazine, int loadCount, bool ignoreRestrictions)
    {
        if (_instantLoad)
        {
            if (Singleton<GUISounds>.Instantiated)
            {
                Singleton<GUISounds>.Instance.PlayUILoadSound();
            }
            var gstruct = (ignoreRestrictions ? magazine.ApplyWithoutRestrictions(this, sourceAmmo, int.MaxValue, true) : magazine.Apply(this, sourceAmmo, int.MaxValue, true));
            return TryRunNetworkTransaction(gstruct, null);
        }

        return base.LoadMagazine(sourceAmmo, magazine, loadCount, ignoreRestrictions);
    }

    private void RunHostOperation(BaseInventoryOperationClass operation, Callback callback)
    {
        // Do not replicate picking up quest items, throws an error on the other clients            
        if (operation is MoveOperationClass moveOperation)
        {
            var lootedItem = moveOperation.Item;
            if (lootedItem.QuestItem)
            {
                if (FikaPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController && sharedQuestController.ContainsAcceptedType("PlaceBeacon"))
                {
                    if (!sharedQuestController.CheckForTemplateId(lootedItem.TemplateId))
                    {
                        sharedQuestController.AddLootedTemplateId(lootedItem.TemplateId);

                        // We use templateId because each client gets a unique itemId
                        QuestItemPacket packet = new()
                        {
                            Nickname = FikaPlayer.Profile.Info.MainProfileNickname,
                            ItemId = lootedItem.TemplateId,
                        };
                        FikaPlayer.PacketSender.NetworkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
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
        if (operation is GClass3493)
        {
            base.vmethod_1(operation, callback);
            return;
        }

        var handler = _hostInventoryOperationHandlerPool.Get();
        handler.Set(this, operation, callback);
        try
        {
            if (vmethod_0(handler.Operation))
            {
                handler.Operation.method_1(handler.HandleResultDelegate);
                FikaPlayer.PacketSender.NetworkManager.SendGenericPacket(EGenericSubPacketType.InventoryOperation,
                    InventoryPacket.FromValue(FikaPlayer.NetId, operation), true);
                return;
            }
            handler.Operation.Dispose();
            handler.Callback?.Fail($"Can't execute {handler.Operation}", 1);
        }
        finally
        {
            _hostInventoryOperationHandlerPool.ReturnHandler(handler);
        }
    }

    public override bool HasCultistAmulet(out CultistAmuletItemClass amulet)
    {
        amulet = null;
        using var enumerator = Inventory.GetItemsInSlots([EquipmentSlot.Pockets]).GetEnumerator();
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

    public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
    {
        return new SearchContentOperationResultClass(method_12(), this, PlayerSearchController, Profile, item);
    }

    public void ClearPool()
    {
        _hostInventoryOperationHandlerPool.Dispose();
    }
}