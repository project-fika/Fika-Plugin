/*using Aki.Reflection.Utils;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using EFT.Weather;
using JsonType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.GameMode
{
    public abstract class AFikaGame : BaseLocalGame<EftGamePlayerOwner>, IBotGame
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

        public static T Create<T>(GInterface170 inputTree, Profile profile, GameDateTime backendDateTime, InsuranceCompanyClass insurance, MenuUI menuUI, GameUI gameUI, LocationSettingsClass.Location location, TimeAndWeatherSettings timeAndWeather, WavesSettings wavesSettings, EDateTime dateTime, Callback<ExitStatus, TimeSpan, MetricsClass> callback, float fixedDeltaTime, EUpdateQueue updateQueue, ISession backEndSession, TimeSpan? sessionTime) where T : AFikaGame
        {
            return null;
        }

        public Dictionary<string, Player> Bots { get; set; } = new Dictionary<string, Player>();

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
*/