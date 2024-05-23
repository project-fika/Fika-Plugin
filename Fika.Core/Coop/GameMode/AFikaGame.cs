using Aki.Reflection.Utils;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using EFT.Weather;
using JsonType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.GameMode
{
    public abstract class AFikaGame : BaseLocalGame<GamePlayerOwner>, IBotGame
    {
        public new bool InRaid { get { return true; } }

        public ISession BackEndSession { get { return PatchConstants.BackEndSession; } }

        BotsController IBotGame.BotsController
        {
            get
            {
                if (BotsController == null)
                {
                    BotsController = (BotsController)GetType().GetFields().Where(x => x.FieldType == typeof(BotsController)).FirstOrDefault().GetValue(this);
                }
                return BotsController;
            }
        }

        private static BotsController BotsController;

        public BotsController PBotsController
        {
            get
            {
                if (BotsController == null)
                {
                    BotsController = (BotsController)GetType().GetFields().Where(x => x.FieldType == typeof(BotsController)).FirstOrDefault().GetValue(this);
                }
                return BotsController;
            }
        }

        public IWeatherCurve WeatherCurve
        {
            get
            {
                return WeatherController.Instance.WeatherCurve;
            }
        }

        ManualLogSource Logger { get; set; }

        public static T Create<T>(InputTree inputTree, Profile profile, GameDateTime backendDateTime, InsuranceCompanyClass insurance, MenuUI menuUI,
            CommonUI commonUI, PreloaderUI preloaderUI, GameUI gameUI, LocationSettingsClass.Location location, TimeAndWeatherSettings timeAndWeather,
            WavesSettings wavesSettings, EDateTime dateTime, Callback<ExitStatus, TimeSpan, MetricsClass> callback, float fixedDeltaTime, EUpdateQueue updateQueue,
            ISession backEndSession, TimeSpan sessionTime) where T : AFikaGame
        {

            var r = smethod_0<T>(inputTree, profile, backendDateTime, insurance, menuUI, commonUI, preloaderUI, gameUI, location, timeAndWeather, wavesSettings, dateTime,
                callback, fixedDeltaTime, updateQueue, backEndSession, new TimeSpan?(sessionTime));

            r.Logger = BepInEx.Logging.Logger.CreateLogSource("Coop Game Mode");
            r.Logger.LogInfo("CoopGame::Create");

            // Non Waves Scenario setup
            r.nonWavesSpawnScenario_0 = (NonWavesSpawnScenario)typeof(NonWavesSpawnScenario).GetMethod(nameof(NonWavesSpawnScenario.smethod_0), BindingFlags.Static | BindingFlags.Public).Invoke
                (null, [r, location, r.PBotsController]);
            r.nonWavesSpawnScenario_0.ImplementWaveSettings(wavesSettings);

            // Waves Scenario setup
            r.wavesSpawnScenario_0 = (WavesSpawnScenario)typeof(WavesSpawnScenario).GetMethod(nameof(WavesSpawnScenario.smethod_0), BindingFlags.Static | BindingFlags.Public).Invoke
                (null, [r.gameObject, location.waves, new Action<BotWaveDataClass>(r.PBotsController.ActivateBotsByWave), location]);

            var bosswavemanagerValue = typeof(GClass579).GetMethod(nameof(GClass579.smethod_0), BindingFlags.Static | BindingFlags.Public).Invoke
                (null, [location.BossLocationSpawn, new Action<BossLocationSpawn>(r.PBotsController.ActivateBotsByWave)]);

            r.GetType().GetFields(BindingFlags.NonPublic).Where(x => x.FieldType == typeof(GClass579)).FirstOrDefault().SetValue(r, bosswavemanagerValue);

            r.GClass579 = bosswavemanagerValue as GClass579;

            r.func_1 = (player) => GamePlayerOwner.Create<GamePlayerOwner>(player, inputTree, insurance, backEndSession, commonUI, preloaderUI, gameUI, r.GameDateTime, location);

            return r;
        }

        public Dictionary<string, Player> Bots { get; set; } = new Dictionary<string, Player>();

        /// <summary>
        /// Matchmaker countdown
        /// </summary>
        /// <param name="timeBeforeDeploy"></param>
        public override void vmethod_1(float timeBeforeDeploy)
        {
            base.vmethod_1(timeBeforeDeploy);
        }

        /// <summary>
        /// Reconnection handling.
        /// </summary>
        public override void vmethod_3()
        {
            base.vmethod_3();
        }

        private GClass579 GClass579;

        private WavesSpawnScenario wavesSpawnScenario_0;

        private NonWavesSpawnScenario nonWavesSpawnScenario_0;

        private Func<Player, GamePlayerOwner> func_1;

        public new void method_6(string backendUrl, string locationId, int variantId)
        {
            Logger.LogInfo("CoopGame:method_6");
            return;
        }
    }
}
