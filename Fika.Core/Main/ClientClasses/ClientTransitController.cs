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

namespace Fika.Core.Main.ClientClasses;

public class ClientTransitController : NetworkTransitController
{
    public ClientTransitController(GlobalConfiguration.TransitGlobalSettings settings, LocationSettings.Location.TransitParameters[] parameters, Profile profile, LocalRaidSettings localRaidSettings)
        : base(settings, parameters)
    {
        OnPlayerEnter += OnClientPlayerEnter;
        OnPlayerExit += OnClientPlayerExit;
        var array = localRaidSettings.transition.visitedLocations.EmptyIfNull()
            .Append(localRaidSettings.location)
            .ToArray();
        summonedTransits[profile.Id] = new Transit(localRaidSettings.transition.transitionRaidId, localRaidSettings.transition.transitionCount, array,
            localRaidSettings.transitionType.HasFlagNoBox(ELocationTransition.Event));
        TransferItemsController.InitItemControllerServer(FikaGlobals.TransitTraderId, FikaGlobals.TransitTraderName);
        _localRaidSettings = localRaidSettings;

        action_0();

        action_0 = GlobalEventsController.Instance.SubscribeOnEvent(new Action<TransitInitEvent>(OnInitEvent));

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
        if (!IsTargetPlayer(initEvent.PlayerId, out var player))
        {
#if DEBUG
            FikaGlobals.LogWarning($"[{initEvent.PlayerId}] was not my player");
#endif
            return;
        }

        /*var transit = summonedTransits[player.ProfileId];
        summonedTransits[player.ProfileId].events = initEvent.EventPlayer;*/
        var list = GetTransitPoints(initEvent.Points, player.Side);
        SetTimers(list, player, false);
        HandleExits(list, player);
    }

    public InteractWithTransitPacket InteractPacket { get; set; }

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
        OnPlayerEnter -= OnClientPlayerEnter;
        OnPlayerExit -= OnClientPlayerExit;
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
            tarkovApplication.transitionStatus = new(location, false, _localRaidSettings.playerSide, eraidMode, _localRaidSettings.timeVariant);
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
            profiles = profileKeys,
            transitionRaidId = summonedTransits[profileId].raidId,
            raidMode = eraidMode,
            side = myPlayer.Side is EPlayerSide.Savage ? ESideType.Savage : ESideType.Pmc,
            dayTime = _localRaidSettings.timeVariant
        };

        alreadyTransits.Add(profileId, gclass);
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame == null || fikaGame is not CoopGame coopGame)
        {
            FikaGlobals.LogError("FikaGame was null or not CoopGame");
            return;
        }

        if (coopGame != null)
        {
            coopGame.Extract((FikaPlayer)myPlayer, null, transitPoint);
        }
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
        SetTimers(list, GamePlayerOwner.MyPlayer, false);
    }
}
