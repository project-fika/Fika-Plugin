using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.UI;
using Koenigz.PerfectCulling;
using Koenigz.PerfectCulling.EFT;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Fika.Core.Coop.FreeCamera
{
    /// <summary>
    /// This is HEAVILY based on Terkoiz's work found here. Thanks for your work Terkoiz! 
    /// https://dev.sp-tarkov.com/Terkoiz/Freecam/raw/branch/master/project/Terkoiz.Freecam/FreecamController.cs
    /// </summary>

    public class FreeCameraController : MonoBehaviour
    {
        //private GameObject _mainCamera;
        private FreeCamera _freeCamScript;

        private BattleUIScreen _playerUi;
        private bool _uiHidden;

        private bool _effectsCleared = false;

        private GamePlayerOwner _gamePlayerOwner;
        private Player _player => Singleton<GameWorld>.Instance.MainPlayer;

        public GameObject CameraParent;
        public Camera CameraFreeCamera { get; private set; }
        public Camera CameraMain { get; private set; }

        private TextMeshProUGUI extractText = null;
        private bool extracted = false;
        private DeathFade deathFade;
        private bool deathFadeEnabled;
        private float DeadTime = 0f;
        private DisablerCullingObjectBase[] allCullingObjects;
        private List<PerfectCullingBakeGroup> previouslyActiveBakeGroups;

        protected void Awake()
        {
            CameraParent = new GameObject("CameraParent");
            Camera FCamera = CameraParent.GetOrAddComponent<Camera>();
            FCamera.enabled = false;
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

            deathFade = CameraClass.Instance.Camera.GetComponent<DeathFade>();
            deathFade.enabled = true;

            allCullingObjects = FindObjectsOfType<DisablerCullingObjectBase>();
            previouslyActiveBakeGroups = [];
        }

        protected void Update()
        {
            if (_gamePlayerOwner == null)
            {
                return;
            }

            if (_player == null)
            {
                return;
            }

            if (_player.PlayerHealthController == null)
            {
                return;
            }

            if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                return;
            }

            CoopGame coopGame = (CoopGame)coopHandler.LocalGameInstance;
            if (coopGame == null)
            {
                return;
            }

            CoopHandler.EQuitState quitState = coopHandler.GetQuitState();

            if (extracted && !_freeCamScript.IsActive)
            {
                ToggleCamera();
            }

            if (FikaPlugin.FreeCamButton.Value.IsDown())
            {
                if (!FikaPlugin.Instance.AllowFreeCam)
                {
                    return;
                }

                if (quitState == CoopHandler.EQuitState.NONE)
                {
                    ToggleCamera();
                    ToggleUi();
                    return;
                }
            }

            if (quitState == CoopHandler.EQuitState.YouHaveExtracted && !extracted)
            {
                if (coopGame.ExtractedPlayers.Contains(((CoopPlayer)_player).NetId))
                {
                    extracted = true;
                    ShowExtractMessage();
                }

                if (!_freeCamScript.IsActive)
                {
                    ToggleCamera();
                    ToggleUi();
                }

                if (!_effectsCleared)
                {
                    if (_player != null)
                    {
                        _player.Muffled = false;
                        _player.HeavyBreath = false;
                    }

                    if (CameraClass.Exist)
                    {
                        ClearEffects();
                    }
                    _effectsCleared = true;
                }
            }

            if (!_player.HealthController.IsAlive)
            {
                DeadTime += Time.deltaTime;
                if (!deathFadeEnabled)
                {
                    deathFade.EnableEffect();
                    deathFadeEnabled = true;
                }
            }

            // Player is dead. Remove all effects!
            if (!_player.HealthController.IsAlive && !_freeCamScript.IsActive && DeadTime > 5f)
            {
                CameraClass cameraClassInstance = CameraClass.Instance;
                if (cameraClassInstance == null)
                {
                    return;
                }

                if (cameraClassInstance.EffectsController == null)
                {
                    return;
                }

                if (cameraClassInstance.Camera != null)
                {
                    cameraClassInstance.Camera.fieldOfView = Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView;
                }

                // Disable the DeathFade effect & Toggle the Camera
                deathFade.DisableEffect();
                ToggleCamera();
                ToggleUi();
                ShowExtractMessage();

                if (!_effectsCleared)
                {
                    if (_player != null)
                    {
                        _player.Muffled = false;
                        _player.HeavyBreath = false;
                    }

                    if (CameraClass.Exist)
                    {
                        ClearEffects();
                    }
                    _effectsCleared = true;
                }
            }
        }

        private void ClearEffects()
        {
            CameraClass cameraClass = CameraClass.Instance;
            cameraClass.EffectsController.method_4(false);
            cameraClass.EffectsController.enabled = false;
            Destroy(cameraClass.EffectsController);
            cameraClass.VisorEffect.Clear();
            Destroy(cameraClass.VisorEffect);
            cameraClass.VisorSwitcher.Deinit();
            Destroy(cameraClass.VisorSwitcher);
            Destroy(cameraClass.NightVision);
            Destroy(cameraClass.ThermalVision);
        }

        private void ShowExtractMessage()
        {
            if (FikaPlugin.ShowExtractMessage.Value)
            {
                extractText = FikaUIUtils.CreateOverlayText("Press 'F8' to extract");
            }
        }

        /// <summary>
        /// Toggles the Freecam mode
        /// </summary>
        public void ToggleCamera()
        {
            // Get our own Player instance. Null means we're not in a raid
            if (_player == null)
            {
                return;
            }

            if (!_freeCamScript.IsActive)
            {
                SetPlayerToFreecamMode(_player);
            }
            else
            {
                SetPlayerToFirstPersonMode(_player);
            }
        }

        /// <summary>
        /// Hides the main UI (health, stamina, stance, hotbar, etc.)
        /// </summary>
        private void ToggleUi()
        {
            // Check if we're currently in a raid
            if (_player == null)
            {
                return;
            }

            // If we don't have the UI Component cached, go look for it in the scene
            if (_playerUi == null)
            {
                GameObject gameObject = GameObject.Find("BattleUIScreen");
                if (gameObject == null)
                {
                    return;
                }

                _playerUi = gameObject.GetComponent<BattleUIScreen>();

                if (_playerUi == null)
                {
                    //FreecamPlugin.Logger.LogError("Failed to locate player UI");
                    return;
                }
            }

            if (_playerUi == null || _playerUi.gameObject == null)
            {
                return;
            }

            _playerUi.gameObject.SetActive(_uiHidden);
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
            _freeCamScript.SetActive(true);

            int count = 0;
            foreach (DisablerCullingObjectBase cullingObject in allCullingObjects)
            {
                if (cullingObject.HasEntered)
                {
                    continue;
                }
                count++;
                cullingObject.SetComponentsEnabled(true);
            }
            FikaPlugin.Instance.FikaLogger.LogDebug($"Enabled {count} Culling Triggers.");

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
                            }
                            else
                            {
                                previouslyActiveBakeGroups.Add(bakeGroup);
                            }
                        }

                        sceneGroup.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// A helper method to reset the player view back to First Person
        /// </summary>
        /// <param name="localPlayer"></param>
        private void SetPlayerToFirstPersonMode(Player localPlayer)
        {
            // re-enable _gamePlayerOwner
            _gamePlayerOwner.enabled = true;
            _freeCamScript.SetActive(false);

            localPlayer.PointOfView = EPointOfView.FirstPerson;
            CameraClass.Instance.SetOcclusionCullingEnabled(true);

            int count = 0;
            foreach (DisablerCullingObjectBase cullingObject in allCullingObjects)
            {
                if (cullingObject.HasEntered)
                {
                    continue;
                }
                count++;
                cullingObject.SetComponentsEnabled(false);
            }
            FikaPlugin.Instance.FikaLogger.LogDebug($"Disabled {count} Culling Triggers.");

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
                            if (bakeGroup.IsEnabled && !previouslyActiveBakeGroups.Contains(bakeGroup))
                            {
                                bakeGroup.IsEnabled = false;
                            }
                            else
                            {
                                previouslyActiveBakeGroups.Remove(bakeGroup);
                            }
                        }

                        previouslyActiveBakeGroups.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current <see cref="Player"/> instance if it's available
        /// </summary>
        /// <returns>Local <see cref="Player"/> instance; returns null if the game is not in raid</returns>
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
            Destroy(CameraParent);

            // Destroy FreeCamScript before FreeCamController if exists
            Destroy(_freeCamScript);
            Destroy(this);
            if (extractText != null)
            {
                Destroy(extractText);
            }
        }
    }
}
