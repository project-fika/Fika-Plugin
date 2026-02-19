using DG.Tweening;
using EFT;
using Fika.Core;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using TMPro;
using UnityEngine.UI;

public class ListPlayer : MonoBehaviour
{
#pragma warning disable CS0649
    [SerializeField]
    private Image Background;

    [SerializeField]
    private TMP_Text HPText;

    [SerializeField]
    private TMP_Text NameText;

    [SerializeField]
    private TMP_Text FactionText;

    [SerializeField]
    private Image HPBackground;

    [SerializeField]
    private Image NameBackground;

    [SerializeField]
    private Image FactionBackground;
#pragma warning restore CS0649

    private const float _tweenLength = 0.25f;

    private FikaPlayer _player;
    private int _lastHealth;

    /// <summary>
    /// Updates values
    /// </summary>
    /// <returns><see langword="true"/> if the entry should be remove, <see langword="false"/> otherwise</returns>
    public bool ManualUpdate()
    {
        if (_player == null || !_player.HealthController.IsAlive)
        {
            Destroy(gameObject);
            return true;
        }

        var health = _player.HealthController.GetBodyPartHealth(EBodyPart.Common, true);
        var current = Mathf.RoundToInt(health.Current);
        if (_lastHealth != current)
        {
            _lastHealth = current;
            UpdateHealth(current,
                Mathf.RoundToInt(health.Maximum));
        }

        return false;
    }

    public void Init(FikaPlayer player)
    {
        NameText.text = player.Profile.GetCorrectedNickname();
        NameText.ForceMeshUpdate(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(NameBackground.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(NameBackground.rectTransform.parent.RectTransform());

        FactionText.text = GetPlayerRole(player);
        FactionText.ForceMeshUpdate(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(FactionBackground.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(FactionBackground.rectTransform.parent.RectTransform());

        _player = player;

        _lastHealth = -1;
        ManualUpdate();
        FactionBackground.color = GetRoleColor(player);

        _lastHealth = Mathf.RoundToInt(player.HealthController.GetBodyPartHealth(EBodyPart.Common, true).Current);
    }

    private static string GetPlayerRole(FikaPlayer player)
    {
        if ((!FikaBackendUtils.IsServer && !player.IsObservedAI) || (FikaBackendUtils.IsServer && !player.IsAI))
        {
            return $"[P] ({player.Profile.Info.Side})";
        }

        var role = player.Profile.Info.Settings.Role;
        var playerRole = role switch
        {
            WildSpawnType.pmcUSEC => "USEC",
            WildSpawnType.pmcBEAR => "BEAR",
            WildSpawnType.assault or WildSpawnType.assaultGroup => "Scav",
            WildSpawnType.pmcBot => "Raider",
            WildSpawnType.exUsec => "Rogue",
            _ when role.IsBoss() => $"Boss ({role})",
            _ when role.IsFollower() => $"Follower ({role})",
            _ => player.Profile.Side.ToString()
        };

        if (role is WildSpawnType.assault or WildSpawnType.assaultGroup)
        {
            playerRole = role switch
            {
                WildSpawnType.marksman => "Sniper Scav",
                WildSpawnType.shooterBTR => "BTR Gunner",
                WildSpawnType.pmcUSEC => "AI USEC",
                WildSpawnType.pmcBEAR => "AI BEAR",
                _ => playerRole
            };
        }

        return playerRole;
    }

    private static Color GetRoleColor(FikaPlayer player)
    {
        if ((!FikaBackendUtils.IsServer && !player.IsObservedAI) || !player.IsAI)
        {
            switch (player.Profile.Info.Side)
            {
                case EPlayerSide.Usec:
                    return new Color(0.290f, 0.565f, 0.886f, 1f);
                case EPlayerSide.Bear:
                    return new Color(0.886f, 0.290f, 0.290f, 1f);
                case EPlayerSide.Savage:
                    return new Color(0.886f, 0.722f, 0.290f, 1f);
            }
        }

        var role = player.Profile.Info.Settings.Role;
        var isPmc = role.IsPmcBot();

        if (!isPmc && role.IsBoss())
        {
            return new Color(0.80f, 0.60f, 0.80f);
        }

        if (!isPmc && role.IsFollower())
        {
            return new Color(0.85f, 0.70f, 0.55f);
        }

        switch (role)
        {
            case WildSpawnType.pmcUSEC:
                return new Color(0.290f, 0.565f, 0.886f, 1f);
            case WildSpawnType.pmcBEAR:
                return new Color(0.886f, 0.290f, 0.290f, 1f);
            case WildSpawnType.assault:
            case WildSpawnType.assaultGroup:
                return new Color(0.886f, 0.722f, 0.290f, 1f);
            case WildSpawnType.pmcBot:
                return new Color(0.75f, 0.65f, 0.45f);
            case WildSpawnType.exUsec:
                return new Color(0.65f, 0.55f, 0.75f);
            case WildSpawnType.marksman:
                return new Color(0.60f, 0.75f, 0.85f);
            case WildSpawnType.shooterBTR:
                return new Color(0.80f, 0.70f, 0.50f);
            default:
                return new Color(0.55f, 0.55f, 0.55f);
        }
    }

    public void ToggleBackground(bool enabled)
    {
        Background.color = enabled ? new Color(0.4f, 0.4f, 0.4f, 0.75f) : Color.clear;
    }

    private void UpdateHealth(int currentHealth, int maxHealth)
    {
        HPText.SetText("{0}/{1}",
            currentHealth, maxHealth);

        var normalizedHealth = Mathf.Clamp01((float)currentHealth / maxHealth);
        HPBackground.DOFillAmount(normalizedHealth, _tweenLength);
        HPBackground.color = Color.Lerp(FikaPlugin.Instance.Settings.LowHealthColor.Value,
            FikaPlugin.Instance.Settings.FullHealthColor.Value, normalizedHealth);
    }
}
