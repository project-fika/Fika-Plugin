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

public static class FikaGlobals
{
    public const string TransitTraderId = "656f0f98d80a697f855d34b1";
    public const string TransitTraderName = "BTR";
    public const string DefaultTransitId = "66f5750951530ca5ae09876d";
    public const string FikaGroupId = "Fika";

    public static int PingMask = LayerMask.GetMask(["HighPolyCollider", "Interactive", "Deadbody", "Player", "Loot", "Terrain"]);

    /// <summary>
    /// Use when no callback is needed to reduce allocations
    /// </summary>
    public static Callback EmptyCallbackDelegate => EmptyCallback;
    /// <summary>
    /// Use when no callback is needed to reduce allocations
    /// </summary>
    public static Action EmptyActionDelegate => EmptyAction;

    public const int PingRange = 1000;

    private static readonly ManualLogSource _logger = Logger.CreateLogSource("FikaGlobals");

    internal static readonly List<EInteraction> BlockedInteractions =
    [
        EInteraction.DropBackpack, EInteraction.NightVisionOffGear, EInteraction.NightVisionOnGear,
        EInteraction.FaceshieldOffGear, EInteraction.FaceshieldOnGear, EInteraction.BipodForwardOn,
        EInteraction.BipodForwardOff, EInteraction.BipodBackwardOn, EInteraction.BipodBackwardOff
    ];

    internal static readonly List<EquipmentSlot> WeaponSlots =
    [
        EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster
    ];

    public static ISearchController SearchControllerSerializer
    {
        get
        {
            return GClass2240.Instance;
        }
    }

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

    public static VoipSettingsClass VOIPHandler
    {
        get
        {
            if (_voipHandler == null)
            {
                _voipHandler = VoipSettingsClass.Default;
                _voipHandler.VoipQualitySettings.Apply();
                _voipHandler.MicrophoneChecked = SoundSettingsControllerClass.CheckMicrophone();
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
    /// <returns></returns>
    public static bool IsInRaid
    {
        get
        {
            return Singleton<IFikaGame>.Instantiated;
        }
    }

    private static VoipSettingsClass _voipHandler;

    internal static float GetOtherPlayerSensitivity()
    {
        return 1f;
    }

    internal static float GetLocalPlayerSensitivity()
    {
        return Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseSensitivity;
    }

    internal static float GetLocalPlayerAimingSensitivity()
    {
        return Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseAimingSensitivity;
    }

    public static float GetApplicationTime()
    {
        return Time.time;
    }

    internal static bool LampControllerNetIdNot0(LampController controller)
    {
        return controller.NetId != 0;
    }

    internal static int LampControllerGetNetId(LampController controller)
    {
        return controller.NetId;
    }

    internal static bool WindowBreakerAvailableToSync(WindowBreaker breaker)
    {
        return breaker.AvailableToSync;
    }

    internal static Item GetLootItemPositionItem(LootItemPositionClass positionClass)
    {
        return positionClass.Item;
    }

    internal static EBodyPart GetBodyPartFromCollider(BodyPartCollider collider)
    {
        return collider.BodyPartType;
    }

    internal static string FormatFileSize(long bytes)
    {
        const int unit = 1024;
        if (bytes < unit) { return $"{bytes} B"; }

        var exp = (int)(Math.Log(bytes) / Math.Log(unit));
        return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
    }

    internal static void SpawnItemInWorld(Item item, FikaPlayer player)
    {
        StaticManager.BeginCoroutine(SpawnItemRoutine(item, player));
    }

    private static IEnumerator SpawnItemRoutine(Item item, FikaPlayer player)
    {
        List<ResourceKey> collection = [];
        foreach (var subItem in item.GetAllItems())
        {
            collection.AddRange(subItem.Template.AllResources);
        }
        var loadTask = Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Online,
            [.. collection], JobPriorityClass.Immediate, null, default);

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
    /// Forces the <see cref="InfoClass.MainProfileNickname"/> to be set on a profile
    /// </summary>
    /// <param name="infoClass"></param>
    /// <param name="nickname"></param>
    public static void SetProfileNickname(this InfoClass infoClass, string nickname)
    {
        Traverse.Create(infoClass).Field<string>("MainProfileNickname").Value = nickname;
    }

    /// <summary>
    /// Checks whether a profile belongs to a player or an AI
    /// </summary>
    /// <param name="profile"></param>
    /// <returns>True if the profile belongs to a player, false if it belongs to an AI</returns>
    public static bool IsPlayerProfile(this Profile profile)
    {
        return !string.IsNullOrEmpty(profile.PetId) || profile.Info.RegistrationDate > 0 || !string.IsNullOrEmpty(profile.Info.MainProfileNickname);
    }

    /// <summary>
    /// Gets the current <see cref="ISession"/>
    /// </summary>
    /// <returns><see cref="ISession"/> of the application</returns>
    public static ISession GetSession()
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
        CompleteProfileDescriptorClass liteDescriptor = new(profile, SearchControllerSerializer)
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
    /// <param name="controller"></param>
    /// <returns><see cref="FirearmLightStateStruct"/></returns>
    public static FirearmLightStateStruct GetFirearmLightStates(TacticalComboVisualController controller)
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
    /// <returns>A new <see cref="FirearmLightStateStruct"/> with data</returns>
    public static FirearmLightStateStruct GetFirearmLightStatesFromComponent(LightComponent component)
    {
        return new FirearmLightStateStruct
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
    /// Unsubscribes all delegates from an <see cref="Action{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
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
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Y"></typeparam>
    /// <param name="action"></param>
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

    public static void LogInfo(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogInfo($"[{caller}]: {message}");
    }

    public static void LogWarning(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogWarning($"[{caller}]: {message}");
    }

    public static void LogError(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogError($"[{caller}]: {message}");
    }

    public static void LogError(object obj, [CallerMemberName] string caller = "")
    {
        Instance.FikaLogger.LogError($"[{caller}]: {obj}");
    }

    public static void LogFatal(string message, [CallerMemberName] string caller = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Instance.FikaLogger.LogFatal($"[{caller}]: {message}");
    }

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

    public static void EmptyAction()
    {

    }

    private static void EmptyCallback(IResult result)
    {

    }

    /// <summary>
    /// Converts the <see cref="ELoadPriority"/> to a delegate
    /// </summary>
    /// <param name="priority">The priority</param>
    /// <returns>A new <see cref="GDelegate62"/> for <see cref="Components.CoopHandler.SpawnPlayer(Components.CoopHandler.SpawnObject)"/></returns>
    public static GDelegate62 ToLoadPriorty(this ELoadPriority priority)
    {
        return priority switch
        {
            ELoadPriority.Low => JobPriorityClass.Low,
            ELoadPriority.Medium => JobPriorityClass.General,
            ELoadPriority.High => JobPriorityClass.Immediate,
            _ => JobPriorityClass.Low,
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
}
