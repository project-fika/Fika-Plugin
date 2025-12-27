using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public class ClientTransitController : GClass1906
{
    public ClientTransitController(BackendConfigSettingsClass.TransitSettingsClass settings, LocationSettingsClass.Location.TransitParameters[] parameters, Profile profile, LocalRaidSettings localRaidSettings)
        : base(settings, parameters)
    {
        OnPlayerEnter += OnClientPlayerEnter;
        OnPlayerExit += OnClientPlayerExit;
        var array = localRaidSettings.transition.visitedLocations.EmptyIfNull().Append(localRaidSettings.location).ToArray();
        summonedTransits[profile.Id] = new TransitDataClass(localRaidSettings.transition.transitionRaidId, localRaidSettings.transition.transitionCount, array,
            localRaidSettings.transitionType.HasFlagNoBox(ELocationTransition.Event));
        TransferItemsController.InitItemControllerServer(FikaGlobals.TransitTraderId, FikaGlobals.TransitTraderName);
        _localRaidSettings = localRaidSettings;
    }

    public TransitInteractionPacketStruct InteractPacket { get; set; }

    private readonly LocalRaidSettings _localRaidSettings;

    private void OnClientPlayerEnter(TransitPoint point, Player player)
    {
        if (!transitPlayers.ContainsKey(player.ProfileId))
        {
            //TransferItemsController.InitPlayerStash(player);
            if (player is FikaPlayer fikaPlayer)
            {
                fikaPlayer.UpdateBtrTraderServiceData().HandleExceptions();
            }
        }
    }

    private void OnClientPlayerExit(TransitPoint point, Player player)
    {

    }

    public void Init()
    {
        EnablePoints(true);
        method_8(Dictionary_0.Values, GamePlayerOwner.MyPlayer, false);
    }

    public override void Dispose()
    {
        base.Dispose();
        OnPlayerEnter -= OnClientPlayerEnter;
        OnPlayerExit -= OnClientPlayerExit;
    }

    public void HandleClientExtract(int transitId, int playerId)
    {
        if (!smethod_2(playerId, out var myPlayer))
        {
            return;
        }

        if (!Dictionary_0.TryGetValue(transitId, out var transitPoint))
        {
            FikaPlugin.Instance.FikaLogger.LogError("FikaClientTransitController::HandleClientExtract: Could not find transit point with id: " + transitId);
            return;
        }

        var location = transitPoint.parameters.location;
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

        AlreadyTransitDataClass gclass = new()
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
        foreach (var transitPoint in Dictionary_0.Values)
        {
            if (!method_7(transitPoint))
            {
                HashSet_0.Add(transitPoint);
            }
            else
            {
                list.Add(transitPoint);
            }
        }
        method_8(list, GamePlayerOwner.MyPlayer, false);
    }
}
