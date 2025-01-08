using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.FreeCamera
{
    /// <summary>
    /// A simple free camera to be added to a Unity game object. <br/><br/>
    /// 
    /// Full credit to Ashley Davis on GitHub for the inital code:<br/>
    /// <see href="https://gist.github.com/ashleydavis/f025c03a9221bc840a2b"/><br/><br/>
    /// 
    /// This is HEAVILY based on Terkoiz's work found here. Thanks for your work Terkoiz! <br/>
    /// <see href="https://dev.sp-tarkov.com/Terkoiz/Freecam/raw/branch/master/project/Terkoiz.Freecam/FreecamController.cs"/>
    /// </summary>
    public class FreeCamera : MonoBehaviour
    {
        public bool IsActive = false;
        private readonly bool isSpectator = FikaBackendUtils.IsSpectator;
        private CoopPlayer currentPlayer;
        private Vector3 lastKnownPlayerPosition;
        private bool isFollowing = false;
        private bool isSpectatingBots = false;
        private bool leftMode = false;
        private bool disableInput = false;
        private bool showOverlay;
        private NightVision nightVision;
        private ThermalVision thermalVision;
        private FreeCameraController freeCameraController;
        private float yaw = 0f;
        private float pitch = 0f;
        private const float lookSensitivity = 3f;
        private const float minFov = 10f;
        private float originalFov;
        private bool nightVisionActive = false;
        private bool thermalVisionActive = false;

        private KeyCode forwardKey = KeyCode.W;
        private KeyCode backKey = KeyCode.S;
        private KeyCode leftKey = KeyCode.A;
        private KeyCode rightKey = KeyCode.D;
        private KeyCode relUpKey = KeyCode.E;
        private KeyCode relDownKey = KeyCode.Q;
        private KeyCode detachKey = KeyCode.G;
        private readonly KeyCode upKey = KeyCode.R;
        private readonly KeyCode downKey = KeyCode.F;

        protected void Start()
        {
            if (FikaPlugin.AZERTYMode.Value)
            {
                forwardKey = KeyCode.Z;
                backKey = KeyCode.S;
                leftKey = KeyCode.Q;
                rightKey = KeyCode.D;

                relUpKey = KeyCode.E;
                relDownKey = KeyCode.A;
            }

            showOverlay = FikaPlugin.KeybindOverlay.Value;
            FikaPlugin.KeybindOverlay.SettingChanged += KeybindOverlay_SettingChanged;

            nightVision = CameraClass.Instance.NightVision;
            thermalVision = CameraClass.Instance.ThermalVision;

            freeCameraController = Singleton<GameWorld>.Instance.gameObject.GetComponent<FreeCameraController>();
            originalFov = CameraClass.Instance.Fov;
        }

        private void KeybindOverlay_SettingChanged(object sender, EventArgs e)
        {
            showOverlay = FikaPlugin.KeybindOverlay.Value;
        }

        public void SetCurrentPlayer(CoopPlayer player)
        {
            currentPlayer = player;
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Setting player to {currentPlayer}");
#endif
        }

        protected void OnGUI()
        {
            if (IsActive && showOverlay)
            {
                string visionText = "Enable nightvision";

                if (nightVision != null && nightVision.On)
                {
                    visionText = "Enable thermals";
                }

                if (thermalVision != null && thermalVision.On)
                {
                    visionText = "Disable thermals";
                }

                GUILayout.BeginArea(new Rect(5, 5, 800, 800));
                GUILayout.BeginVertical();

                if (FikaPlugin.Instance.AllowSpectateFreeCam || isSpectator)
                {
                    GUILayout.Label($"Left/Right Mouse Button: Jump between players");
                    GUILayout.Label($"CTRL + Left/Right Mouse Button: Jump and spectate in 3rd person");
                }
                else
                {
                    GUILayout.Label($"Left/Right Mouse Button: Jump and spectate in 3rd person");
                }
                GUILayout.Label($"Spacebar + Left/Right Mouse Button: Jump and spectate in head cam");
                if (FikaPlugin.Instance.AllowSpectateFreeCam || isSpectator || isSpectatingBots)
                {
                    GUILayout.Label($"G: Detach Camera");
                }
                GUILayout.Label($"T: Teleport to cam position");
                GUILayout.Label($"N: {visionText}");
                GUILayout.Label($"M: Disable culling");
                GUILayout.Label($"HOME: {(disableInput ? "Enable Input" : "Disable Input")}");
                GUILayout.Label($"Shift + Ctrl: Turbo Speed");

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        public void DetachCamera()
        {
            currentPlayer = null;
            if (isFollowing)
            {
                isFollowing = false;
                transform.parent = null;
            }
            return;
        }

        public void SwitchSpectateMode()
        {
            bool shouldHeadCam = Input.GetKey(KeyCode.Space);
            bool should3rdPerson = Input.GetKey(KeyCode.LeftControl);
            if (shouldHeadCam)
            {
                AttachToPlayer();
            }
            else if (should3rdPerson)
            {
                Attach3rdPerson();
            }
            else
            {
                if (FikaPlugin.Instance.AllowSpectateFreeCam || isSpectator)
                {
                    JumpToPlayer();
                }
                else
                {
                    Attach3rdPerson();
                }
            }
        }

        /// <summary>
        /// Helper method to cycle spectating players
        /// </summary>
        /// <param name="reverse">
        /// If true, cycle players in reverse direction
        /// </param>
        public void CycleSpectatePlayers(bool reverse = false)
        {
            if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                return;
            }

            List<CoopPlayer> players = [.. coopHandler.HumanPlayers.Where(x => !x.IsYourPlayer && x.HealthController.IsAlive)];
            // If no alive players, add bots to spectate pool if enabled
            if (players.Count <= 0 && FikaPlugin.AllowSpectateBots.Value)
            {
                isSpectatingBots = true;
                if (FikaBackendUtils.IsServer)
                {
                    players = [.. coopHandler.Players.Values.Where(x => x.IsAI && x.HealthController.IsAlive)];
                }
                else
                {
                    players = [.. coopHandler.Players.Values.Where(x => x.IsObservedAI && x.HealthController.IsAlive)];
                }
            }
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: There are {players.Count} players");
#endif

            if (players.Count <= 0)
            {
                // Clear out all spectate positions
                DetachCamera();

                return;
            }

            // Start spectating a player if we haven't before
            if (currentPlayer == null && players[0])
            {
                currentPlayer = players[0];
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: currentPlayer was null, setting to first player {players[0].Profile.Nickname}");
#endif
                SwitchSpectateMode();
                return;
            }

            // Cycle through spectate-able players
            int nextIndex = reverse ? players.IndexOf(currentPlayer) - 1 : players.IndexOf(currentPlayer) + 1;
            if (!reverse)
            {
                if (nextIndex <= players.Count - 1)
                {
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Setting to next player");
#endif
                    currentPlayer = players[nextIndex];
                    SwitchSpectateMode();
                }
                else
                {
                    // hit end of list, loop from start
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Looping back to start player");
#endif
                    currentPlayer = players[0];
                    SwitchSpectateMode();
                }
            }
            else
            {
                if (nextIndex >= 0)
                {
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Setting to previous player");
#endif
                    currentPlayer = players[nextIndex];
                    SwitchSpectateMode();
                }
                else
                {
                    // hit beginning of list, loop from end
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Looping back to end player");
#endif
                    currentPlayer = players[players.Count - 1];
                    SwitchSpectateMode();
                }
            }
        }

        protected void Update()
        {
            if (!IsActive)
            {
                return;
            }

            // Toggle input
            if (Input.GetKeyDown(KeyCode.Home))
            {
                disableInput = !disableInput;
                NotificationManagerClass.DisplayMessageNotification(disableInput ? LocaleUtils.FREECAM_DISABLED.Localized() : LocaleUtils.FREECAM_ENABLED.Localized());
            }

            if (disableInput)
            {
                return;
            }

            if (MonoBehaviourSingleton<PreloaderUI>.Instance.Console.IsConsoleVisible)
            {
                return;
            }

            // Spectate next player
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                CycleSpectatePlayers(false);
                return;
            }

            // Spectate previous player
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                CycleSpectatePlayers(true);
                return;
            }

            // Toggle vision
            if (Input.GetKeyDown(KeyCode.N))
            {
                ToggleVision();
                return;
            }

            // Disable culling
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (freeCameraController != null)
                {
                    freeCameraController.DisableAllCullingObjects();
                    return;
                }
            }

            if (Input.GetKeyDown(detachKey) && (isSpectatingBots || FikaPlugin.Instance.AllowSpectateFreeCam || isSpectator))
            {
                DetachCamera();
            }

            if (isFollowing)
            {
                if (currentPlayer != null)
                {
                    lastKnownPlayerPosition = currentPlayer.PlayerBones.Neck.position;
                    if (currentPlayer.MovementContext.LeftStanceEnabled && !leftMode)
                    {
#if DEBUG
                        FikaPlugin.Instance.FikaLogger.LogInfo("Setting left shoulder mode");
#endif
                        SetLeftShoulderMode(true);
                    }
                    else if (!currentPlayer.MovementContext.LeftStanceEnabled && leftMode)
                    {
#if DEBUG
                        FikaPlugin.Instance.FikaLogger.LogInfo("Unsetting left shoulder mode");
#endif
                        SetLeftShoulderMode(false);
                    }
                }
                else
                {
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: currentPlayer vanished while we were following, finding next player to attach to");
#endif
                    CycleSpectatePlayers();
                    if (currentPlayer == null)
                    {
                        // still no players, let's go to map
                        AttachToMap();
                    }
                }
                return;
            }

            bool fastMode = Input.GetKey(KeyCode.LeftShift);
            bool superFastMode = Input.GetKey(KeyCode.LeftControl);
            float movementSpeed = fastMode ? 20f : 2f;
            float deltaTime = Time.deltaTime;

            if (superFastMode)
            {
                movementSpeed *= 12;
            }

            if (Input.GetKey(leftKey))
            {
                transform.position += -transform.right * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(rightKey))
            {
                transform.position += transform.right * (movementSpeed * deltaTime);
            }

            if (FikaPlugin.DroneMode.Value)
            {
                if (Input.GetKey(forwardKey))
                {
                    transform.position += GetNormalizedVector3(transform) * (movementSpeed * deltaTime);
                }

                if (Input.GetKey(backKey))
                {
                    transform.position += -GetNormalizedVector3(transform) * (movementSpeed * deltaTime);
                }
            }
            else
            {
                if (Input.GetKey(forwardKey))
                {
                    transform.position += transform.forward * (movementSpeed * deltaTime);
                }

                if (Input.GetKey(backKey))
                {
                    transform.position += -transform.forward * (movementSpeed * deltaTime);
                }
            }

            if (Input.GetKey(relUpKey))
            {
                transform.position += transform.up * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(relDownKey))
            {
                transform.position += -transform.up * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(upKey))
            {
                transform.position += Vector3.up * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(downKey))
            {
                transform.position += -Vector3.up * (movementSpeed * deltaTime);
            }

            // Teleportation
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    return;
                }

                CoopPlayer player = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;

                if (player != null && !coopHandler.ExtractedPlayers.Contains(player.NetId) && player.HealthController.IsAlive)
                {
                    player.Teleport(transform.position);
                }
            }

            // Zooming
            float scrollValue = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scrollValue != 0)
            {
                float currentFov = CameraClass.Instance.Fov;
                if (currentFov >= minFov && currentFov <= originalFov)
                {
                    float newFov = Mathf.Clamp(currentFov -= (scrollValue * 100), minFov, originalFov);
                    CameraClass.Instance.SetFov(newFov, 1f);
                }
            }

            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");

            pitch += y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89, 89);
            transform.eulerAngles = new(-pitch, yaw, 0);
            yaw = (yaw + x * lookSensitivity) % 360f;
        }

        private Vector3 GetNormalizedVector3(Transform transform)
        {
            Vector3 newForward = transform.forward;
            newForward.y = 0f;
            return newForward.normalized;
        }

        private void SetLeftShoulderMode(bool enabled)
        {
            if (enabled)
            {
                // Use different coordinates for headcam
                if (transform.localPosition.z == -0.17f)
                {
                    transform.localPosition = new(transform.localPosition.x, transform.localPosition.y, -transform.localPosition.z);
                }
                else
                {
                    transform.localPosition = new(-transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
                }
                leftMode = true;

                return;
            }

            // Use different coordinates for headcam
            if (transform.localPosition.z == 0.17f)
            {
                transform.localPosition = new(transform.localPosition.x, transform.localPosition.y, -transform.localPosition.z);
            }
            else
            {
                transform.localPosition = new(-transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
            }
            leftMode = false;
        }

        private void ToggleVision()
        {
            if (nightVision != null && thermalVision != null)
            {
                if (!nightVision.On && !thermalVision.On)
                {
                    nightVision.On = true;
                }
                else if (nightVision.On && !thermalVision.On)
                {
                    nightVision.On = false;
                    thermalVision.On = true;
                }
                else if (thermalVision.On)
                {
                    thermalVision.On = false;
                }
            }
        }

        public void JumpToPlayer()
        {
            Vector3 position = currentPlayer.PlayerBones.Neck.position;
            transform.position = position + Vector3.back + (Vector3.up / 2);
            transform.LookAt(position);

            pitch = -transform.eulerAngles.x;
            yaw = transform.eulerAngles.y;

            if (isFollowing)
            {
                isFollowing = false;
                leftMode = false;
                transform.parent = null;
            }
        }

        public void AttachDedicated(CoopPlayer player)
        {
            FikaPlugin.Instance.FikaLogger.LogInfo("Attaching camera to: " + player.Profile.Info.MainProfileNickname);
            transform.SetParent(player.Transform.Original);
            transform.localPosition = new(0, 125, 0);
            transform.LookAt(player.PlayerBones.Head.position);

            pitch = -transform.eulerAngles.x;
            yaw = transform.eulerAngles.y;
        }

        public void AttachToPlayer()
        {
            CheckAndResetFov();
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to helmet cam current player {currentPlayer.Profile.Nickname}");
#endif
            transform.SetParent(currentPlayer.PlayerBones.Head.Original);
            transform.localPosition = new Vector3(-0.1f, -0.07f, -0.17f);
            transform.localEulerAngles = new Vector3(260, 80, 0);
            isFollowing = true;
        }

        public void AttachToMap()
        {
            if (lastKnownPlayerPosition != null)
            {
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to last tracked player position {lastKnownPlayerPosition}");
#endif
                transform.position = lastKnownPlayerPosition;
                return;
            }
        }

        public void Attach3rdPerson()
        {
            CheckAndResetFov();
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to 3rd person current player {currentPlayer.Profile.Nickname}");
#endif
            if (!currentPlayer.IsAI)
            {
                transform.SetParent(currentPlayer.SpectateTransform);
                transform.localPosition = new Vector3(0.3f, 0.2f, -0.65f);
                transform.localEulerAngles = new Vector3(4.3f, 5.9f, 0f);
            }
            else
            {
                transform.SetParent(currentPlayer.PlayerBones.Head.Original);
                transform.localPosition = new Vector3(0f, -0.32f, -0.53f);
                transform.localEulerAngles = new Vector3(-115f, 99f, 5f);
            }
            isFollowing = true;
        }

        public void SetActive(bool active)
        {
            if (!active)
            {
                if (nightVision != null && nightVision.On)
                {
                    nightVision.method_1(false);
                }

                if (thermalVision != null && thermalVision.On)
                {
                    thermalVision.method_1(false);
                }

                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                if (player != null && player.HealthController.IsAlive)
                {
                    if (nightVisionActive)
                    {
                        player.NightVisionObserver.Component.Togglable.ForceToggle(true);
                    }

                    if (thermalVisionActive)
                    {
                        player.ThermalVisionObserver.Component.Togglable.ForceToggle(true);
                    }
                }

                CheckAndResetFov();
            }

            if (active)
            {
                nightVisionActive = false;
                thermalVisionActive = false;
                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                if (player != null && player.HealthController.IsAlive)
                {
                    if (player.NightVisionObserver.Component != null && player.NightVisionObserver.Component.Togglable.On)
                    {
                        player.NightVisionObserver.Component.Togglable.ForceToggle(false);
                        nightVisionActive = true;
                    }

                    if (player.ThermalVisionObserver.Component != null && player.ThermalVisionObserver.Component.Togglable.On)
                    {
                        player.ThermalVisionObserver.Component.Togglable.ForceToggle(false);
                        thermalVisionActive = true;
                    }
                }
                else if (player != null && !player.HealthController.IsAlive)
                {
                    if (nightVision != null && nightVision.On)
                    {
                        nightVision.method_1(false);
                    }

                    if (thermalVision != null && thermalVision.On)
                    {
                        thermalVision.method_1(false);
                    }
                }
            }

            IsActive = active;
            isFollowing = false;
            leftMode = false;
            transform.parent = null;
        }

        private void CheckAndResetFov()
        {
            if (CameraClass.Instance.Fov != originalFov)
            {
                CameraClass.Instance.SetFov(originalFov, 0.1f);
            }
        }

        protected void OnDestroy()
        {
            Destroy(this);
        }
    }
}