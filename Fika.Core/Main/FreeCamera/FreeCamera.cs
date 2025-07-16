using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;
using Fika.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Main.FreeCamera
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
        public bool IsActive { get; set; }

        private const float _lookSensitivity = 3f;
        private const float _minFov = 10f;

        private bool _isSpectator;
        private CoopPlayer _currentPlayer;
        private Vector3 _lastKnownPlayerPosition;
        private bool _isFollowing;
        private bool _isSpectatingBots;
        private bool _leftMode;
        private bool _disableInput;
        private bool _showOverlay;
        private NightVision _nightVision;
        private ThermalVision _thermalVision;
        private FreeCameraController _freeCameraController;
        private float _yaw;
        private float _pitch;
        private float _originalFov;
        private bool _nightVisionActive;
        private bool _thermalVisionActive;
        private FreecamUI _freecamUI;

        private KeyCode _forwardKey;
        private KeyCode _backKey;
        private KeyCode _leftKey;
        private KeyCode _rightKey;
        private KeyCode _relUpKey;
        private KeyCode _relDownKey;
        private KeyCode _detachKey;
        private KeyCode _upKey;
        private KeyCode _downKey;

        protected void Awake()
        {
            _isSpectator = FikaBackendUtils.IsSpectator;
            _yaw = 0f;
            _pitch = 0f;
            _forwardKey = KeyCode.W;
            _backKey = KeyCode.S;
            _leftKey = KeyCode.A;
            _rightKey = KeyCode.D;
            _relUpKey = KeyCode.E;
            _relDownKey = KeyCode.Q;
            _detachKey = KeyCode.G;
            _upKey = KeyCode.R;
            _downKey = KeyCode.F;
        }

        protected void Start()
        {
            if (FikaPlugin.AZERTYMode.Value)
            {
                _forwardKey = KeyCode.Z;
                _backKey = KeyCode.S;
                _leftKey = KeyCode.Q;
                _rightKey = KeyCode.D;

                _relUpKey = KeyCode.E;
                _relDownKey = KeyCode.A;
            }

            _showOverlay = FikaPlugin.KeybindOverlay.Value;
            FikaPlugin.KeybindOverlay.SettingChanged += KeybindOverlay_SettingChanged;

            _nightVision = CameraClass.Instance.NightVision;
            _thermalVision = CameraClass.Instance.ThermalVision;

            _freeCameraController = Singleton<GameWorld>.Instance.gameObject.GetComponent<FreeCameraController>();
            _originalFov = CameraClass.Instance.Fov;

            GameObject asset = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.FreecamUI);
            GameObject freecamObject = Instantiate(asset);
            freecamObject.transform.SetParent(transform);
            _freecamUI = freecamObject.GetComponent<FreecamUI>();
            if (_freecamUI == null)
            {
                throw new NullReferenceException("Could not assign FreecamUI");
            }
            freecamObject.SetActive(false);
        }

        private void KeybindOverlay_SettingChanged(object sender, EventArgs e)
        {
            _showOverlay = FikaPlugin.KeybindOverlay.Value;
            if (IsActive)
            {
                _freecamUI.gameObject.SetActive(_showOverlay);
            }
        }

        public void SetCurrentPlayer(CoopPlayer player)
        {
            _currentPlayer = player;
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Setting player to {_currentPlayer}");
#endif
        }

        public void DetachCamera()
        {
            _currentPlayer = null;
            if (_isFollowing)
            {
                _isFollowing = false;
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
                if (FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator)
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

            List<CoopPlayer> players = [];
            List<CoopPlayer> humanPlayers = coopHandler.HumanPlayers;
            for (int i = 0; i < humanPlayers.Count; i++)
            {
                CoopPlayer player = humanPlayers[i];
                if (!player.IsYourPlayer && player.HealthController.IsAlive)
                {
                    players.Add(player);
                }
            }
            // If no alive players, add bots to spectate pool if enabled
#if DEBUG
            if (FikaPlugin.AllowSpectateBots.Value)
#else

            if (players.Count <= 0 && FikaPlugin.AllowSpectateBots.Value)
#endif
            {
                _isSpectatingBots = true;
                if (FikaBackendUtils.IsServer)
                {
                    foreach (CoopPlayer player in coopHandler.Players.Values)
                    {
                        if (player.IsAI && player.HealthController.IsAlive)
                        {
                            players.Add(player);
                        }
                    }
                }
                else
                {
                    foreach (CoopPlayer player in coopHandler.Players.Values)
                    {
                        if (player.IsObservedAI && player.HealthController.IsAlive)
                        {
                            players.Add(player);
                        }
                    }
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
            if (_currentPlayer == null && players[0])
            {
                _currentPlayer = players[0];
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: currentPlayer was null, setting to first player {players[0].Profile.Nickname}");
#endif
                SwitchSpectateMode();
                return;
            }

            // Cycle through spectate-able players
            int nextIndex = reverse ? players.IndexOf(_currentPlayer) - 1 : players.IndexOf(_currentPlayer) + 1;
            if (!reverse)
            {
                if (nextIndex <= players.Count - 1)
                {
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Setting to next player");
#endif
                    _currentPlayer = players[nextIndex];
                    SwitchSpectateMode();
                }
                else
                {
                    // hit end of list, loop from start
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Looping back to start player");
#endif
                    _currentPlayer = players[0];
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
                    _currentPlayer = players[nextIndex];
                    SwitchSpectateMode();
                }
                else
                {
                    // hit beginning of list, loop from end
#if DEBUG
                    FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Looping back to end player");
#endif
                    _currentPlayer = players[players.Count - 1];
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
                _disableInput = !_disableInput;
                _freecamUI.InputText.SetText($"HOME: {(_disableInput ? "Enable Input" : "Disable Input")}");
                NotificationManagerClass.DisplayMessageNotification(_disableInput ? LocaleUtils.FREECAM_DISABLED.Localized() : LocaleUtils.FREECAM_ENABLED.Localized());
            }

            if (_disableInput)
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
                if (_freeCameraController != null)
                {
                    _freeCameraController.DisableAllCullingObjects();
                    return;
                }
            }

            if (Input.GetKeyDown(_detachKey) && (_isSpectatingBots || FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator))
            {
                DetachCamera();
            }

            if (_isFollowing)
            {
                if (_currentPlayer != null)
                {
                    _lastKnownPlayerPosition = _currentPlayer.PlayerBones.Neck.position;
                    if (_currentPlayer.MovementContext.LeftStanceEnabled && !_leftMode)
                    {
#if DEBUG
                        FikaPlugin.Instance.FikaLogger.LogInfo("Setting left shoulder mode");
#endif
                        SetLeftShoulderMode(true);
                    }
                    else if (!_currentPlayer.MovementContext.LeftStanceEnabled && _leftMode)
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
                    if (_currentPlayer == null)
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

            if (Input.GetKey(_leftKey))
            {
                transform.position += -transform.right * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(_rightKey))
            {
                transform.position += transform.right * (movementSpeed * deltaTime);
            }

            if (FikaPlugin.DroneMode.Value)
            {
                if (Input.GetKey(_forwardKey))
                {
                    transform.position += GetNormalizedVector3(transform) * (movementSpeed * deltaTime);
                }

                if (Input.GetKey(_backKey))
                {
                    transform.position += -GetNormalizedVector3(transform) * (movementSpeed * deltaTime);
                }
            }
            else
            {
                if (Input.GetKey(_forwardKey))
                {
                    transform.position += transform.forward * (movementSpeed * deltaTime);
                }

                if (Input.GetKey(_backKey))
                {
                    transform.position += -transform.forward * (movementSpeed * deltaTime);
                }
            }

            if (Input.GetKey(_relUpKey))
            {
                transform.position += transform.up * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(_relDownKey))
            {
                transform.position += -transform.up * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(_upKey))
            {
                transform.position += Vector3.up * (movementSpeed * deltaTime);
            }

            if (Input.GetKey(_downKey))
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
                if (currentFov >= _minFov && currentFov <= _originalFov)
                {
                    float newFov = Mathf.Clamp(currentFov -= (scrollValue * 100), _minFov, _originalFov);
                    CameraClass.Instance.SetFov(newFov, 1f);
                }
            }

            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");

            _pitch += y * _lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -89, 89);
            transform.eulerAngles = new(-_pitch, _yaw, 0);
            _yaw = (_yaw + x * _lookSensitivity) % 360f;
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
                _leftMode = true;

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
            _leftMode = false;
        }

        private void ToggleVision()
        {
            if (_nightVision != null && _thermalVision != null)
            {
                if (!_nightVision.On && !_thermalVision.On)
                {
                    _nightVision.On = true;
                    _freecamUI.VisionText.SetText("N: Enable thermals");
                }
                else if (_nightVision.On && !_thermalVision.On)
                {
                    _nightVision.On = false;
                    _thermalVision.On = true;
                    _freecamUI.VisionText.SetText("N: Disable thermals");
                }
                else if (_thermalVision.On)
                {
                    _thermalVision.On = false;
                    _freecamUI.VisionText.SetText("N: Enable nightvision");
                }
            }
        }

        public void JumpToPlayer()
        {
            Vector3 position = _currentPlayer.PlayerBones.Neck.position;
            transform.position = position + Vector3.back + (Vector3.up / 2);
            transform.LookAt(position);

            _pitch = -transform.eulerAngles.x;
            _yaw = transform.eulerAngles.y;

            if (_isFollowing)
            {
                _isFollowing = false;
                _leftMode = false;
                transform.parent = null;
            }
        }

        public void AttachToPlayer()
        {
            CheckAndResetFov();
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to helmet cam current player {_currentPlayer.Profile.Nickname}");
#endif
            transform.SetParent(_currentPlayer.PlayerBones.Head.Original);
            transform.localPosition = new Vector3(-0.1f, -0.07f, -0.17f);
            transform.localEulerAngles = new Vector3(260, 80, 0);
            _isFollowing = true;
        }

        public void AttachToMap()
        {
            if (_lastKnownPlayerPosition != null)
            {
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to last tracked player position {_lastKnownPlayerPosition}");
#endif
                transform.position = _lastKnownPlayerPosition;
                return;
            }
        }

        public void Attach3rdPerson()
        {
            CheckAndResetFov();
#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to 3rd person current player {_currentPlayer.Profile.Nickname}");
#endif
            if (!_currentPlayer.IsAI)
            {
                transform.SetParent(_currentPlayer.SpectateTransform);
                transform.localPosition = new Vector3(0.3f, 0.2f, -0.65f);
                transform.localEulerAngles = new Vector3(4.3f, 5.9f, 0f);
            }
            else
            {
                transform.SetParent(_currentPlayer.PlayerBones.Head.Original);
                transform.localPosition = new Vector3(0f, -0.32f, -0.53f);
                transform.localEulerAngles = new Vector3(-115f, 99f, 5f);
            }
            _isFollowing = true;
        }

        public void SetActive(bool active)
        {
            if (!active)
            {
                _freecamUI.gameObject.SetActive(false);
                if (_nightVision != null && _nightVision.On)
                {
                    _nightVision.method_1(false);
                }

                if (_thermalVision != null && _thermalVision.On)
                {
                    _thermalVision.method_1(false);
                }

                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                if (player != null && player.HealthController.IsAlive)
                {
                    if (_nightVisionActive)
                    {
                        player.NightVisionObserver.Component.Togglable.ForceToggle(true);
                    }

                    if (_thermalVisionActive)
                    {
                        player.ThermalVisionObserver.Component.Togglable.ForceToggle(true);
                    }
                }

                CheckAndResetFov();
            }

            if (active)
            {
                if (_showOverlay)
                {
                    _freecamUI.gameObject.SetActive(true);
                    _freecamUI.VisionText.SetText("N: Enable nightvision"); 
                }
                _nightVisionActive = false;
                _thermalVisionActive = false;
                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                if (player != null && player.HealthController.IsAlive)
                {
                    if (player.NightVisionObserver.Component != null && player.NightVisionObserver.Component.Togglable.On)
                    {
                        player.NightVisionObserver.Component.Togglable.ForceToggle(false);
                        _nightVisionActive = true;
                    }

                    if (player.ThermalVisionObserver.Component != null && player.ThermalVisionObserver.Component.Togglable.On)
                    {
                        player.ThermalVisionObserver.Component.Togglable.ForceToggle(false);
                        _thermalVisionActive = true;
                    }
                }
                else if (player != null && !player.HealthController.IsAlive)
                {
                    if (_nightVision != null && _nightVision.On)
                    {
                        _nightVision.method_1(false);
                    }

                    if (_thermalVision != null && _thermalVision.On)
                    {
                        _thermalVision.method_1(false);
                    }
                }
            }

            IsActive = active;
            _isFollowing = false;
            _leftMode = false;
            transform.parent = null;
        }

        private void CheckAndResetFov()
        {
            if (CameraClass.Instance.Fov != _originalFov)
            {
                CameraClass.Instance.SetFov(_originalFov, 0.1f);
            }
        }

        protected void OnDestroy()
        {
            FikaPlugin.KeybindOverlay.SettingChanged -= KeybindOverlay_SettingChanged;
            Destroy(_freecamUI.gameObject);
            Destroy(this);
        }
    }
}