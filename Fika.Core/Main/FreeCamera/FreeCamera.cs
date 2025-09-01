using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using System;
using System.Collections.Generic;

namespace Fika.Core.Main.FreeCamera;

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
    private FikaPlayer _currentPlayer;
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

    private bool _hidePlayerList;
    private bool _initPlayerListGuiStyles;
    private Texture2D _texWhite;
    private GUIStyle _rowGuiStyle, _badgeGuiStyle, _hpTextGuiStyle, _nameGuiStyle;
    private FikaPlayer _lastSpectatingPlayer;
    private bool _superFastMode;

    enum CameraState
    {
        Follow3rdPerson,
        FollowHeadcam
    };
    CameraState _cameraState;

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
        _hidePlayerList = false;
    }

    private void KeybindOverlay_SettingChanged(object sender, EventArgs e)
    {
        _showOverlay = FikaPlugin.KeybindOverlay.Value;
        if (IsActive)
        {
            _freecamUI.gameObject.SetActive(_showOverlay);
        }
    }

    public void SetCurrentPlayer(FikaPlayer player)
    {
        _currentPlayer = player;
#if DEBUG
        FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Setting player to {_currentPlayer}");
#endif
    }

    public void DetachCamera()
    {
        if (_currentPlayer)
        {
            _lastSpectatingPlayer = _currentPlayer;
        }

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
        if (!_isFollowing)
        {
            if (FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator)
            {
                JumpToPlayer();
                return;
            }
        }

        switch (_cameraState)
        {
            case CameraState.FollowHeadcam:
                AttachToPlayer();
                break;
            case CameraState.Follow3rdPerson:
                Attach3rdPerson();
                break;
        }
    }

    private void InitPlayerListGuiStyles()
    {
        if (_texWhite == null)
        {
            _texWhite = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _texWhite.SetPixel(0, 0, Color.white);
            _texWhite.Apply();
        }

        _rowGuiStyle = new GUIStyle(GUI.skin.label) { padding = new RectOffset(6, 6, 4, 4) };

        _badgeGuiStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(6, 6, 1, 1),
            margin = new RectOffset(4, 6, 0, 0),
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(0, 0, 0, 0),
            normal = { textColor = Color.black }
        };

        _hpTextGuiStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            normal = { textColor = Color.white }
        };

        _nameGuiStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(6, 6, 1, 1),
            margin = new RectOffset(4, 6, 0, 0),
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(0, 0, 0, 0),
            normal = { textColor = Color.black }
        };
    }

    private static void DrawRect(Rect r, Color c, Texture2D white)
    {
        var prev = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = prev;
    }

    private static void DrawRectBorder(Rect r, float border, Color color, Texture2D white)
    {
        DrawRect(new Rect(r.x, r.y, r.width, border), color, white);
        DrawRect(new Rect(r.x, r.yMax - border, r.width, border), color, white);
        DrawRect(new Rect(r.x, r.y, border, r.height), color, white);
        DrawRect(new Rect(r.xMax - border, r.y, border, r.height), color, white);
    }

    private void DrawPlayerRow(FikaPlayer p)
    {
        string playerName = p.Profile.Info.MainProfileNickname;
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = p.Profile.GetCorrectedNickname();

        var common = p.HealthController.GetBodyPartHealth(EBodyPart.Common);
        float hpCur = common.Current;
        float hpMax = common.Maximum;

        var role = p.Profile.Info.Settings.Role;
        string kind =
            role == WildSpawnType.pmcUSEC ? "USEC" :
            role == WildSpawnType.pmcBEAR ? "BEAR" :
            role == WildSpawnType.assault || role == WildSpawnType.assaultGroup ? "Scav" :
            role == WildSpawnType.pmcBot ? "Raider" :
            role == WildSpawnType.exUsec ? "Rogue" :
            role.ToString().StartsWith("boss", StringComparison.OrdinalIgnoreCase) ? $"Boss ({role})" :
            role.ToString().StartsWith("follower", StringComparison.OrdinalIgnoreCase) ? $"Follower ({role})" :
            p.Profile.Side.ToString();

        if (kind == "Scav" && !string.IsNullOrEmpty(p.Profile.Info.MainProfileNickname))
            kind = "Player Scav";

        if (kind == "Savage")
        {
            string r = p.Profile.Info.Settings != null ? p.Profile.Info.Settings.Role.ToString() : "NoRole";
            if (r == "marksman") kind = "Sniper Scav";
            else if (r == "shooterBTR") kind = "BTR Gunner";
            else if (r == "pmcUSEC") kind = "AI USEC";
            else if (r == "pmcBEAR") kind = "AI BEAR";
        }

        Color kindColor =
            kind == "USEC" ? new Color(0.45f, 0.55f, 0.80f, 1f) :
            kind == "BEAR" ? new Color(0.70f, 0.45f, 0.45f, 1f) :
            kind == "AI USEC" ? new Color(0.35f, 0.45f, 0.65f, 1f) :
            kind == "AI BEAR" ? new Color(0.55f, 0.35f, 0.35f, 1f) :
            kind == "Scav" ? new Color(0.50f, 0.70f, 0.50f, 1f) :
            kind == "Player Scav" ? new Color(0.55f, 0.80f, 0.80f, 1f) :
            kind == "Sniper Scav" ? new Color(0.60f, 0.75f, 0.85f, 1f) :
            kind == "BTR Gunner" ? new Color(0.80f, 0.70f, 0.50f, 1f) :
            kind == "Raider" ? new Color(0.75f, 0.65f, 0.45f, 1f) :
            kind == "Rogue" ? new Color(0.65f, 0.55f, 0.75f, 1f) :
            kind.StartsWith("Boss") ? new Color(0.80f, 0.60f, 0.80f, 1f) :
            kind.StartsWith("Follower") ? new Color(0.85f, 0.70f, 0.55f, 1f) :
            new Color(0.55f, 0.55f, 0.55f, 1f);

        const float hpWidth = 100f;
        Vector2 nameSize = _nameGuiStyle.CalcSize(new GUIContent(playerName));
        Vector2 kindSize = _badgeGuiStyle.CalcSize(new GUIContent(kind));

        const float pad = 6f;
        Rect row = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        float x = row.x + pad;
        float y = row.y + 3f;

        float rowWidth = hpWidth + 24f + nameSize.x + kindSize.x;
        var rowRect = new Rect(row.x, row.y + 1, rowWidth + 28, row.height);

        if (p == _currentPlayer)
        {
            DrawRect(rowRect, new Color(0.5f, 0.5f, 0.5f, 0.75f), _texWhite);
            DrawRectBorder(rowRect, 1f, Color.black, _texWhite);
        }
        else if (_currentPlayer == null && p == _lastSpectatingPlayer)
        {
            DrawRect(rowRect, new Color(0.5f, 0.5f, 0.5f, 0.5f), _texWhite);
            DrawRectBorder(rowRect, 1f, Color.grey, _texWhite);
        }

        var hpRect = new Rect(x, y + 2f, hpWidth, row.height - 8f);
        DrawRect(hpRect, Color.black, _texWhite);
        float healthPercent = Mathf.Clamp01(hpMax > 0 ? hpCur / hpMax : 0);

        float healthRedValue = 0.0f;
        float healthGreenValue = 0.0f;
        if (healthPercent > 0.5f)
        {
            // Start at red=0 at 100% health, end at red=.75 at 50% health
            healthRedValue = (1.0f - healthPercent) / 0.5f * 0.75f;
            healthGreenValue = 0.5f;
        }
        else
        {
            healthRedValue = 0.75f;
            // Start at green=0.5 at 50% health, end at green=0 at 0% health
            healthGreenValue = healthPercent;
        }
        var healthBarColor = new Color(healthRedValue, healthGreenValue, 0.0f, 1f);

        var fill = new Rect(hpRect.x, hpRect.y, hpRect.width * healthPercent, hpRect.height);
        DrawRect(fill, healthBarColor, _texWhite);
        DrawRectBorder(hpRect, 1f, Color.black, _texWhite);
        var hpLabelRect = new Rect(x, y + 1f, hpWidth, row.height - 6f);
        GUI.Label(hpLabelRect, $"{(int)hpCur}/{(int)hpMax}", _hpTextGuiStyle);
        x += hpWidth + 8f;

        var nameRect = new Rect(x, y + 2f, nameSize.x + 12f, row.height - 8f);
        DrawRect(nameRect, kindColor, _texWhite);
        DrawRectBorder(nameRect, 1f, Color.black, _texWhite);
        var prevNameBg = _nameGuiStyle.normal.background; _nameGuiStyle.normal.background = null;
        GUI.Label(nameRect, playerName, _nameGuiStyle);
        _nameGuiStyle.normal.background = prevNameBg;
        x += nameRect.width + 8f;

        var kindRect = new Rect(x, y + 2f, kindSize.x + 12f, row.height - 8f);
        DrawRect(kindRect, kindColor, _texWhite);
        DrawRectBorder(kindRect, 1f, Color.black, _texWhite);
        var prevBadgeBg = _badgeGuiStyle.normal.background; _badgeGuiStyle.normal.background = null;
        GUI.Label(kindRect, kind, _badgeGuiStyle);
        _badgeGuiStyle.normal.background = prevBadgeBg;
    }

    private void DrawPlayerList()
    {
        if (!_initPlayerListGuiStyles)
        {
            InitPlayerListGuiStyles();
            _initPlayerListGuiStyles = true;
        }

        if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
        {
            return;
        }

        List<FikaPlayer> players = [];
        List<FikaPlayer> humanPlayers = coopHandler.HumanPlayers;
        for (int i = 0; i < humanPlayers.Count; i++)
        {
            FikaPlayer player = humanPlayers[i];
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
                foreach (FikaPlayer player in coopHandler.Players.Values)
                {
                    if (player.IsAI && player.HealthController.IsAlive)
                    {
                        players.Add(player);
                    }
                }
            }
            else
            {
                foreach (FikaPlayer player in coopHandler.Players.Values)
                {
                    if (player.IsObservedAI && player.HealthController.IsAlive)
                    {
                        players.Add(player);
                    }
                }
            }
        }

        if (players == null || players.Count == 0) return;

        for (int i = 0; i < players.Count; i++)
        {
            DrawPlayerRow(players[i]);
        }
    }

    protected void OnGUI()
    {
        if (!IsActive || !_showOverlay || _hidePlayerList)
        {
            return;
        }

        const float verticalOffset = 360f;
        GUILayout.BeginArea(new Rect(5f, 5f + verticalOffset, 500f, Screen.height - 10f - verticalOffset));
        GUILayout.BeginVertical();

        DrawPlayerList();

        GUILayout.EndVertical();
        GUILayout.EndArea();
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

        List<FikaPlayer> players = [];
        List<FikaPlayer> humanPlayers = coopHandler.HumanPlayers;
        for (int i = 0; i < humanPlayers.Count; i++)
        {
            FikaPlayer player = humanPlayers[i];
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
                foreach (FikaPlayer player in coopHandler.Players.Values)
                {
                    if (player.IsAI && player.HealthController.IsAlive)
                    {
                        players.Add(player);
                    }
                }
            }
            else
            {
                foreach (FikaPlayer player in coopHandler.Players.Values)
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
            if (_lastSpectatingPlayer && players.Contains(_lastSpectatingPlayer))
            {
                _currentPlayer = _lastSpectatingPlayer;
            }
            else
            {
                _currentPlayer = players[0];
            }

#if DEBUG
            FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: currentPlayer was null, setting to first player {players[0].Profile.GetCorrectedNickname()}");
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
            }
            else
            {
                // hit end of list, loop from start
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Looping back to start player");
#endif
                _currentPlayer = players[0];
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
            }
            else
            {
                // hit beginning of list, loop from end
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo("Freecam: Looping back to end player");
#endif
                _currentPlayer = players[players.Count - 1];
            }
        }
        SwitchSpectateMode();
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

        if (Input.GetKeyDown(KeyCode.L))
        {
            _hidePlayerList = !_hidePlayerList;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            switch (_cameraState)
            {
                case CameraState.FollowHeadcam:
                    _cameraState = CameraState.Follow3rdPerson;
                    break;
                case CameraState.Follow3rdPerson:
                    _cameraState = CameraState.FollowHeadcam;
                    break;
            }
            SwitchSpectateMode();
            return;
        }

        if (Input.GetKeyDown(_detachKey))
        {
            if (_isFollowing)
            {
                if (_isSpectatingBots || FikaPlugin.Instance.AllowSpectateFreeCam || _isSpectator)
                {
                    DetachCamera();
                }
            }
            else
            {
                _isFollowing = true;
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
        if (Input.GetKeyDown(KeyCode.LeftControl)) _superFastMode = !_superFastMode;
        float movementSpeed = fastMode ? 20f : 2f;
        float deltaTime = Time.deltaTime;

        if (_superFastMode)
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

            FikaPlayer player = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;

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

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // update yaw first
        _yaw += mouseX * _lookSensitivity;

        // update and clamp pitch
        _pitch -= mouseY * _lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);

        // apply rotation
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
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
        SetCameraPosition(_currentPlayer.CameraPosition);

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
        FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to helmet cam current player {_currentPlayer.Profile.GetCorrectedNickname()}");
#endif
        transform.SetParent(_currentPlayer.PlayerBones.Head.Original);
        transform.localPosition = new Vector3(-0.1f, -0.07f, -0.17f);
        transform.localEulerAngles = new Vector3(260, 80, 0);
        _isFollowing = true;
        _cameraState = CameraState.FollowHeadcam;
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
        FikaPlugin.Instance.FikaLogger.LogInfo($"Freecam: Attaching to 3rd person current player {_currentPlayer.Profile.GetCorrectedNickname()}");
#endif
        if (!_currentPlayer.IsAI)
        {
            transform.SetParent(_currentPlayer.SpectateTransform);
            transform.localPosition = new Vector3(2.0f, 1.5f, -1.5f);
            transform.localEulerAngles = new Vector3(4.3f, 25.0f, -30f);
        }
        else
        {
            transform.SetParent(_currentPlayer.PlayerBones.Head.Original);
            transform.localPosition = new Vector3(0f, -1.5f, -1.5f);
            transform.localEulerAngles = new Vector3(-115f, 125f, -30f);
        }
        _isFollowing = true;
        _cameraState = CameraState.Follow3rdPerson;
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
            if (player != null)
            {
                if (player.HealthController.IsAlive)
                {
                    if (!extracted)
                    {
                        SetCameraPosition(player.CameraPosition);
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

    private void SetCameraPosition(Transform target)
    {
        // set camera position and rotation based on target
        transform.rotation = target.rotation;
        transform.position = target.position - target.forward * 1f + target.up * 0.1f;

        // extract pitch and yaw from current camera rotation
        Vector3 euler = transform.eulerAngles;
        _pitch = -NormalizeAngle(euler.x);
        _yaw = NormalizeAngle(euler.y);

        // reapply adjusted rotation
        transform.rotation = Quaternion.Euler(-_pitch, _yaw, 0f);
    }

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
        FikaPlugin.KeybindOverlay.SettingChanged -= KeybindOverlay_SettingChanged;
        Destroy(_freecamUI.gameObject);
        Destroy(this);
    }
}
