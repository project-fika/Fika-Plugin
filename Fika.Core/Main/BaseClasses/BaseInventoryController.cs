using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;
using Diz.LanguageExtensions;
using Diz.Utils;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.BaseClasses;

/// <summary>
/// Base inventory controller made for Fika
/// </summary>
public class BaseInventoryController : Player.PlayerOwnerInventoryController
{
    public BaseInventoryController(IntPtr pointer) : base(pointer)
    {
    }

    /// <summary>
    /// Whether strict inventory syncing is active
    /// </summary>
    public bool StrictSync { get; }

    protected readonly bool _instantLoad;
    protected readonly bool _fastLoad;

    protected BaseInventoryController(IntPtr pointer, Player player, Profile profile, bool examined, bool strictSync) : base(pointer)
    {
        ClassInjector.DerivedConstructorBody(this);
        _instantLoad = FikaPlugin.Instance.Settings.InstantLoad;
        _fastLoad = !_instantLoad && FikaPlugin.Instance.Settings.FastLoad;
        ClassInjector.InvokeBaseConstructor<Player.PlayerOwnerInventoryController>(this, player, profile, examined);
        StrictSync = strictSync;
    }

    public BaseInventoryController(Player player, Profile profile, bool examined, bool strictSync) : this(Il2CppInjection.Allocate<BaseInventoryController>(), player, profile, examined, strictSync)
    {
    }

    public override SearchContentOperation CreateSearchOperation(SearchableItem item)
    {
        throw new NotImplementedException();
    }

    public override void RemoveItem(Item item, Callback callback)
    {
        TryRunNetworkTransaction(ItemManipulator.Remove(item, this, true), callback);
    }

    public override Il2CppSystem.Threading.Tasks.Task<IResult> LoadMagazine(Ammo sourceAmmo, Magazine magazine, int loadCount, bool ignoreRestrictions)
    {
        if (_instantLoad)
        {
            if (Singleton<GUISounds>.Instantiated)
            {
                Singleton<GUISounds>.Instance.PlayUILoadSound();
            }

            var gstruct = ignoreRestrictions
                ? magazine.ApplyWithoutRestrictions(this, sourceAmmo, int.MaxValue, true)
                : magazine.Apply(this, sourceAmmo, int.MaxValue, true);

            return TryRunNetworkTransaction(gstruct, null);
        }

        if (_fastLoad)
        {
            return QuickLoadMagazine(sourceAmmo, magazine, loadCount, ignoreRestrictions).ToIl2Cpp();
        }

        return base.LoadMagazine(sourceAmmo, magazine, loadCount, ignoreRestrictions);
    }

    private async Task<IResult> QuickLoadMagazine(Ammo sourceAmmo, Magazine magazine, int loadCount, bool ignoreRestrictions)
    {
        if (loadCount <= 0)
        {
            return new FailedResult("Can not load 0 bullets.", 0);
        }

        StopProcesses();

        var speedPercentage = 100f - Profile.Skills.MagDrillsLoadSpeed + magazine.LoadUnloadModifier;
        var finalLoadSpeed = Singleton<GlobalConfiguration>.Instance.BaseLoadTime * speedPercentage / 100f;
        var loadPerTick = GetLoadPerTick(magazine);

        var operationResult = ignoreRestrictions
            ? magazine.ApplyWithoutRestrictions(this, sourceAmmo, loadPerTick, true)
            : magazine.Apply(this, sourceAmmo, loadPerTick, true);

        if (operationResult.Failed || !CanExecute(operationResult.Value))
        {
            return operationResult.ToResult();
        }

        var readinessResult = await WaitForProcess();
        if (readinessResult.Failed)
        {
            return readinessResult;
        }

        var loader = new CustomAmmoLoader(this, magazine, sourceAmmo, loadCount,
            Profile.Skills.MagDrillsLoadProgression, finalLoadSpeed, loadPerTick);
        _loadProcess = loader;

        var executionResult = await loader.StartLoading();

        _loadProcess = null;

        return executionResult;
    }

    public override Il2CppSystem.Threading.Tasks.Task<IResult> UnloadMagazine(Magazine magazine, bool equipmentBlocked)
    {
        if (_instantLoad)
        {
            return UnloadAmmoInstantly(magazine, equipmentBlocked);
        }

        if (_fastLoad)
        {
            return QuickUnloadMagazine(magazine).ToIl2Cpp();
        }

        return base.UnloadMagazine(magazine, equipmentBlocked);
    }

    private async Task<IResult> QuickUnloadMagazine(Magazine magazine)
    {
        StopProcesses();
        var unloadSpeed = 100f - Profile.Skills.MagDrillsUnloadSpeed + magazine.LoadUnloadModifier;
        var unloadOneAmmoSpeed = Singleton<GlobalConfiguration>.Instance.BaseUnloadTime * unloadSpeed / 100f;
        var unloadPerTick = GetLoadPerTick(magazine);

        var awaitClear = await WaitForProcess();
        if (awaitClear.Failed)
        {
            return awaitClear;
        }

        var unloader = new CustomAmmoUnloader(this, magazine, unloadPerTick, unloadOneAmmoSpeed, Profile.Skills.MagDrillsLoadProgression);
        _loadProcess = unloader;
        var result = await unloader.StartUnloading();

        _loadProcess = null;

        return result;
    }

    /// <summary>
    /// Returns how many bullets should be loaded per tick into the <paramref name="magazine"/>
    /// </summary>
    /// <param name="magazine">The magazine to check the <see cref="Magazine.MaxCount"/> on</param>
    /// <returns>The amount of bullets to load per tick</returns>
    private static int GetLoadPerTick(Magazine magazine)
    {
        var maxCount = magazine.MaxCount;
        if (maxCount <= 5)
        {
            return 1;
        }

        if (maxCount == 10)
        {
            return 2;
        }

        if (maxCount < 20)
        {
            return 3;
        }

        return 5;
    }

    public override IPlayerSearchController PlayerSearchController { get; }

    private sealed class CustomAmmoLoader : Il2CppSystem.Object, IMagazineLoadingProcess
    {
        private readonly InventoryController _inventoryController;
        private readonly Magazine _magazine;
        private readonly Ammo _sourceAmmo;
        private readonly int _totalLoadCount;
        private readonly bool _isElite;
        private readonly float _baseLoadSpeed;
        private readonly int _loadPerTick;

        private readonly IItemOwner _magazineOwner;
        private readonly IItemOwner _ammoOwner;

        private CancellationTokenSource _cts;
        private float _currentLoadSpeed;

        public bool IsCancelled => _cts?.IsCancellationRequested != false;

        public CustomAmmoLoader(IntPtr pointer) : base(pointer)
        {
        }

        public CustomAmmoLoader(InventoryController inventoryController, Magazine magazine, Ammo sourceAmmo,
            int count, bool elite, float loadOneAmmoSpeed, int loadPerTick) : base(Il2CppInjection.Allocate<CustomAmmoLoader>())
        {
            ClassInjector.DerivedConstructorBody(this);
            _inventoryController = inventoryController;
            _magazine = magazine;
            _sourceAmmo = sourceAmmo;
            _totalLoadCount = count;
            _isElite = elite;
            _baseLoadSpeed = loadOneAmmoSpeed;
            _currentLoadSpeed = loadOneAmmoSpeed;

            _magazineOwner = _magazine.Parent.GetOwner();
            _ammoOwner = _sourceAmmo.Parent.GetOwner();
            _loadPerTick = loadPerTick;
        }

        public Il2CppSystem.Threading.Tasks.Task<IResult> Start()
        {
            return StartLoading().ToIl2Cpp();
        }

        public async Task<IResult> StartLoading()
        {
            ResetToken();
            _cts = new CancellationTokenSource();

            var cancellationHandlerSource = new TaskCompletionSource<IResult>();
            _cts.Token.Register(() => cancellationHandlerSource.TrySetResult(SuccessfulResult.New));
            RaiseEvents(CommandStatus.Begin);
            var result = await await Task.WhenAny(DoLoadLoopAsync(), cancellationHandlerSource.Task);

            Proceed(result.Succeed);
            return result;
        }

        public void Proceed(bool success = true)
        {
            if (_cts?.IsCancellationRequested != false)
            {
                return;
            }

            ResetToken();
            RaiseEvents(success ? CommandStatus.Succeed : CommandStatus.Failed);
            RefreshIcons(false);
        }

        public void ResetToken()
        {
            if (_cts == null)
            {
                return;
            }

            _cts.Cancel(false);
            _cts.Dispose();
            _cts = null;
        }

        public void TryProceedForItem(Item item)
        {
            if (_magazine == item || _sourceAmmo == item)
            {
                Proceed(true);
            }
        }

        public void RaiseEvents(CommandStatus status)
        {
            var loadCount = Mathf.CeilToInt((float)_totalLoadCount / _loadPerTick);
            var geventArgs = new LoadMagazineEventArgs(_sourceAmmo, _magazine,
                loadCount, _baseLoadSpeed, status, _inventoryController);
            _magazineOwner.RaiseLoadMagazineEvent(geventArgs);

            if (_magazineOwner != _inventoryController)
            {
                _inventoryController.RaiseLoadMagazineEvent(geventArgs);
            }

            if (_ammoOwner != _magazineOwner)
            {
                _ammoOwner.RaiseLoadMagazineEvent(geventArgs);
            }
        }

        public async Task<IResult> DoLoadLoopAsync()
        {
            var loadedCount = 0;

            while (loadedCount < _totalLoadCount)
            {
                await Task.Delay(Mathf.CeilToInt(_currentLoadSpeed * 1000f));

                if (IsCancelled)
                {
                    break;
                }

                if (_isElite)
                {
                    var progressModifier = Singleton<GlobalConfiguration>.Instance.LoadTimeSpeedProgress;
                    _currentLoadSpeed = Mathf.Clamp(_currentLoadSpeed - (_baseLoadSpeed * progressModifier / 100f), _baseLoadSpeed * 40f / 100f, 10f);
                }

                var gstruct = _magazine.ApplyWithoutRestrictions(_inventoryController, _sourceAmmo, _loadPerTick, true);

                if (gstruct.Failed)
                {
                    return gstruct.ToResult();
                }

                var operation = _inventoryController.ConvertOperationResultToOperation(gstruct.Value);
                var executionSource = new TaskCompletionSource<IResult>();

                _inventoryController.Execute(operation, new System.Action<Comfort.Common.IResult>(executionSource.SetResult));

                var operationResult = await executionSource.Task;

                if (operationResult.Failed)
                {
                    return operationResult;
                }

                PlayLoadSound();
                RefreshIcons(loadedCount == _totalLoadCount - 1);

                if (IsCancelled)
                {
                    break;
                }

                // increment loop by the amount loaded per tick instead of single units
                loadedCount += _loadPerTick;
            }
            return SuccessfulResult.New;
        }

        public void RefreshIcons(bool refreshIcon = false)
        {
            if (_sourceAmmo.CurrentAddress != null)
            {
                _sourceAmmo.RaiseRefreshEvent(refreshIcon, true);
            }
            _magazine.RaiseRefreshEvent(refreshIcon, true);
        }

        public void PlayLoadSound()
        {
            if (!Singleton<GUISounds>.Instantiated)
            {
                return;
            }
            Singleton<GUISounds>.Instance.PlayUILoadSound();
        }
    }

    private sealed class CustomAmmoUnloader : Il2CppSystem.Object, IMagazineLoadingProcess
    {
        private readonly BaseInventoryController _inventoryController;
        private readonly Magazine _magazine;
        private readonly int _unloadPerTick;
        private readonly float _baseUnloadSpeed;
        private readonly bool _isElite;
        private readonly int _totalAmmoCount;

        private float _currentUnloadSpeed;
        private int _remainingAmmoCount;
        private CancellationTokenSource _cts;
        private Item _currentAmmoItem;
        private Item _targetItem;

        public CustomAmmoUnloader(IntPtr pointer) : base(pointer)
        {
        }

        public CustomAmmoUnloader(BaseInventoryController baseInventoryController, Magazine magazine, int unloadPerTick, float unloadOneAmmoSpeed, bool isElite)
            : base(Il2CppInjection.Allocate<CustomAmmoUnloader>())
        {
            ClassInjector.DerivedConstructorBody(this);
            _inventoryController = baseInventoryController;
            _magazine = magazine;
            _unloadPerTick = unloadPerTick;
            _baseUnloadSpeed = unloadOneAmmoSpeed;
            _isElite = isElite;

            _currentUnloadSpeed = unloadOneAmmoSpeed;
            _totalAmmoCount = magazine.Cartridges.Items.Sum(i => i.StackObjectsCount);
            _remainingAmmoCount = _totalAmmoCount;
        }

        public Il2CppSystem.Threading.Tasks.Task<IResult> Start()
        {
            return StartUnloading().ToIl2Cpp();
        }

        public async Task<IResult> StartUnloading()
        {
            Cancel();

            if (_remainingAmmoCount == 0)
            {
                return new AmmoContainerIsEmptyError(_magazine).ToResult();
            }

            _cts = new CancellationTokenSource();
            var cancellationTcs = new TaskCompletionSource<IResult>();

            _cts.Token.Register(() => cancellationTcs.TrySetResult(SuccessfulResult.New));

            var loopResult = await await Task.WhenAny(DoUnloadLoopAsync(), cancellationTcs.Task);

            Proceed(loopResult.Succeed);
            return loopResult;
        }

        public void Cancel()
        {
            if (_cts == null)
            {
                return;
            }

            _cts.Cancel(false);
            _cts.Dispose();
            _cts = null;
        }

        public void Proceed(bool success)
        {
            if (_cts?.IsCancellationRequested != false)
            {
                return;
            }

            Cancel();
            RaiseUnloadEvent(success ? CommandStatus.Succeed : CommandStatus.Failed);
        }

        public void TryProceedForItem(Item item)
        {
            if (_magazine == item || _currentAmmoItem == item || _targetItem == item)
            {
                Proceed(true);
            }
        }

        private void PlayUnloadSound()
        {
            if (!Singleton<GUISounds>.Instantiated)
            {
                return;
            }
            Singleton<GUISounds>.Instance.PlayUIUnloadSound();
        }

        private async Task<IResult> DoUnloadLoopAsync()
        {
            var delayTask = GetDelayTask();

            while (!_cts.IsCancellationRequested)
            {
                if (_magazine.Cartridges.Items.LastOrDefault() is not Ammo ammoItem)
                {
                    break;
                }

                var findPlaceResult = ItemManipulator.QuickFindAppropriatePlace(ammoItem, _inventoryController,
                    _inventoryController.Inventory.Equipment.AsIl2CppEnumerable<CompoundItem>(),
                    ItemManipulator.EMoveItemOrder.UnloadAmmo, true);

                if (findPlaceResult.Failed)
                {
                    return findPlaceResult.ToResult();
                }

                ItemAddress targetAddress = null;
                Item targetItem = null;
                var interactionValue = findPlaceResult.Value;

                if (interactionValue is IToEmptyAddressResult moveOperation)
                {
                    targetAddress = moveOperation.To;
                }
                else if (interactionValue is ITransferOrMergeResult mergeOperation)
                {
                    targetItem = mergeOperation.TargetItem;
                }

                if (targetAddress == null && targetItem == null)
                {
                    break;
                }

                if (_currentAmmoItem != ammoItem || targetItem != _targetItem)
                {
                    if (_currentAmmoItem != null)
                    {
                        RaiseUnloadEvent(CommandStatus.Succeed);
                    }
                    _currentAmmoItem = ammoItem;
                    _targetItem = targetItem;
                    RaiseUnloadEvent(CommandStatus.Begin);
                }

                await delayTask;

                if (_cts.IsCancellationRequested)
                {
                    break;
                }

                delayTask = GetDelayTask();

                if (_isElite)
                {
                    var speedModifier = _baseUnloadSpeed * Singleton<GlobalConfiguration>.Instance.LoadTimeSpeedProgress / 100f;
                    _currentUnloadSpeed = Mathf.Clamp(_currentUnloadSpeed - speedModifier, _baseUnloadSpeed * 40f / 100f, 10f);
                }

                var applyResult = (_targetItem != null)
                    ? Ammo.ApplyToAmmo(_currentAmmoItem, _targetItem, _unloadPerTick, _inventoryController, true)
                    : Ammo.ApplyToAddress(_currentAmmoItem, targetAddress, _unloadPerTick, _inventoryController, true);

                if (applyResult.Failed)
                {
                    return applyResult.ToResult();
                }

                IUnloadMagOperationResult operationResult = new UnloadMagOperationResult(applyResult.Value);
                var operation = _inventoryController.ConvertOperationResultToOperation(operationResult) as UnloadMagOperation;

                var executionTcs = new TaskCompletionSource<IResult>();

                _inventoryController.Execute(operation, new Action<IResult>(res => executionTcs.SetResult(res)));

                var executionResult = await executionTcs.Task;
                if (executionResult.Failed)
                {
                    return executionResult;
                }

                _remainingAmmoCount -= _unloadPerTick;
                _currentAmmoItem.RaiseRefreshEvent(false, true);
                _magazine.RaiseRefreshEvent(_remainingAmmoCount == 0, true);
                _targetItem?.RaiseRefreshEvent(false, true);

                PlayUnloadSound();
            }

            return SuccessfulResult.New;
        }

        private void RaiseUnloadEvent(CommandStatus status)
        {
            var owner = _magazine.Parent.GetOwner();
            var unloadCount = Mathf.CeilToInt((float)_totalAmmoCount / _unloadPerTick);

            var eventArgs = new UnloadMagazineEventArgs(_currentAmmoItem, _targetItem, _magazine, _totalAmmoCount - _remainingAmmoCount, unloadCount,
                                            _baseUnloadSpeed, status, _inventoryController);

            owner.RaiseUnloadMagazineEvent(eventArgs);
            if (owner != _inventoryController)
            {
                _inventoryController.RaiseUnloadMagazineEvent(eventArgs);
            }
        }

        private Task GetDelayTask()
        {
            return Task.Delay(Mathf.CeilToInt(_currentUnloadSpeed * 1000f));
        }
    }
}

