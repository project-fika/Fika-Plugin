using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.UI;
using HarmonyLib;
using Koenigz.PerfectCulling;
using Koenigz.PerfectCulling.EFT;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private FreeCamera freeCamScript;

        private EftBattleUIScreen playerUi;
        private bool uiHidden;

        private bool effectsCleared = false;

        private GamePlayerOwner gamePlayerOwner;
        private CoopPlayer player => (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        private CoopHandler coopHandler;

        public GameObject CameraParent;
        public Camera CameraMain { get; private set; }
        public bool IsScriptActive
        {
            get
            {
                if (freeCamScript != null)
                {
                    return freeCamScript.IsActive;
                }
                return false;
            }
        }

        private TextMeshProUGUI extractText = null;
        private bool extracted = false;
        private DeathFade deathFade;
        private bool deathFadeEnabled;
        private DisablerCullingObjectBase[] allCullingObjects;
        private List<PerfectCullingBakeGroup> previouslyActiveBakeGroups;
        private bool hasEnabledCulling = false;

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
            freeCamScript = CameraMain.gameObject.AddComponent<FreeCamera>();
            if (freeCamScript == null)
            {
                return;
            }

            // Get GamePlayerOwner component
            gamePlayerOwner = GetLocalPlayerFromWorld().GetComponentInChildren<GamePlayerOwner>();
            if (gamePlayerOwner == null)
            {
                return;
            }

            deathFade = CameraClass.Instance.Camera.GetComponent<DeathFade>();
            deathFade.enabled = true;

            allCullingObjects = FindObjectsOfType<DisablerCullingObjectBase>();
            previouslyActiveBakeGroups = [];

            player.ActiveHealthController.DiedEvent += MainPlayer_DiedEvent;

            if (CoopHandler.TryGetCoopHandler(out CoopHandler cHandler))
            {
                coopHandler = cHandler;
            }
        }

        private void MainPlayer_DiedEvent(EDamageType obj)
        {
            player.ActiveHealthController.DiedEvent -= MainPlayer_DiedEvent;

            if (!deathFadeEnabled)
            {
                deathFade.EnableEffect();
                deathFadeEnabled = true;
            }

            StartCoroutine(DeathRoutine());
        }

        protected void Update()
        {
            if (gamePlayerOwner == null)
            {
                return;
            }

            if (player == null)
            {
                return;
            }

            if (player.PlayerHealthController == null)
            {
                return;
            }

            CoopHandler.EQuitState quitState = coopHandler.GetQuitState();

            if (extracted && !freeCamScript.IsActive)
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
                CoopGame coopGame = coopHandler.LocalGameInstance;
                if (coopGame.ExtractedPlayers.Contains(player.NetId))
                {
                    extracted = true;
                    ShowExtractMessage();
                }

                if (!freeCamScript.IsActive)
                {
                    ToggleCamera();
                    ToggleUi();
                }

                if (!effectsCleared)
                {
                    if (player != null)
                    {
                        player.Muffled = false;
                        player.HeavyBreath = false;
                    }

                    if (CameraClass.Exist)
                    {
                        ClearEffects();
                    }
                    effectsCleared = true;
                }
            }
        }

        private IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(5);

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
            deathFade.DisableEffect();
            if (!freeCamScript.IsActive)
            {
                ToggleCamera();
                ToggleUi();
            }
            ShowExtractMessage();

            if (!effectsCleared)
            {
                if (player != null)
                {
                    player.Muffled = false;
                    player.HeavyBreath = false;
                }

                if (CameraClass.Exist)
                {
                    ClearEffects();
                }
                effectsCleared = true;
            }
        }

        private void ClearEffects()
        {
            CameraClass cameraClass = CameraClass.Instance;
            cameraClass.EffectsController.method_4(false);

            Traverse effectsController = Traverse.Create(cameraClass.EffectsController);

            BloodOnScreen bloodOnScreen = effectsController.Field<BloodOnScreen>("bloodOnScreen_0").Value;
            if (bloodOnScreen != null)
            {
                Destroy(bloodOnScreen);
            }

            List<EffectsController.Class576> effectsManagerList = effectsController.Field<List<EffectsController.Class576>>("list_0").Value;
            if (effectsManagerList != null)
            {
                foreach (EffectsController.Class576 effectsManager in effectsManagerList)
                {
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
                extractText = FikaUIUtils.CreateOverlayText($"Press '{text}' to extract");
            }
        }

        /// <summary>
        /// Toggles the Freecam mode
        /// </summary>
        public void ToggleCamera()
        {
            // Get our own Player instance. Null means we're not in a raid
            if (player == null)
            {
                return;
            }

            if (!freeCamScript.IsActive)
            {
                SetPlayerToFreecamMode(player);
            }
            else
            {
                SetPlayerToFirstPersonMode(player);
            }
        }

        /// <summary>
        /// Hides the main UI (health, stamina, stance, hotbar, etc.)
        /// </summary>
        private void ToggleUi()
        {
            // Check if we're currently in a raid
            if (player == null)
            {
                return;
            }

            // If we don't have the UI Component cached, go look for it in the scene
            if (playerUi == null)
            {
                GameObject gameObject = GameObject.Find("BattleUIScreen");
                if (gameObject == null)
                {
                    return;
                }

                playerUi = gameObject.GetComponent<EftBattleUIScreen>();

                if (playerUi == null)
                {
                    //FreecamPlugin.Logger.LogError("Failed to locate player UI");
                    return;
                }
            }

            if (playerUi == null || playerUi.gameObject == null)
            {
                return;
            }

            playerUi.gameObject.SetActive(uiHidden);
            uiHidden = !uiHidden;
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

            gamePlayerOwner.enabled = false;
            freeCamScript.SetActive(true);
        }

        /// <summary>
        /// A helper method to reset the player view back to First Person
        /// </summary>
        /// <param name="localPlayer"></param>
        private void SetPlayerToFirstPersonMode(Player localPlayer)
        {
            // re-enable _gamePlayerOwner
            gamePlayerOwner.enabled = true;
            freeCamScript.SetActive(false);

            localPlayer.PointOfView = EPointOfView.FirstPerson;
            CameraClass.Instance.SetOcclusionCullingEnabled(true);

            if (hasEnabledCulling)
            {
                EnableAllCullingObjects();
            }
        }

        public void DisableAllCullingObjects()
        {
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

            hasEnabledCulling = true;
        }

        public void EnableAllCullingObjects()
        {
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

            hasEnabledCulling = false;
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
            if (!Singleton<FreeCameraController>.TryRelease(this))
            {
                FikaPlugin.Instance.FikaLogger.LogWarning("Unable to release FreeCameraController singleton");
            }
            Destroy(CameraParent);

            // Destroy FreeCamScript before FreeCamController if exists
            Destroy(freeCamScript);
            if (extractText != null)
            {
                Destroy(extractText);
            }
            Destroy(this);
        }
    }
}
