using Comfort.Common;
using EFT;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.Lighthouse
{

    /// <summary>
	/// Based on <see href="https://dev.sp-tarkov.com/SPT/Modules/src/branch/3.9.x-DEV/project/SPT.SinglePlayer/Models/Progression/LighthouseProgressionClass.cs"/>
	/// </summary>
    public class FikaLighthouseProgressionClass : MonoBehaviour
    {
        private CoopHandler coopHandler;
        private GameWorld _gameWorld;
        private float _timer;
        private List<MineDirectional> _bridgeMines;
        private RecodableItemClass _transmitter;
        private List<string> ZryachiyAndFollowersIds = new List<string>();
        private bool _aggressor;
        private bool _isDoorDisabled;
        private readonly string _transmitterId = "62e910aaf957f2915e0a5e36";
        private readonly string _lightKeeperTid = "638f541a29ffd1183d187f57";

        public void Start()
        {
            coopHandler = CoopHandler.GetCoopHandler();
            _gameWorld = Singleton<GameWorld>.Instance;

            if (_gameWorld == null || coopHandler.MyPlayer == null)
            {
                Destroy(this);

                return;
            }

            // Get transmitter from players inventory
            _transmitter = GetTransmitterFromInventory();

            // Exit if transmitter does not exist and isnt green
            if (!PlayerHasActiveTransmitterInInventory())
            {
                Destroy(this);

                return;
            }

            List<AIPlaceInfo> places = Singleton<IBotGame>.Instance.BotsController.CoversData.AIPlaceInfoHolder.Places;

            places.First(x => x.name == "Attack").gameObject.SetActive(false);

            // Zone was added in a newer version and the gameObject actually has a \
            places.First(y => y.name == "CloseZone\\").gameObject.SetActive(false);

            // Give access to Lightkeepers door
            _gameWorld.BufferZoneController.SetPlayerAccessStatus(coopHandler.MyPlayer.ProfileId, true);

            _bridgeMines = _gameWorld.MineManager.Mines;

            // Set mines to be non-active
            SetBridgeMinesStatus(false);
        }

        public void Update()
        {
            IncrementLastUpdateTimer();

            // Exit early if last update() run time was < 10 secs ago
            if (_timer < 10f)
            {
                return;
            }

            // Skip if:
            // GameWorld missing
            // Player not an enemy to Zryachiy
            // Lk door not accessible
            // Player has no transmitter on thier person
            if (_gameWorld == null || _isDoorDisabled || _transmitter == null)
            {
                return;
            }

            if (FikaBackendUtils.IsServer)
            {
                // Find Zryachiy and prep him
                if (ZryachiyAndFollowersIds.Count == 0)
                {
                    SetupZryachiyAndFollowerHostility();
                }
            }

            // If player becomes aggressor, block access to LK
            if (_aggressor)
            {
                DisableAccessToLightKeeper();
            }
        }

        /// <summary>
        /// Gets transmitter from players inventory
        /// </summary>
        private RecodableItemClass GetTransmitterFromInventory()
        {
            return (RecodableItemClass)coopHandler.MyPlayer.Profile.Inventory.AllRealPlayerItems.FirstOrDefault(x => x.TemplateId == _transmitterId);
        }

        /// <summary>
        /// Checks for transmitter status and exists in players inventory
        /// </summary>
        private bool PlayerHasActiveTransmitterInInventory()
        {
            return _transmitter != null &&
                   _transmitter?.RecodableComponent?.Status == RadioTransmitterStatus.Green;
        }

        /// <summary>
        /// Update _time to diff from last run of update()
        /// </summary>
        private void IncrementLastUpdateTimer()
        {
            _timer += Time.deltaTime;
        }

        /// <summary>
        /// Set all brdige mines to desire state
        /// </summary>
        /// <param name="desiredMineState">What state should bridge mines be set to</param>
        private void SetBridgeMinesStatus(bool desiredMineState)
        {
            // Find mines with opposite state of what we want
            IEnumerable<MineDirectional> mines = _bridgeMines.Where(mine => mine.gameObject.activeSelf == !desiredMineState && mine.transform.parent.gameObject.name == "Directional_mines_LHZONE");
            foreach (MineDirectional mine in mines)
            {
                mine.gameObject.SetActive(desiredMineState);
            }
        }

        /// <summary>
        /// Put Zryachiy and followers into a list and sub to their death event
        /// Make player agressor if player kills them.
        /// </summary>
        private void SetupZryachiyAndFollowerHostility()
        {
            // Only process non-players (ai)
            foreach (CoopPlayer player in coopHandler.Players.Values)
            {   
                if (ZryachiyAndFollowersIds.Contains(player.ProfileId))
                {
                    continue;
                }

                if (player.IsYourPlayer || player.AIData.BotOwner == null)
                {
                    continue;
                }

                // Edge case of bossZryachiy not being hostile to player
                if (player.AIData.BotOwner.IsRole(WildSpawnType.bossZryachiy) || player.AIData.BotOwner.IsRole(WildSpawnType.followerZryachiy))
                {
                    AddZryachiyOrFollower(player);
                }
            }
        }

        private void AddZryachiyOrFollower(CoopPlayer bot)
        {
            bot.OnPlayerDead += OnZryachiyOrFollowerDeath;
            ZryachiyAndFollowersIds.Add(bot.ProfileId);
        }

        private void OnZryachiyOrFollowerDeath(Player player, IPlayer lastAggressor, DamageInfo damageInfo, EBodyPart part)
        {
            player.OnPlayerDead -= OnZryachiyOrFollowerDeath;

            if (player.KillerId == coopHandler.MyPlayer.ProfileId)
            {
                // If player kills zryachiy or follower, force aggressor state
                // Also set players Lk standing to negative (allows access to quest chain (Making Amends))
                _aggressor = true;
                coopHandler.MyPlayer.Profile.TradersInfo[_lightKeeperTid].SetStanding(-0.01);
            }
        }

        /// <summary>
        /// Disable door + set transmitter to 'red'
        /// </summary>
        private void DisableAccessToLightKeeper()
        {
            // Disable access to Lightkeepers door for the player
            _gameWorld.BufferZoneController.SetPlayerAccessStatus(coopHandler.MyPlayer.ProfileId, false);
            _transmitter?.RecodableComponent?.SetStatus(RadioTransmitterStatus.Yellow);
            _transmitter?.RecodableComponent?.SetEncoded(false);
            _isDoorDisabled = true;
        }
    }
}
