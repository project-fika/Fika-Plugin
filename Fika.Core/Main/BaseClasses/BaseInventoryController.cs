using System;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;

namespace Fika.Core.Main.BaseClasses;

/// <summary>
/// Base inventory controller made for Fika
/// </summary>
public class BaseInventoryController : Player.PlayerOwnerInventoryController
{
    /// <summary>
    /// Whether strict inventory syncing is active
    /// </summary>
    public bool StrictSync { get; }

    protected readonly bool _instantLoad;
    protected readonly bool _fastLoad;

    public BaseInventoryController(Player player, Profile profile, bool examined, bool strictSync) : base(player, profile, examined)
    {
        _instantLoad = FikaPlugin.Instance.Settings.InstantLoad;
        _fastLoad = !_instantLoad && FikaPlugin.Instance.Settings.FastLoad;
        StrictSync = strictSync;
    }

    public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
    {
        throw new NotImplementedException();
    }

    public override Task<IResult> LoadMagazine(AmmoItemClass sourceAmmo, MagazineItemClass magazine, int loadCount, bool ignoreRestrictions)
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
            return QuickLoadMagazine(sourceAmmo, magazine, loadCount, ignoreRestrictions);
        }

        return base.LoadMagazine(sourceAmmo, magazine, loadCount, ignoreRestrictions);
    }

    private async Task<IResult> QuickLoadMagazine(AmmoItemClass sourceAmmo, MagazineItemClass magazine, int loadCount, bool ignoreRestrictions)
    {
        if (loadCount <= 0)
        {
            return new FailedResult("Can not load 0 bullets.", 0);
        }

        StopProcesses();

        var speedPercentage = 100f - Profile.Skills.MagDrillsLoadSpeed + magazine.LoadUnloadModifier;
        var finalLoadSpeed = Singleton<BackendConfigSettingsClass>.Instance.BaseLoadTime * speedPercentage / 100f;
        var loadPerTick = GetLoadPerTick(magazine);

        var operationResult = ignoreRestrictions
            ? magazine.ApplyWithoutRestrictions(this, sourceAmmo, loadPerTick, true)
            : magazine.Apply(this, sourceAmmo, loadPerTick, true);

        if (operationResult.Failed || !CanExecute(operationResult.Value))
        {
            return operationResult.ToResult();
        }

        var readinessResult = await method_30();
        if (readinessResult.Failed)
        {
            return readinessResult;
        }

        Interface19_0 = new CustomAmmoLoader(this, magazine, sourceAmmo, loadCount,
            Profile.Skills.MagDrillsLoadProgression, finalLoadSpeed, loadPerTick);

        var executionResult = await Interface19_0.Start();

        Interface19_0 = null;

        return executionResult;
    }

    /// <summary>
    /// Returns how many bullets should be loaded per tick into the <paramref name="magazine"/>
    /// </summary>
    /// <param name="magazine">The magazine to check the <see cref="MagazineItemClass.MaxCount"/> on</param>
    /// <returns>The amount of bullets to load per tick</returns>
    private static int GetLoadPerTick(MagazineItemClass magazine)
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

    private sealed class CustomAmmoLoader : Interface19
    {
        private readonly InventoryController _inventoryController;
        private readonly MagazineItemClass _magazine;
        private readonly AmmoItemClass _sourceAmmo;
        private readonly int _totalLoadCount;
        private readonly bool _isElite;
        private readonly float _baseLoadSpeed;
        private readonly int _loadPerTick;

        private readonly IItemOwner _magazineOwner;
        private readonly IItemOwner _ammoOwner;

        private CancellationTokenSource _cts;
        private float _currentLoadSpeed;

        public bool IsCancelled => _cts?.IsCancellationRequested != false;

        public CustomAmmoLoader(InventoryController inventoryController, MagazineItemClass magazine, AmmoItemClass sourceAmmo,
            int count, bool elite, float loadOneAmmoSpeed, int loadPerTick)
        {
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

        public async Task<IResult> Start()
        {
            ResetToken();
            _cts = new CancellationTokenSource();

            var cancellationHandlerSource = new TaskCompletionSource<IResult>();
            _cts.Token.Register(cancellationHandlerSource.Succeed);
            RaiseEvents(CommandStatus.Begin);
            var result = await await Task.WhenAny(DoLoadLoop(), cancellationHandlerSource.Task);

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
            var geventArgs = new GEventArgs7(_sourceAmmo, _magazine,
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

        public async Task<IResult> DoLoadLoop()
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
                    var progressModifier = Singleton<BackendConfigSettingsClass>.Instance.LoadTimeSpeedProgress;
                    _currentLoadSpeed = Mathf.Clamp(_currentLoadSpeed - (_baseLoadSpeed * progressModifier / 100f), _baseLoadSpeed * 40f / 100f, 10f);
                }

                var gstruct = _magazine.ApplyWithoutRestrictions(_inventoryController, _sourceAmmo, _loadPerTick, true);

                if (gstruct.Failed)
                {
                    return gstruct.ToResult();
                }

                var operation = _inventoryController.ConvertOperationResultToOperation(gstruct.Value);
                var executionSource = new TaskCompletionSource<IResult>();

                _inventoryController.vmethod_1(operation, executionSource.SetResult);

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
}
