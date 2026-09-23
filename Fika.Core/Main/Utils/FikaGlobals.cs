using Diz.Jobs;
using EFT.Settings;
using EFT.Settings.Sound;
using JsonType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using HarmonyLib;
using static Fika.Core.FikaPlugin;
using static Fika.Core.Networking.IFikaNetworkManager;

namespace Fika.Core.Main.Utils;

/// <summary>
/// Global utility constants, properties, and helper methods used across the Fika plugin.
/// </summary>
public static class FikaGlobals
{
    /// <summary>
    /// The MongoDB ObjectId string for the transit trader (BTR).
    /// </summary>
    public const string TransitTraderId = "656f0f98d80a697f855d34b1";

    /// <summary>
    /// The display and lookup name of the transit trader.
    /// </summary>
    public const string TransitTraderName = "BTR";

    /// <summary>
    /// The default transit location identifier.
    /// </summary>
    public const string DefaultTransitId = "66f5750951530ca5ae09876d";

    /// <summary>
    /// The group identifier assigned to players participating in a Fika co-op session.
    /// </summary>
    public const string FikaGroupId = "Fika";

    /// <summary>
    /// Layer mask used for raycasting pings against the environment and entities.
    /// </summary>
    public static int PingMask = LayerMask.GetMask(["HighPolyCollider", "Interactive", "Deadbody", "Player", "Loot", "Terrain"]);

    /// <summary>
    /// Use when no callback is needed to reduce allocations
    /// </summary>
    public static Callback EmptyCallbackDelegate => EmptyCallback;
    /// <summary>
    /// Use when no callback is needed to reduce allocations
    /// </summary>
    public static Action EmptyActionDelegate => EmptyAction;

    /// <summary>
    /// The maximum raycast distance in meters for placing pings.
    /// </summary>
    public const int PingRange = 1000;

    private static readonly ManualLogSource _logger = Logger.CreateLogSource("FikaGlobals");

    /// <summary>
    /// Interactions that are blocked from being executed in co-op.
    /// </summary>
    internal static readonly List<EInteraction> BlockedInteractions =
    [
        EInteraction.DropBackpack, EInteraction.NightVisionOffGear, EInteraction.NightVisionOnGear,
        EInteraction.FaceshieldOffGear, EInteraction.FaceshieldOnGear, EInteraction.BipodForwardOn,
        EInteraction.BipodForwardOff, EInteraction.BipodBackwardOn, EInteraction.BipodBackwardOff
    ];

    /// <summary>
    /// Equipment slots that represent weapon slots.
    /// </summary>
    internal static readonly List<EquipmentSlot> WeaponSlots =
    [
        EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster
    ];

    /// <summary>
    /// Gets the search controller serializer singleton where all items are treated as fully searched.
    /// </summary>
    public static ISearchController SearchControllerSerializer
    {
        get
        {
            return FullySearchedSearchController.Instance;
        }
    }

    /// <summary>
    /// Gets the global EFT <see cref="InputTree"/> component.
    /// </summary>
    public static InputTree InputTree
    {
        get
        {
            if (_inputTree == null)
            {
                var inputObj = GameObject.Find("___Input")
                    ?? throw new NullReferenceException("Could not find InputTree object!");

                _inputTree = inputObj.GetComponent<InputTree>();
            }

            return _inputTree;
        }
    }

    private static InputTree _inputTree;

    /// <summary>
    /// Gets the VoIP settings handler, initializing default push-to-talk and quality settings if needed.
    /// </summary>
    public static VoipSettings VOIPHandler
    {
        get
        {
            if (_voipHandler == null)
            {
                _voipHandler = VoipSettings.Default;
                _voipHandler.VoipQualitySettings.Apply();
                _voipHandler.MicrophoneChecked = SoundSettingsGroup.CheckMicrophone();
                _voipHandler.VoipEnabled = true;
                var pttSettings = _voipHandler.PushToTalkSettings;
                pttSettings.SpeakingSecondsLimit = 20f;
                pttSettings.BlockingTime = 5f;
            }

            return _voipHandler;
        }
    }

    /// <summary>
    /// Checks whether the game client is in a raid
    /// </summary>
    /// <returns><see langword="true"/> if the client is currently in a raid; otherwise, <see langword="false"/>.</returns>
    public static bool IsInRaid
    {
        get
        {
            return Singleton<IFikaGame>.Instantiated;
        }
    }

    private static VoipSettings _voipHandler;

    /// <summary>
    /// Gets the default mouse sensitivity multiplier applied to other players.
    /// </summary>
    /// <returns>A sensitivity float value of 1.0f.</returns>
    internal static float GetOtherPlayerSensitivity()
    {
        return 1f;
    }

    /// <summary>
    /// Gets the local player's configured mouse sensitivity from game control settings.
    /// </summary>
    /// <returns>The mouse sensitivity value.</returns>
    internal static float GetLocalPlayerSensitivity()
    {
        return Singleton<SettingsManager>.Instance.Control.Settings.MouseSensitivity;
    }

    /// <summary>
    /// Gets the local player's configured mouse aiming sensitivity from game control settings.
    /// </summary>
    /// <returns>The mouse aiming sensitivity value.</returns>
    internal static float GetLocalPlayerAimingSensitivity()
    {
        return Singleton<SettingsManager>.Instance.Control.Settings.MouseAimingSensitivity;
    }

    /// <summary>
    /// Gets the current application runtime in seconds since the game started.
    /// </summary>
    /// <returns>Current application time in seconds.</returns>
    public static float GetApplicationTime()
    {
        return Time.time;
    }

    /// <summary>
    /// Checks whether the lamp controller has a non-zero network ID.
    /// </summary>
    /// <param name="controller">The <see cref="LampController"/> to check.</param>
    /// <returns><see langword="true"/> if the lamp controller's NetId is not 0; otherwise, <see langword="false"/>.</returns>
    internal static bool LampControllerNetIdNot0(LampController controller)
    {
        return controller.NetId != 0;
    }

    /// <summary>
    /// Gets the network ID of the specified lamp controller.
    /// </summary>
    /// <param name="controller">The <see cref="LampController"/> to inspect.</param>
    /// <returns>The network ID of the lamp controller.</returns>
    internal static int LampControllerGetNetId(LampController controller)
    {
        return controller.NetId;
    }

    /// <summary>
    /// Checks whether a window breaker is available to synchronize.
    /// </summary>
    /// <param name="breaker">The <see cref="WindowBreaker"/> to check.</param>
    /// <returns><see langword="true"/> if available to synchronize; otherwise, <see langword="false"/>.</returns>
    internal static bool WindowBreakerAvailableToSync(WindowBreaker breaker)
    {
        return breaker.AvailableToSync;
    }

    /// <summary>
    /// Retrieves the underlying <see cref="Item"/> from a <see cref="JsonLootItem"/>.
    /// </summary>
    /// <param name="positionClass">The JSON loot item wrapper.</param>
    /// <returns>The contained <see cref="Item"/>.</returns>
    internal static Item GetLootItemPositionItem(JsonLootItem positionClass)
    {
        return positionClass.Item;
    }

    /// <summary>
    /// Gets the body part type corresponding to the specified collider.
    /// </summary>
    /// <param name="collider">The body part collider.</param>
    /// <returns>The corresponding <see cref="EBodyPart"/> type.</returns>
    internal static EBodyPart GetBodyPartFromCollider(BodyPartCollider collider)
    {
        return collider.BodyPartType;
    }

    /// <summary>
    /// Formats a byte size into a human-readable string representation with appropriate unit suffixes (B, KB, MB, etc.).
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>A formatted string representing the file size.</returns>
    internal static string FormatFileSize(long bytes)
    {
        const int unit = 1024;
        if (bytes < unit) { return $"{bytes} B"; }

        var exp = (int)(Math.Log(bytes) / Math.Log(unit));
        return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
    }

    /// <summary>
    /// Asynchronously loads asset bundles and pools for an item and sets up/spawns it in the world in front of the player.
    /// </summary>
    /// <param name="item">The item to spawn.</param>
    /// <param name="player">The player in front of whom the item will be spawned.</param>
    internal static void SpawnItemInWorld(Item item, FikaPlayer player)
    {
        StaticManager.BeginCoroutine(SpawnItemRoutine(item, player));
    }

    /// <summary>
    /// Coroutine that loads item bundles and creates pools, then instantiates the item in the world.
    /// </summary>
    /// <param name="item">The item to spawn.</param>
    /// <param name="player">The player in front of whom the item will be spawned.</param>
    /// <returns>An enumerator for coroutine progression.</returns>
    private static IEnumerator SpawnItemRoutine(Item item, FikaPlayer player)
    {
        List<ResourceKey> collection = [];
        foreach (var subItem in item.GetAllItems())
        {
            collection.AddRange(subItem.Template.AllResources);
        }
        var loadTask = Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(ObjectsFactory.PoolsCategory.Raid, ObjectsFactory.AssemblyType.Online,
            [.. collection], JobYieldPriority.Immediate, null, default);

        WaitForEndOfFrame waitForEndOfFrame = new();
        while (!loadTask.IsCompleted)
        {
            yield return waitForEndOfFrame;
        }

        Singleton<GameWorld>.Instance.SetupItem(item, player,
            player.Transform.Original.position + player.Transform.Original.forward + (player.Transform.Original.up / 2), Quaternion.identity);

        if (player.IsYourPlayer)
        {
            ConsoleScreen.Log("Spawned item: " + item.ShortName.Localized());
            yield break;
        }
        ConsoleScreen.Log($"{player.Profile.Info.Nickname} has spawned item: {item.ShortName.Localized()}");
    }

    /// <summary>
    /// Forces the <see cref="ProfileInfo.MainProfileNickname"/> to be set on a profile
    /// </summary>
    /// <param name="infoClass">The profile information instance to modify.</param>
    /// <param name="nickname">The nickname to assign to the main profile.</param>
    public static void SetProfileNickname(this ProfileInfo infoClass, string nickname)
    {
        infoClass.MainProfileNickname = nickname;
    }

    /// <summary>
    /// Checks whether a profile belongs to a player or an AI
    /// </summary>
    /// <param name="profile">The profile to inspect.</param>
    /// <returns><see langword="true"/> if the profile belongs to a player; otherwise, <see langword="false"/> if it belongs to an AI.</returns>
    public static bool IsPlayerProfile(this Profile profile)
    {
        return !string.IsNullOrEmpty(profile.PetId) || profile.Info.RegistrationDate > 0 || !string.IsNullOrEmpty(profile.Info.MainProfileNickname);
    }

    /// <summary>
    /// Gets the current <see cref="IEftSession"/>
    /// </summary>
    /// <returns><see cref="IEftSession"/> of the application</returns>
    public static IEftSession GetSession()
    {
        if (TarkovApplication.Exist(out var tarkovApplication))
        {
            return tarkovApplication.Session;
        }

        _logger.LogError("GetSession: Could not find TarkovApplication!");
        return null;
    }

    /// <summary>
    /// Gets the current PMC or scav profile
    /// </summary>
    /// <param name="scav">If the scav profile should be returned</param>
    /// <returns><see cref="Profile"/> of chosen side</returns>
    public static Profile GetProfile(bool scav)
    {
        var session = GetSession();
        if (session == null)
        {
            _logger.LogError("GetProfile: Session was null!");
            return null;
        }

        if (!scav)
        {
            return session.Profile;
        }

        return session.ProfileOfPet;
    }

    /// <summary>
    /// Gets the current PMC or scav profile with trimmed data
    /// </summary>
    /// <param name="scav">If the scav profile should be returned</param>
    /// <returns>A trimmed <see cref="Profile"/> of chosen side</returns>
    public static Profile GetLiteProfile(bool scav)
    {
        var profile = GetProfile(scav);
        ProfileDescriptor liteDescriptor = new(profile, SearchControllerSerializer)
        {
            Encyclopedia = [],
            InsuredItems = [],
            TaskConditionCounters = []
        };
        return new(liteDescriptor);
    }

    /// <summary>
    /// Gets the states from a <see cref="TacticalComboVisualController"/>
    /// </summary>
    /// <param name="controller">The controller to inspect.</param>
    /// <returns><see cref="LightsState"/></returns>
    public static LightsState GetFirearmLightStates(TacticalComboVisualController controller)
    {
        return controller.LightMod.GetLightState(false, false);
    }

    /// <summary>
    /// Gets the contained item in a <see cref="Slot"/>
    /// </summary>
    /// <param name="slot">The <see cref="Slot"/> to check</param>
    /// <returns>An <see cref="Item"/> in the slot</returns>
    public static Item GetContainedItem(Slot slot)
    {
        return slot.ContainedItem;
    }

    /// <summary>
    /// Gets a light states from a <see cref="LightComponent"/>
    /// </summary>
    /// <param name="component">The <see cref="LightComponent"/> to check</param>
    /// <returns>A new <see cref="LightsState"/> with data</returns>
    public static LightsState GetFirearmLightStatesFromComponent(LightComponent component)
    {
        return new LightsState
        {
            Id = component.Item.Id,
            IsActive = component.IsActive,
            LightMode = component.SelectedMode
        };
    }

    /// <summary>
    /// Checks whether the player is part of the player group
    /// </summary>
    /// <param name="player">The <see cref="Player"/> to check</param>
    /// <returns>True if in the player group</returns>
    public static bool IsGroupMember(this Player player)
    {
        return string.Equals(player.GroupId, FikaGroupId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Unsubscribes all delegates from an <see cref="Action"/>
    /// </summary>
    /// <param name="action">The action delegate to clear.</param>
    /// <returns>The cleared action delegate.</returns>
    public static Action ClearDelegates(Action action)
    {
        var list = action.GetInvocationList();
        for (var i = 0; i < list.Length; i++)
        {
#if DEBUG
            LogWarning($"Clearing {list[i].Method.Name}");
#endif
            action = (Action)Delegate.Remove(action, list[i]);
        }

        return action;
    }

    /// <summary>
    /// Unsubscribes all delegates from an <see cref="Action{T}"/>
    /// </summary>
    /// <typeparam name="T">The parameter type of the action delegate.</typeparam>
    /// <param name="action">The action delegate to clear.</param>
    /// <returns>The cleared action delegate.</returns>
    public static Action<T> ClearDelegates<T>(Action<T> action) where T : class
    {
        var list = action.GetInvocationList();
        for (var i = 0; i < list.Length; i++)
        {
#if DEBUG
            LogWarning($"Clearing {list[i].Method.Name}");
#endif
            action = (Action<T>)Delegate.Remove(action, list[i]);
        }

        return action;
    }

    /// <summary>
    /// Unsubscribes all delegates from an <see cref="Action{T, Y}"/>
    /// </summary>
    /// <typeparam name="T">The first parameter type of the action delegate.</typeparam>
    /// <typeparam name="Y">The second parameter type of the action delegate.</typeparam>
    /// <param name="action">The action delegate to clear.</param>
    /// <returns>The cleared action delegate.</returns>
    public static Action<T, Y> ClearDelegates<T, Y>(Action<T, Y> action)
        where T : class
        where Y : class
    {
        var list = action.GetInvocationList();
        for (var i = 0; i < list.Length; i++)
        {
#if DEBUG
            LogWarning($"Clearing {list[i].Method.Name}");
#endif
            action = (Action<T, Y>)Delegate.Remove(action, list[i]);
        }

        return action;
    }

    /// <summary>
    /// Logs an informational message with the calling member name prefixed.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="caller">The name of the calling member, populated automatically via <see cref="CallerMemberNameAttribute"/>.</param>
    public static void LogInfo(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogInfo($"[{caller}]: {message}");
    }

    /// <summary>
    /// Logs a warning message with the calling member name prefixed.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="caller">The name of the calling member, populated automatically via <see cref="CallerMemberNameAttribute"/>.</param>
    public static void LogWarning(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogWarning($"[{caller}]: {message}");
    }

    /// <summary>
    /// Logs an error message with the calling member name prefixed.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="caller">The name of the calling member, populated automatically via <see cref="CallerMemberNameAttribute"/>.</param>
    public static void LogError(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogError($"[{caller}]: {message}");
    }

    /// <summary>
    /// Logs an object representation as an error with the calling member name prefixed.
    /// </summary>
    /// <param name="obj">The object to log as a string.</param>
    /// <param name="caller">The name of the calling member, populated automatically via <see cref="CallerMemberNameAttribute"/>.</param>
    public static void LogError(object obj, [CallerMemberName] string caller = "")
    {
        Instance.FikaLogger.LogError($"[{caller}]: {obj}");
    }

    /// <summary>
    /// Logs a fatal error message with the calling member name prefixed.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="caller">The name of the calling member, populated automatically via <see cref="CallerMemberNameAttribute"/>.</param>
    public static void LogFatal(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogFatal($"[{caller}]: {message}");
    }

    /// <summary>
    /// Converts an <see cref="ESendRate"/> enum value into its corresponding numeric tick rate.
    /// </summary>
    /// <param name="rate">The network send rate enum value.</param>
    /// <returns>The tick rate as an integer frequency value (e.g. 10, 20, 30).</returns>
    public static int ToNumber(this ESendRate rate)
    {
        return rate switch
        {
            ESendRate.Low => 10,
            ESendRate.Medium => 20,
            ESendRate.High => 30,
            _ => 20,
        };
    }

    /// <summary>
    /// An empty action method used as a no-op delegate to avoid allocations.
    /// </summary>
    public static void EmptyAction()
    {

    }

    /// <summary>
    /// An empty callback method used as a no-op delegate for asynchronous results.
    /// </summary>
    /// <param name="result">The result of the asynchronous operation (unused).</param>
    private static void EmptyCallback(IResult result)
    {

    }

    /// <summary>
    /// Converts the <see cref="ELoadPriority"/> to a delegate
    /// </summary>
    /// <param name="priority">The priority</param>
    /// <returns>A new <see cref="YieldDelegate"/> for <see cref="Components.CoopHandler.SpawnPlayer(Components.CoopHandler.SpawnObject)"/></returns>
    public static YieldDelegate ToLoadPriorty(this ELoadPriority priority)
    {
        return priority switch
        {
            ELoadPriority.Low => JobYieldPriority.Low,
            ELoadPriority.Medium => JobYieldPriority.General,
            ELoadPriority.High => JobYieldPriority.Immediate,
            _ => JobYieldPriority.Low,
        };
    }

    /// <summary>
    /// Migrates IL labels
    /// </summary>
    /// <param name="codes">List of instructions</param>
    /// <param name="index">Index to start at</param>
    /// <param name="count">Iterations</param>
    public static void MigrateLabels(List<CodeInstruction> codes, int index, int count)
    {
        var targetIndex = index + count;

        if (targetIndex < codes.Count)
        {
            var labelsToMove = new List<Label>();
            for (var i = index; i < targetIndex; i++)
            {
                labelsToMove.AddRange(codes[i].labels);
            }

            codes[targetIndex].labels.AddRange(labelsToMove);
        }
    }

    /// <summary>
    /// Checks whether all modifiers are pressed for a <see cref="KeyboardShortcut"/>
    /// </summary>
    /// <param name="shortcut">The shortcut to check</param>
    /// <returns><see langword="true"/> if all modifiers are pressed; <see langword="false"/> if not</returns>
    public static bool AreModifiersPressed(KeyboardShortcut shortcut)
    {
        foreach (var key in shortcut.Modifiers)
        {
            if (!Input.GetKey(key))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Dynamically creates and compiles a high-performance getter delegate for a private instance field using Expression Trees.
    /// </summary>
    /// <typeparam name="T">The declaring <see cref="Type"/> of the class containing the field.</typeparam>
    /// <typeparam name="TResult">The <see cref="Type"/> of the field value to retrieve.</typeparam>
    /// <param name="fieldName">The exact case-sensitive name of the private field.</param>
    /// <returns>A compiled <see cref="Func{T, TResult}"/> delegate that yields the field value when invoked.</returns>
    /// <exception cref="NullReferenceException">Thrown when the specified <paramref name="fieldName"/> cannot be found via reflection.</exception>
    public static Func<T, TResult> CreateGetter<T, TResult>(string fieldName)
    {
        var fieldInfo = typeof(T).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (fieldInfo != null)
        {
            var targetParam = Expression.Parameter(typeof(T), "instance");
            var fieldAccess = Expression.Field(targetParam, fieldInfo);

            return Expression.Lambda<Func<T, TResult>>(fieldAccess, targetParam)
                .Compile();
        }
        else
        {
            throw new NullReferenceException($"Failed to find private field [{fieldName}] in {typeof(T).Name}.");
        }
    }

    /// <summary>
    /// Dynamically creates and compiles a high-performance setter delegate for a private instance field using Expression Trees.
    /// </summary>
    /// <typeparam name="T">The declaring <see cref="Type"/> of the class containing the field.</typeparam>
    /// <typeparam name="TResult">The <see cref="Type"/> of the field value to assign.</typeparam>
    /// <param name="fieldName">The exact case-sensitive name of the private field.</param>
    /// <returns>A compiled <see cref="Action{T, TResult}"/> delegate that assigns a new value to the field when invoked.</returns>
    /// <exception cref="NullReferenceException">Thrown when the specified <paramref name="fieldName"/> cannot be found via reflection.</exception>
    public static Action<T, TResult> CreateSetter<T, TResult>(string fieldName)
    {
        var fieldInfo = typeof(T).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (fieldInfo != null)
        {
            var targetParam = Expression.Parameter(typeof(T), "instance");
            var fieldAccess = Expression.Field(targetParam, fieldInfo);
            var valueParam = Expression.Parameter(typeof(TResult), "value");
            var assignExpr = Expression.Assign(fieldAccess, valueParam);

            return Expression.Lambda<Action<T, TResult>>(assignExpr, targetParam, valueParam)
                .Compile();
        }
        else
        {
            throw new NullReferenceException($"Failed to find private field [{fieldName}] in {typeof(T).Name}.");
        }
    }

    /// <summary>
    /// Checks whether the shot type is a misfire
    /// </summary>
    /// <param name="shotType">The shot type to check.</param>
    /// <returns><see langword="true"/> if the shot is a misfire; otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMisfire(this EShotType shotType)
    {
        return shotType >= EShotType.Misfire;
    }
}
