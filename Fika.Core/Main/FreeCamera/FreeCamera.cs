using System;
using System.Collections;
using System.Collections.Generic;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using UnityEngine.UI;

namespace Fika.Core.Main.FreeCamera;

/// <summary>
/// <para>FreeCamera modified for Fika</para>
/// <para>
/// This is based on the original freecam by Terkoiz <br/>
/// <see href="https://github.com/acidphantasm/SPT-Freecam"/>
/// </para>
/// </summary>
public partial class FreeCamera : MonoBehaviour
{
    public bool IsActive { get; set; }

    private const float _lookSensitivity = 3f;
    private const float _minFov = 10f;

    private bool _isSpectator;
    private FikaPlayer _currentPlayer;
    private Vector3 _lastKnownPlayerPosition;
    private bool _isFollowing;
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
    private CoopHandler _coopHandler;
    private List<FikaPlayer> _players;

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

        _playersTracker = [];
        _playersToRemove = [];
        _players = [];

        if (!CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            FikaGlobals.LogError("Could not find CoopHandler when creating FreeCamera");
            return;
        }

        _coopHandler = coopHandler;

        FikaPlayer.OnPlayerSpawned += OnPlayerSpawned;
    }

    protected void Start()
    {
        if (FikaPlugin.Instance.Settings.AZERTYMode.Value)
        {
            _forwardKey = KeyCode.Z;
            _backKey = KeyCode.S;
            _leftKey = KeyCode.Q;
            _rightKey = KeyCode.D;

            _relUpKey = KeyCode.E;
            _relDownKey = KeyCode.A;
        }

        _showOverlay = FikaPlugin.Instance.Settings.KeybindOverlay.Value;
        FikaPlugin.Instance.Settings.KeybindOverlay.SettingChanged += KeybindOverlay_SettingChanged;

        _nightVision = CameraClass.Instance.NightVision;
        _thermalVision = CameraClass.Instance.ThermalVision;

        _freeCameraController = Singleton<GameWorld>.Instance.gameObject.GetComponent<FreeCameraController>();
        _originalFov = CameraClass.Instance.Fov;

        var asset = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.FreecamUI);
        var freecamObject = Instantiate(asset);
        freecamObject.transform.SetParent(transform);
        _freecamUI = freecamObject.GetComponent<FreecamUI>();
        if (_freecamUI == null)
        {
            throw new NullReferenceException("Could not assign FreecamUI");
        }
        freecamObject.SetActive(false);
        _hidePlayerList = false;

        foreach (var player in _coopHandler.Players.Values)
        {
            if (!player.IsYourPlayer && player.HealthController.IsAlive)
            {
                OnPlayerSpawned(player);
            }
        }
    }

    private void KeybindOverlay_SettingChanged(object sender, EventArgs e)
    {
        _showOverlay = FikaPlugin.Instance.Settings.KeybindOverlay.Value;
        if (IsActive)
        {
            _freecamUI.gameObject.SetActive(_showOverlay);
        }
    }

    public void SetCurrentPlayer(FikaPlayer player)
    {
        if (_currentPlayer != null && _playersTracker.TryGetValue(_currentPlayer.NetId, out var listPlayer))
        {
            listPlayer.ToggleBackground(false);
        }

        _currentPlayer = player;
        if (_currentPlayer != null && _playersTracker.TryGetValue(_currentPlayer.NetId, out listPlayer))
        {
            listPlayer.ToggleBackground(true);
        }
#if DEBUG
        FikaGlobals.LogInfo($"Freecam: Setting player to {_currentPlayer}");
#endif
    }

    public void DetachCamera(bool force = false)
    {
        if (!FikaPlugin.Instance.AllowSpectateFreeCam && !_isSpectator && !force)
        {
            return;
        }

        if (_currentPlayer)
        {
            _lastSpectatingPlayer = _currentPlayer;
        }

        SetCurrentPlayer(null); ;
        if (_isFollowing)
        {
            _isFollowing = false;
            transform.parent = null;
        }
    }

    public void SwitchSpectateMode()
    {
        if (!_isFollowing)
        {
            if ((FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator) && CheckAndAssignPlayer())
            {
                JumpToPlayer();
            }

            return;
        }

        switch (_cameraState)
        {
            case ECameraState.FollowHeadcam:
                AttachToPlayer();
                break;
            case ECameraState.Follow3rdPerson:
                Attach3rdPerson();
                break;
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
        ClearAndAddPlayers();

        // If no alive players, add bots to spectate pool if enabled
#if DEBUG
        if (FikaPlugin.Instance.Settings.AllowSpectateBots.Value)
#else

        if (_players.Count == 0 && FikaPlugin.Instance.Settings.AllowSpectateBots.Value)
#endif
        {
            if (FikaBackendUtils.IsServer)
            {
                foreach (var player in _coopHandler.Players.Values)
                {
                    if (player.IsAI && player.HealthController.IsAlive)
                    {
                        _players.Add(player);
                    }
                }
            }
            else
            {
                foreach (var player in _coopHandler.Players.Values)
                {
                    if (player.IsObservedAI && player.HealthController.IsAlive)
                    {
                        _players.Add(player);
                    }
                }
            }
        }
#if DEBUG
        FikaGlobals.LogInfo($"Freecam: There are {_players.Count} players");
#endif

        if (_players.Count == 0)
        {
            // Clear out all spectate positions
            DetachCamera(true);

            return;
        }

        // Start spectating a player if we haven't before
        if (_currentPlayer == null && _players[0])
        {
            if (_lastSpectatingPlayer && _players.Contains(_lastSpectatingPlayer))
            {
                SetCurrentPlayer(_lastSpectatingPlayer);
            }
            else
            {
                SetCurrentPlayer(_players[0]);
            }

#if DEBUG
            FikaGlobals.LogInfo($"Freecam: currentPlayer was null, setting to first player {_players[0].Profile.GetCorrectedNickname()}");
#endif
            SwitchSpectateMode();
            return;
        }

        // Cycle through spectate-able players
        var nextIndex = reverse ? _players.IndexOf(_currentPlayer) - 1 : _players.IndexOf(_currentPlayer) + 1;
        if (!reverse)
        {
            if (nextIndex <= _players.Count - 1)
            {
#if DEBUG
                FikaGlobals.LogInfo("Freecam: Setting to next player");
#endif
                SetCurrentPlayer(_players[nextIndex]);
            }
            else
            {
                // hit end of list, loop from start
#if DEBUG
                FikaGlobals.LogInfo("Freecam: Looping back to start player");
#endif
                SetCurrentPlayer(_players[0]);
            }
        }
        else
        {
            if (nextIndex >= 0)
            {
#if DEBUG
                FikaGlobals.LogInfo("Freecam: Setting to previous player");
#endif
                SetCurrentPlayer(_players[nextIndex]);
            }
            else
            {
                // hit beginning of list, loop from end
#if DEBUG
                FikaGlobals.LogInfo("Freecam: Looping back to end player");
#endif
                SetCurrentPlayer(_players[^1]);
            }
        }
        SwitchSpectateMode();
    }

    private void ClearAndAddPlayers()
    {
        _players.Clear();

        var humanPlayers = _coopHandler.HumanPlayers;
        for (var i = 0; i < humanPlayers.Count; i++)
        {
            var player = humanPlayers[i];
            if (!player.IsYourPlayer && player.HealthController.IsAlive)
            {
                _players.Add(player);
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
        if (Input.GetKeyDown(KeyCode.M) && _freeCameraController != null)
        {
            _freeCameraController.DisableAllCullingObjects();
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            TogglePlayerList();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            switch (_cameraState)
            {
                case ECameraState.FollowHeadcam:
                    _cameraState = ECameraState.Follow3rdPerson;
                    break;
                case ECameraState.Follow3rdPerson:
                    _cameraState = ECameraState.FollowHeadcam;
                    break;
            }
            SwitchSpectateMode();
            return;
        }

        if (!_hidePlayerList)
        {
            UpdatePlayerList();
        }

        if (Input.GetKeyDown(_detachKey))
        {
            ClearAndAddPlayers();

            if (_isFollowing)
            {
                DetachCamera(_players.Count == 0);
            }
            else
            {
                _isFollowing = true;
                SwitchSpectateMode();
            }
        }

        if (_isFollowing)
        {
            if (_currentPlayer != null)
            {
                _lastKnownPlayerPosition = _currentPlayer.PlayerBones.Neck.position;
                if (_currentPlayer.MovementContext.LeftStanceEnabled && !_leftMode)
                {
#if DEBUG
                    FikaGlobals.LogInfo("Setting left shoulder mode");
#endif
                    SetLeftShoulderMode(true);
                }
                else if (!_currentPlayer.MovementContext.LeftStanceEnabled && _leftMode)
                {
#if DEBUG
                    FikaGlobals.LogInfo("Unsetting left shoulder mode");
#endif
                    SetLeftShoulderMode(false);
                }
            }
            else
            {
#if DEBUG
                FikaGlobals.LogInfo("Freecam: currentPlayer vanished while we were following, finding next player to attach to");
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

        var fastMode = Input.GetKey(KeyCode.LeftShift);
        var movementSpeed = fastMode ? 20f : 2f;
        var deltaTime = Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftControl))
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

        if (FikaPlugin.Instance.Settings.DroneMode.Value)
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
            var player = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;

            if (player != null && !_coopHandler.ExtractedPlayers.Contains(player.NetId) && player.HealthController.IsAlive)
            {
                player.Teleport(transform.position);
            }
        }

        // Zooming
        const string mouseScrollAxis = "Mouse ScrollWheel";
        var scrollValue = Input.GetAxisRaw(mouseScrollAxis);
        if (scrollValue != 0)
        {
            var currentFov = CameraClass.Instance.Fov;
            if (currentFov >= _minFov && currentFov <= _originalFov)
            {
                var newFov = Mathf.Clamp(currentFov -= (scrollValue * 100), _minFov, _originalFov);
                CameraClass.Instance.SetFov(newFov, 1f);
            }
        }

        const string mouseAxisX = "Mouse X";
        const string mouseAxisY = "Mouse Y";
        var mouseX = Input.GetAxis(mouseAxisX);
        var mouseY = Input.GetAxis(mouseAxisY);

        // update yaw first
        _yaw += mouseX * _lookSensitivity;

        // update and clamp pitch
        _pitch -= mouseY * _lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);

        // apply rotation
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void TogglePlayerList()
    {
        _hidePlayerList = !_hidePlayerList;
        _freecamUI.ListOfPlayers.gameObject.SetActive(_hidePlayerList);
    }

    private Vector3 GetNormalizedVector3(Transform transform)
    {
        var newForward = transform.forward;
        newForward.y = 0f;
        return newForward.normalized;
    }

    private void SetLeftShoulderMode(bool enabled)
    {
        if (enabled)
        {
            // use different coordinates for headcam
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

        // use different coordinates for headcam
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
        SetCameraPosition(_currentPlayer);

        if (_isFollowing)
        {
            _isFollowing = false;
            _leftMode = false;
            transform.parent = null;
        }
    }

    public void AttachToPlayer()
    {
        if (!CheckAndAssignPlayer())
        {
            return;
        }

        CheckAndResetFov();
#if DEBUG
        FikaGlobals.LogInfo($"Freecam: Attaching to helmet cam current player {_currentPlayer.Profile.GetCorrectedNickname()}");
#endif
        transform.SetParent(_currentPlayer.PlayerBones.Head.Original);
        transform.localPosition = new Vector3(-0.1f, -0.07f, -0.17f);
        transform.localEulerAngles = new Vector3(260, 80, 0);
        _isFollowing = true;
        _cameraState = ECameraState.FollowHeadcam;
    }

    /// <summary>
    /// Checks if there is an active player, and if not assigns the last spectated
    /// </summary>
    /// <returns><see langword="true"/> if a player was assigned</returns>
    private bool CheckAndAssignPlayer()
    {
        if (_currentPlayer == null && _lastSpectatingPlayer != null)
        {
            SetCurrentPlayer(_lastSpectatingPlayer);
        }

        return _currentPlayer != null;
    }

    public void AttachToMap()
    {
        if (_lastKnownPlayerPosition != default)
        {
#if DEBUG
            FikaGlobals.LogInfo($"Freecam: Attaching to last tracked player position {_lastKnownPlayerPosition}");
#endif
            transform.position = _lastKnownPlayerPosition;
        }
    }

    public void Attach3rdPerson()
    {
        if (!CheckAndAssignPlayer())
        {
            return;
        }

        CheckAndResetFov();
#if DEBUG
        FikaGlobals.LogInfo($"Freecam: Attaching to 3rd person current player {_currentPlayer.Profile.GetCorrectedNickname()}");
#endif
        transform.SetParent(_currentPlayer.SpectateTransform);
        transform.localPosition = new Vector3(0.3f, 0.2f, -0.65f);
        transform.localEulerAngles = new Vector3(4.3f, 5.9f, 0f);
        _isFollowing = true;
        _cameraState = ECameraState.Follow3rdPerson;
    }

    public void SetActive(bool active, bool extracted = false)
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

            var player = Singleton<GameWorld>.Instance.MainPlayer;
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

            var player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player != null)
            {
                if (player.HealthController.IsAlive)
                {
                    if (!extracted)
                    {
                        SetCameraPosition(player);
                    }

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
                else
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
        }

        IsActive = active;
        _isFollowing = false;
        _leftMode = false;
        transform.parent = null;
    }

    /// <summary>
    /// Sets the camera's position relative to the player
    /// </summary>
    /// <param name="player">The <see cref="Player"/> object containing transform and bone references</param>
    private void SetCameraPosition(Player player)
    {
        // offset camera relative to player
        transform.position = player.Transform.position - (player.Transform.forward * 1.5f) + (player.Transform.up * 2f);
        // look at the head
        transform.LookAt(player.PlayerBones.Head.Original.position, Vector3.up);

        // extract pitch and yaw from rotation
        var euler = transform.eulerAngles;
        _pitch = NormalizeAngle(euler.x);
        _yaw = NormalizeAngle(euler.y);
    }

    /// <summary>
    /// Normalizes an angle to the range [-180, 180] degrees
    /// </summary>
    /// <param name="angle">The angle in degrees to normalize</param>
    /// <returns>The normalized angle within [-180, 180]</returns>
    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
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
        FikaPlayer.OnPlayerSpawned -= OnPlayerSpawned;

        FikaPlugin.Instance.Settings.KeybindOverlay.SettingChanged -= KeybindOverlay_SettingChanged;

        _playersTracker.Clear();
        _players.Clear();

        Destroy(_freecamUI.gameObject);
        Destroy(this);
    }

    private enum ECameraState
    {
        Follow3rdPerson,
        FollowHeadcam
    };
}
