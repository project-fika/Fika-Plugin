using System.Collections.Generic;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.FreeCamera;

public partial class FreeCamera
{
    private bool _hidePlayerList;
    private Texture2D _texWhite;
    private GUIStyle _rowGuiStyle, _badgeGuiStyle, _hpTextGuiStyle, _nameGuiStyle;
    private FikaPlayer _lastSpectatingPlayer;
    private bool _guiCreated;

    ECameraState _cameraState;

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

        _guiCreated = true;
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
        var playerName = string.IsNullOrWhiteSpace(p.Profile.Info.MainProfileNickname)
            ? p.Profile.GetCorrectedNickname()
            : p.Profile.Info.MainProfileNickname;

        var health = p.HealthController.GetBodyPartHealth(EBodyPart.Common);
        var hpCur = health.Current;
        var hpMax = health.Maximum;

        var kind = GetPlayerKind(p);
        var kindColor = GetKindColor(kind);

        const float hpWidth = 100f;
        const float pad = 6f;
        var row = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));

        var nameSize = _nameGuiStyle.CalcSize(new GUIContent(playerName));
        var kindSize = _badgeGuiStyle.CalcSize(new GUIContent(kind));
        var rowWidth = hpWidth + 24f + nameSize.x + kindSize.x;
        Rect rowRect = new(row.x, row.y + 1, rowWidth + 28, row.height);

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

        var x = row.x + pad;
        var y = row.y + 3f;
        var healthPercent = Mathf.Clamp01(hpMax > 0 ? hpCur / hpMax : 0);

        Rect hpRect = new(x, y + 2f, hpWidth, row.height - 8f);
        DrawRect(hpRect, Color.black, _texWhite);
        Rect fill = new(hpRect.x, hpRect.y, hpRect.width * healthPercent, hpRect.height);
        DrawRect(fill, GetHealthBarColor(hpCur, hpMax), _texWhite);
        DrawRectBorder(hpRect, 1f, Color.black, _texWhite);
        GUI.Label(new Rect(x, y + 1f, hpWidth, row.height - 6f), $"{(int)hpCur}/{(int)hpMax}", _hpTextGuiStyle);

        x += hpWidth + 8f;

        DrawLabelBox(new Rect(x, y + 2f, nameSize.x + 12f, row.height - 8f), playerName, kindColor, _nameGuiStyle);
        x += nameSize.x + 20f;

        DrawLabelBox(new Rect(x, y + 2f, kindSize.x + 12f, row.height - 8f), kind, kindColor, _badgeGuiStyle);
    }

    private string GetPlayerKind(FikaPlayer p)
    {
        var role = p.Profile.Info.Settings.Role;
        var kind = role switch
        {
            WildSpawnType.pmcUSEC => "USEC",
            WildSpawnType.pmcBEAR => "BEAR",
            WildSpawnType.assault or WildSpawnType.assaultGroup => "Scav",
            WildSpawnType.pmcBot => "Raider",
            WildSpawnType.exUsec => "Rogue",
            _ when role.IsBoss() => $"Boss ({role})",
            _ when role.IsFollower() => $"Follower ({role})",
            _ => p.Profile.Side.ToString()
        };

        if (kind == "Scav" && !string.IsNullOrEmpty(p.Profile.Info.MainProfileNickname))
        {
            kind = "Player Scav";
        }

        if (kind == "Savage")
        {
            var roleName = p.Profile.Info.Settings?.Role.ToString() ?? "NoRole";
            kind = roleName switch
            {
                "marksman" => "Sniper Scav",
                "shooterBTR" => "BTR Gunner",
                "pmcUSEC" => "AI USEC",
                "pmcBEAR" => "AI BEAR",
                _ => kind
            };
        }

        return kind;
    }

    private Color GetKindColor(string kind)
    {
        return kind switch
        {
            "USEC" => new Color(0.45f, 0.55f, 0.80f, 1f),
            "BEAR" => new Color(0.70f, 0.45f, 0.45f, 1f),
            "AI USEC" => new Color(0.35f, 0.45f, 0.65f, 1f),
            "AI BEAR" => new Color(0.55f, 0.35f, 0.35f, 1f),
            "Scav" => new Color(0.50f, 0.70f, 0.50f, 1f),
            "Player Scav" => new Color(0.55f, 0.80f, 0.80f, 1f),
            "Sniper Scav" => new Color(0.60f, 0.75f, 0.85f, 1f),
            "BTR Gunner" => new Color(0.80f, 0.70f, 0.50f, 1f),
            "Raider" => new Color(0.75f, 0.65f, 0.45f, 1f),
            "Rogue" => new Color(0.65f, 0.55f, 0.75f, 1f),
            _ when kind.StartsWith("Boss") => new Color(0.80f, 0.60f, 0.80f, 1f),
            _ when kind.StartsWith("Follower") => new Color(0.85f, 0.70f, 0.55f, 1f),
            _ => new Color(0.55f, 0.55f, 0.55f, 1f)
        };
    }

    private Color GetHealthBarColor(float current, float max)
    {
        var percent = Mathf.Clamp01(max > 0 ? current / max : 0);
        float r, g;

        if (percent > 0.5f)
        {
            r = (1f - percent) / 0.5f * 0.75f;
            g = 0.5f;
        }
        else
        {
            r = 0.75f;
            g = percent;
        }

        return new Color(r, g, 0f, 1f);
    }

    private void DrawLabelBox(Rect rect, string text, Color bgColor, GUIStyle style)
    {
        DrawRect(rect, bgColor, _texWhite);
        DrawRectBorder(rect, 1f, Color.black, _texWhite);
        var prevBg = style.normal.background;
        style.normal.background = null;
        GUI.Label(rect, text, style);
        style.normal.background = prevBg;
    }


    private void DrawPlayerList()
    {
        List<FikaPlayer> players = [];
        var humanPlayers = _coopHandler.HumanPlayers;
        for (var i = 0; i < humanPlayers.Count; i++)
        {
            var player = humanPlayers[i];
            if (!player.IsYourPlayer && player.HealthController.IsAlive)
            {
                players.Add(player);
            }
        }
        // If no alive players, add bots to spectate pool if enabled
#if DEBUG
        if (FikaPlugin.Instance.Settings.AllowSpectateBots.Value)
#else

        if (players.Count == 0 && FikaPlugin.Instance.Settings.AllowSpectateBots.Value)
#endif
        {
            if (FikaBackendUtils.IsServer)
            {
                foreach (var player in _coopHandler.Players.Values)
                {
                    if (player.IsAI && player.HealthController.IsAlive)
                    {
                        players.Add(player);
                    }
                }
            }
            else
            {
                foreach (var player in _coopHandler.Players.Values)
                {
                    if (player.IsObservedAI && player.HealthController.IsAlive)
                    {
                        players.Add(player);
                    }
                }
            }
        }

        if (players == null || players.Count == 0)
        {
            return;
        }

        for (var i = 0; i < players.Count; i++)
        {
            DrawPlayerRow(players[i]);
        }
    }

    protected void OnGUI()
    {
        if (!IsActive || !_showOverlay || _hidePlayerList || !FikaPlugin.Instance.Settings.ShowPlayerList.Value)
        {
            return;
        }

        if (!_guiCreated)
        {
            InitPlayerListGuiStyles();
        }

        const float verticalOffset = 360f;
        GUILayout.BeginArea(new Rect(5f, 5f + verticalOffset, 500f, Screen.height - 10f - verticalOffset));
        GUILayout.BeginVertical();

        DrawPlayerList();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private enum ECameraState
    {
        Follow3rdPerson,
        FollowHeadcam
    };
}
