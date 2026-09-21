using JsonType;
using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.GlobalEvents;
using EFT.Interactive;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Fika.Core.Main.ClientClasses;

public class ClientTransitController : NetworkTransitController
{
    public ClientTransitController(IntPtr pointer) : base(pointer)
    {
    }

    public ClientTransitController(TransitGlobalSettings settings, Il2CppReferenceArray<LocationSettings.Location.TransitParameters> parameters, Profile profile, LocalRaidSettings localRaidSettings) : base(Il2CppInjection.Allocate<ClientTransitController>())
    {
        ClassInjector.DerivedConstructorBody(this);
        _onClientPlayerEnter = new Action<TransitPoint, Player>(OnClientPlayerEnter);
        _onClientPlayerExit = new Action<TransitPoint, Player>(OnClientPlayerExit);
        ClassInjector.InvokeBaseConstructor<NetworkTransitController>(this, settings, parameters);
        OnPlayerEnter += _onClientPlayerEnter;
        OnPlayerExit += _onClientPlayerExit;
        var array = (localRaidSettings.transition.visitedLocations ?? (IEnumerable<string>)[])
            .Append(localRaidSettings.location)
            .ToArray();
        summonedTransits[profile.Id] = new Transit(localRaidSettings.transition.transitionRaidId, localRaidSettings.transition.transitionCount, array,
            localRaidSettings.transitionType.HasFlagNoBox(ELocationTransition.Event));
        TransferItemsController.InitItemControllerServer(FikaGlobals.TransitTraderId, FikaGlobals.TransitTraderName);
        _localRaidSettings = localRaidSettings;

        _eventInitUnsubscribe.Invoke();
        _eventInitUnsubscribe = GlobalEventsController.Instance.SubscribeOnEvent<TransitInitEvent>(new Action<TransitInitEvent>(OnInitEvent));

        ReEnablePoints();
    }

    private void ReEnablePoints()
    {
        foreach (var transitPoint in pointsById.Values)
        {
            transitPoint.gameObject.SetActive(true);
        }
    }

    private void OnInitEvent(TransitInitEvent initEvent)
    {
        FikaGlobals.LogInfo($"Received TransitInitEvent from server with {initEvent.Points.Count} points");
        if (!IsTargetPlayer(initEvent.PlayerRaidId, out var player))
        {
#if DEBUG
            FikaGlobals.LogWarning($"[{initEvent.PlayerRaidId}] was not my player");
#endif
            return;
        }

        _completedQuestRequirementMetByPointId.Clear();
        if (initEvent.CompletedQuestRequirementMetByPointId != null)
        {
            foreach (var entry in initEvent.CompletedQuestRequirementMetByPointId)
            {
                _completedQuestRequirementMetByPointId[entry.Key] = entry.Value;
            }
        }

        /*var transit = summonedTransits[player.ProfileId];
        summonedTransits[player.ProfileId].events = initEvent.EventPlayer;*/
        var list = GetTransitPoints(initEvent.Points, player.Side);
        SetTimers(list, player, false);
        HandleExits(list, player);
    }

    public InteractWithTransitPacket InteractPacket { get; set; }

    private readonly Il2CppSystem.Action<TransitPoint, Player> _onClientPlayerEnter;
    private readonly Il2CppSystem.Action<TransitPoint, Player> _onClientPlayerExit;

    private readonly LocalRaidSettings _localRaidSettings;

    private void OnClientPlayerEnter(TransitPoint point, Player player)
    {
        if (!transitPlayers.ContainsKey(player.ProfileId))
        {
            //TransferItemsController.InitPlayerStash(player);
            if (player is FikaPlayer fikaPlayer)
            {
                fikaPlayer.UpdateBtrTraderServiceData()
                    .HandleExceptions();
            }
        }
    }

    private void OnClientPlayerExit(TransitPoint point, Player player)
    {

    }

    public void Init()
    {
        /*EnablePoints(true);
        method_8(Dictionary_0.Values, GamePlayerOwner.MyPlayer, false);*/
    }

    public override void Dispose()
    {
        base.Dispose();
        OnPlayerEnter -= _onClientPlayerEnter;
        OnPlayerExit -= _onClientPlayerExit;
    }

    public void HandleClientExtract(int transitId, int playerId)
    {
        if (!IsTargetPlayer(playerId, out var myPlayer))
        {
            return;
        }

        if (!pointsById.TryGetValue(transitId, out var transitPoint))
        {
            FikaGlobals.LogError("FikaClientTransitController::HandleClientExtract: Could not find transit point with id: " + transitId);
            return;
        }

        var location = transitPoint.parameters.location;
        FikaGlobals.LogInfo($"Using transit to {location}");
        var eraidMode = ERaidMode.Local;
        if (TarkovApplication.Exist(out var tarkovApplication))
        {
            eraidMode = ERaidMode.Local;
            tarkovApplication.TransitionStatus = new TransitionStatus(location, false, _localRaidSettings.playerSide, eraidMode, _localRaidSettings.timeVariant);
        }
        var profileId = myPlayer.ProfileId;
        Dictionary<string, ProfileKey> profileKeys = [];
        profileKeys.Add(profileId, new()
        {
            _id = profileId,
            keyId = InteractPacket.keyId,
            isSolo = true
        });

        LocationTransit gclass = new()
        {
            hash = Guid.NewGuid().ToString(),
            playersCount = 1,
            ip = "",
            location = location,
            profiles = (profileKeys).ToIl2CppDictionary(),
            transitionRaidId = summonedTransits[profileId].raidId,
            raidMode = eraidMode,
            side = myPlayer.Side is EPlayerSide.Savage ? ESideType.Savage : ESideType.Pmc,
            dayTime = _localRaidSettings.timeVariant
        };

        alreadyTransits.Add(profileId, gclass);
        var fikaGame = FikaGlobals.FikaGame;
        if (fikaGame == null || fikaGame is not CoopGame coopGame)
        {
            FikaGlobals.LogError("FikaGame was null or not CoopGame");
            return;
        }

        var fikaPlayer = myPlayer.TryCast<FikaPlayer>();
        if (fikaPlayer == null)
        {
            FikaGlobals.LogError("HandleClientExtract: transit player was not a FikaPlayer");
            return;
        }

        coopGame.Extract(fikaPlayer, null, transitPoint);
    }

    public void UpdateTimers()
    {
        var list = new List<TransitPoint>();
        foreach (var transitPoint in pointsById.Values)
        {
            if (!IsVisibleTransitPoint(transitPoint))
            {
                _waitForVisibleTransitPoints.Add(transitPoint);
            }
            else
            {
                list.Add(transitPoint);
            }
        }
        SetTimers((list).ToIl2CppList(), GamePlayerOwner.MyPlayer, false);
    }
}
