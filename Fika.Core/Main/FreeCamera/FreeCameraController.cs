using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.UI;
using Fika.Core.Utils;
using HarmonyLib;
using Koenigz.PerfectCulling;
using Koenigz.PerfectCulling.EFT;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Fika.Core.Main.FreeCamera
{
    /// <summary>
    /// This is HEAVILY based on Terkoiz's work found here. Thanks for your work Terkoiz! <br/>
    /// <see href="https://dev.sp-tarkov.com/Terkoiz/Freecam/raw/branch/master/project/Terkoiz.Freecam/FreecamController.cs"/>
    /// </summary>

    public class FreeCameraController : MonoBehaviour
    {
        private FikaPlayer Player
        {
            get
            {
                return (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            }
        }

        public bool IsScriptActive
        {
            get
            {
                if (_freeCamScript != null)
                {
                    return _freeCamScript.IsActive;
                }
                return false;
            }
        }

        public Camera CameraMain { get; private set; }

        private EftBattleUIScreen BattleUI
        {
            get
            {
                if (_playerUI == null)
                {
                    GameObject gameObject = GameObject.Find("BattleUIScreen");
                    if (gameObject == null)
                    {
                        return null;
                    }

                    _playerUI = gameObject.GetComponent<EftBattleUIScreen>();
                    if (_playerUI == null)
                    {
                        return null;
                    }
                }

                return _playerUI;
            }
            set
            {
                _playerUI = value;
            }
        }

        private EftBattleUIScreen _playerUI;
        private GameObject _cameraParent;
        private bool _isSpectator;
        private FreeCamera _freeCamScript;
        private bool _uiHidden;
        private bool _effectsCleared;
        private GamePlayerOwner _gamePlayerOwner;
        private Vector3 _lastKnownPosition;
        private CoopHandler _coopHandler;
        private TextMeshProUGUI _extractText;
        private bool _extracted;
        private DeathFade _deathFade;
        private bool _deathFadeEnabled;
        private DisablerCullingObjectBase[] _allCullingObjects;
        private List<PerfectCullingBakeGroup> _previouslyActiveBakeGroups;
        private bool _hasEnabledCulling;

        protected void Awake()
        {
            _cameraParent = new GameObject("CameraParent");
            Camera FCamera = _cameraParent.GetOrAddComponent<Camera>();
            FCamera.enabled = false;
            _isSpectator = FikaBackendUtils.IsSpectator;
        }

        protected void Start()
        {
            // Find Main Camera
            CameraMain = CameraClass.Instance.Camera;
            if (CameraMain == null)
            {
                return;
            }

            // Add Freecam script to main camera in scene
            _freeCamScript = CameraMain.gameObject.AddComponent<FreeCamera>();
            if (_freeCamScript == null)
            {
                return;
            }

            // Get GamePlayerOwner component
            _gamePlayerOwner = GetLocalPlayerFromWorld().GetComponentInChildren<GamePlayerOwner>();
            if (_gamePlayerOwner == null)
            {
                return;
            }

            _deathFade = CameraClass.Instance.Camera.GetComponent<DeathFade>();
            _deathFade.enabled = true;

            _allCullingObjects = FindObjectsOfType<DisablerCullingObjectBase>();
            _previouslyActiveBakeGroups = [];

            Player.ActiveHealthController.DiedEvent += MainPlayer_DiedEvent;

            if (CoopHandler.TryGetCoopHandler(out CoopHandler cHandler))
            {
                _coopHandler = cHandler;
            }
        }

        private void MainPlayer_DiedEvent(EDamageType obj)
        {
            Player.ActiveHealthController.DiedEvent -= MainPlayer_DiedEvent;

            if (!_deathFadeEnabled)
            {
                _deathFade.EnableEffect();
                _deathFadeEnabled = true;
            }

            StartCoroutine(DeathRoutine());
        }

        protected void Update()
        {
            if (_gamePlayerOwner == null)
            {
                return;
            }

            if (Player == null)
            {
                return;
            }

            if (Player.ActiveHealthController == null)
            {
                return;
            }

            CoopHandler.EQuitState quitState = _coopHandler.QuitState;
            if (quitState != CoopHandler.EQuitState.Extracted)
            {
                _lastKnownPosition = Player.PlayerBones.Neck.position;
            }

            if (_extracted && !_freeCamScript.IsActive)
            {
                ToggleUi();
                if (FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator)
                {
                    ToggleCamera();
                }
                else
                {
                    ToggleSpectateCamera();
                }
            }

            if (FikaPlugin.FreeCamButton.Value.IsDown())
            {
                if (!FikaPlugin.Instance.AllowFreeCam)
                {
                    return;
                }

                if (quitState == CoopHandler.EQuitState.None)
                {
                    ToggleCamera();
                    ToggleUi();
                    return;
                }
            }

            if (quitState == CoopHandler.EQuitState.Extracted && !_extracted)
            {
                FikaPlugin.Instance.FikaLogger.LogDebug($"Freecam: player has extracted");
                IFikaGame fikaGame = _coopHandler.LocalGameInstance;
                if (fikaGame.ExtractedPlayers.Contains(Player.NetId))
                {
                    _extracted = true;
                    ShowExtractMessage();
                }

                if (!_freeCamScript.IsActive)
                {
                    ToggleUi();
                    if (FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator)
                    {
                        _freeCamScript.transform.position = _lastKnownPosition;
                        ToggleCamera();
                    }
                    else
                    {
                        ToggleSpectateCamera();
                    }
                }

                if (!_effectsCleared)
                {
                    if (Player != null)
                    {
                        Player.Muffled = false;
                        Player.HeavyBreath = false;
                    }

                    if (CameraClass.Exist)
                    {
                        ClearEffects();
                    }
                    _effectsCleared = true;
                }
            }
        }

        private IEnumerator DeathRoutine()
        {
            if (!_isSpectator)
            {
                yield return new WaitForSeconds(5);
            }

            CameraClass cameraClassInstance = CameraClass.Instance;
            if (cameraClassInstance == null)
            {
                yield break;
            }

            if (cameraClassInstance.EffectsController == null)
            {
                yield break;
            }

            if (cameraClassInstance.Camera != null)
            {
                cameraClassInstance.Camera.fieldOfView = Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView;
            }

            // Disable the DeathFade effect & Toggle the Camera
            _deathFade.DisableEffect();
            if (!_freeCamScript.IsActive)
            {
                ToggleUi();
                if (FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator)
                {
                    ToggleCamera();

                    if (_isSpectator)
                    {
                        // Cycle camera to any alive player
                        _freeCamScript.CycleSpectatePlayers();
                    }
                }
                else
                {
                    ToggleSpectateCamera();
                }
            }
            ShowExtractMessage();

            if (!_effectsCleared)
            {
                if (Player != null)
                {
                    Player.Muffled = false;
                    Player.HeavyBreath = false;
                }

                if (CameraClass.Exist)
                {
                    ClearEffects();
                }
                _effectsCleared = true;
            }
        }

        private void ClearEffects()
        {
            CameraClass cameraClass = CameraClass.Instance;

            cameraClass.EffectsController.method_4(null, false);

            Traverse effectsController = Traverse.Create(cameraClass.EffectsController);

            BloodOnScreen bloodOnScreen = effectsController.Field<BloodOnScreen>("bloodOnScreen_0").Value;
            if (bloodOnScreen != null)
            {
                Destroy(bloodOnScreen);
            }

            List<EffectsController.Class632> effectsManagerList = effectsController.Field<List<EffectsController.Class632>>("list_0").Value;
            if (effectsManagerList != null)
            {
                for (int i = 0; i < effectsManagerList.Count; i++)
                {
                    EffectsController.Class632 effectsManager = effectsManagerList[i];
                    while (effectsManager.ActiveEffects.Count > 0)
                    {
                        IEffect effect = effectsManager.ActiveEffects[0];
                        effectsManager.DeleteEffect(effect);
                    }
                }
                effectsManagerList.Clear();
            }

            CC_Wiggle wiggleEffect = cameraClass.Camera.gameObject.GetComponent<CC_Wiggle>();
            if (wiggleEffect != null)
            {
                wiggleEffect.enabled = false;
            }

            CC_Blend[] blendEffects = cameraClass.Camera.gameObject.GetComponents<CC_Blend>();
            if (blendEffects.Length > 0)
            {
                foreach (CC_Blend blendEffect in blendEffects)
                {
                    blendEffect.enabled = false;
                }
            }

            Destroy(cameraClass.EffectsController);
            cameraClass.VisorEffect.Clear();
            Destroy(cameraClass.VisorEffect);
            cameraClass.VisorSwitcher.Deinit();
            Destroy(cameraClass.VisorSwitcher);
            if (cameraClass.NightVision.On)
            {
                cameraClass.NightVision.method_1(false);
            }
            if (cameraClass.ThermalVision.On)
            {
                cameraClass.ThermalVision.method_1(false);
            }
        }

        private void ShowExtractMessage()
        {
            if (FikaPlugin.ShowExtractMessage.Value)
            {
                string text = FikaPlugin.ExtractKey.Value.MainKey.ToString();
                if (FikaPlugin.ExtractKey.Value.Modifiers.Count() > 0)
                {
                    string modifiers = string.Join(" + ", FikaPlugin.ExtractKey.Value.Modifiers);
                    text = modifiers + " + " + text;
                }
                _extractText = FikaUIGlobals.CreateOverlayText(string.Format(LocaleUtils.UI_EXTRACT_MESSAGE.Localized(), $"'{text}'"));
            }
        }

        /// <summary>
        /// Toggles the Freecam mode
        /// </summary>
        public void ToggleCamera()
        {
            // Get our own Player instance. Null means we're not in a raid
            if (Player == null)
            {
                return;
            }

            if (!_freeCamScript.IsActive)
            {
                SetPlayerToFreecamMode(Player);
            }
            else
            {
                SetPlayerToFirstPersonMode(Player);
            }
        }

        public void ToggleSpectateCamera()
        {
            if (Player == null)
            {
                return;
            }
            if (!_freeCamScript.IsActive)
            {
                if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    List<FikaPlayer> alivePlayers = [];

                    List<FikaPlayer> humanPlayers = coopHandler.HumanPlayers;
                    for (int i = 0; i < humanPlayers.Count; i++)
                    {
                        FikaPlayer player = humanPlayers[i];
                        if (!player.IsYourPlayer && player.HealthController.IsAlive)
                        {
                            alivePlayers.Add(player);
                        }
                    }
                    if (alivePlayers.Count <= 0)
                    {
                        // No alive players to attach to at this time, so let's fallback to freecam on last known position
                        _freeCamScript.transform.position = _lastKnownPosition;
                        ToggleCamera();
                        return;
                    }
                    FikaPlayer fikaPlayer = alivePlayers[0];
                    _freeCamScript.SetCurrentPlayer(fikaPlayer);
                    FikaPlugin.Instance.FikaLogger.LogDebug("FreecamController: Spectating new player: " + fikaPlayer.Profile.Info.MainProfileNickname);

                    Player.PointOfView = EPointOfView.ThirdPerson;
                    if (Player.PlayerBody != null)
                    {
                        Player.PlayerBody.PointOfView.Value = EPointOfView.FreeCamera;
                        Player.GetComponent<PlayerCameraController>().UpdatePointOfView();
                    }
                    _gamePlayerOwner.enabled = false;
                    _freeCamScript.SetActive(true, _extracted);

                    _freeCamScript.Attach3rdPerson();
                    return;
                }
            }
        }

        /// <summary>
        /// Hides the main UI (health, stamina, stance, hotbar, etc.)
        /// </summary>
        private void ToggleUi()
        {
            // Check if we're currently in a raid
            if (Player == null)
            {
                return;
            }

            if (BattleUI == null || BattleUI.gameObject == null)
            {
                return;
            }

            BattleUI.gameObject.SetActive(_uiHidden);
            _uiHidden = !_uiHidden;
        }

        /// <summary>
        /// A helper method to set the Player into Freecam mode
        /// </summary>
        /// <param name="localPlayer"></param>
        private void SetPlayerToFreecamMode(Player localPlayer)
        {
            // We set the player to third person mode
            // This means our character will be fully visible, while letting the camera move freely
            localPlayer.PointOfView = EPointOfView.ThirdPerson;

            if (localPlayer.PlayerBody != null)
            {
                localPlayer.PlayerBody.PointOfView.Value = EPointOfView.FreeCamera;
                localPlayer.GetComponent<PlayerCameraController>().UpdatePointOfView();
            }

            _gamePlayerOwner.enabled = false;
            _freeCamScript.SetActive(true, _extracted);
        }

        /// <summary>
        /// A helper method to reset the player view back to First Person
        /// </summary>
        /// <param name="localPlayer"></param>
        private void SetPlayerToFirstPersonMode(Player localPlayer)
        {
            // re-enable _gamePlayerOwner
            _gamePlayerOwner.enabled = true;
            _freeCamScript.SetActive(false, _extracted);

            localPlayer.PointOfView = EPointOfView.FirstPerson;
            CameraClass.Instance.SetOcclusionCullingEnabled(true);

            if (_hasEnabledCulling)
            {
                EnableAllCullingObjects();
            }
        }

        public void DisableAllCullingObjects()
        {
            int count = 0;
            foreach (DisablerCullingObjectBase cullingObject in _allCullingObjects)
            {
                if (cullingObject.HasEntered)
                {
                    continue;
                }
                count++;
                cullingObject.SetComponentsEnabled(true);
            }
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogWarning($"Enabled {count} Culling Triggers.");
#endif

            PerfectCullingAdaptiveGrid perfectCullingAdaptiveGrid = FindObjectOfType<PerfectCullingAdaptiveGrid>();
            if (perfectCullingAdaptiveGrid != null)
            {
                if (perfectCullingAdaptiveGrid.RuntimeGroupMapping.Count > 0)
                {
                    foreach (PerfectCullingCrossSceneGroup sceneGroup in perfectCullingAdaptiveGrid.RuntimeGroupMapping)
                    {
                        foreach (PerfectCullingBakeGroup bakeGroup in sceneGroup.bakeGroups)
                        {
                            if (!bakeGroup.IsEnabled)
                            {
                                bakeGroup.IsEnabled = true;
                                continue;
                            }

                            _previouslyActiveBakeGroups.Add(bakeGroup);
                        }

                        sceneGroup.enabled = false;
                    }
                }
            }

            _hasEnabledCulling = true;
        }

        public void EnableAllCullingObjects()
        {
            int count = 0;
            foreach (DisablerCullingObjectBase cullingObject in _allCullingObjects)
            {
                if (cullingObject.HasEntered)
                {
                    continue;
                }
                count++;
                cullingObject.SetComponentsEnabled(false);
            }
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogWarning($"Disabled {count} Culling Triggers.");
#endif

            PerfectCullingAdaptiveGrid perfectCullingAdaptiveGrid = FindObjectOfType<PerfectCullingAdaptiveGrid>();
            if (perfectCullingAdaptiveGrid != null)
            {
                if (perfectCullingAdaptiveGrid.RuntimeGroupMapping.Count > 0)
                {
                    foreach (PerfectCullingCrossSceneGroup sceneGroup in perfectCullingAdaptiveGrid.RuntimeGroupMapping)
                    {
                        sceneGroup.enabled = true;

                        foreach (PerfectCullingBakeGroup bakeGroup in sceneGroup.bakeGroups)
                        {
                            if (bakeGroup.IsEnabled && !_previouslyActiveBakeGroups.Contains(bakeGroup))
                            {
                                bakeGroup.IsEnabled = false;
                                continue;
                            }

                            _previouslyActiveBakeGroups.Remove(bakeGroup);
                        }

                        _previouslyActiveBakeGroups.Clear();
                    }
                }
            }

            _hasEnabledCulling = false;
        }

        /// <summary>
        /// Gets the current <see cref="EFT.Player"/> instance if it's available
        /// </summary>
        /// <returns>Local <see cref="EFT.Player"/> instance; returns null if the game is not in raid</returns>
        private Player GetLocalPlayerFromWorld()
        {
            // If the GameWorld instance is null or has no RegisteredPlayers, it most likely means we're not in a raid
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.MainPlayer == null)
            {
                return null;
            }

            // One of the RegisteredPlayers will have the IsYourPlayer flag set, which will be our own Player instance
            return gameWorld.MainPlayer;
        }

        public void OnDestroy()
        {
            if (!Singleton<FreeCameraController>.TryRelease(this))
            {
                FikaPlugin.Instance.FikaLogger.LogWarning("Unable to release FreeCameraController singleton");
            }
            Destroy(_cameraParent);

            // Destroy FreeCamScript before FreeCamController if exists
            Destroy(_freeCamScript);
            if (_extractText != null)
            {
                Destroy(_extractText);
            }
            Destroy(this);
        }
    }
}
