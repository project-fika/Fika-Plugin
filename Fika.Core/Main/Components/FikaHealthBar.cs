// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using Comfort.Common;
using DG.Tweening;
using EFT;
using EFT.HealthSystem;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using TMPro;
using UnityEngine.UI;

namespace Fika.Core.Main.Components;

/// <summary>
/// Displays a health bar over another player <br/>
/// Created by: ssh_
/// </summary>
public sealed class FikaHealthBar : MonoBehaviour
{
    /// <summary>
    /// Check for GClass increments, can be checked in <see cref="StaticIcons.EffectSprites"/> method <see cref="ISerializationCallbackReceiver.OnAfterDeserialize"/> <br/><br/>
    /// <see cref="ActiveHealthController.Wound"/>, <see cref="ActiveHealthController.Encumbered"/>, <see cref="ActiveHealthController.OverEncumbered"/>, <br/>
    /// <see cref="ActiveHealthController.MildMusclePain"/>, <see cref="ActiveHealthController.SevereMusclePain"/>
    /// </summary>
    private static readonly Type[] _ignoredTypes = [typeof(GInterface362), typeof(GInterface364), typeof(GInterface365), typeof(GInterface379), typeof(GInterface380)];

    private const float _tweenLength = 0.25f;

    private ObservedPlayer _currentPlayer;
    private Camera _camera;
    private FikaPlayer _mainPlayer;
    private PlayerPlateUI _playerPlate;
    private Dictionary<Type, Sprite> _effectIcons;
    private List<HealthBarEffect> _effects;
    private float _counter;
    private bool _updatePos = true;
    private RectTransform _canvasRect;
    private float _lastFinalAlpha = -1f;
    private RectTransform _plateRectTransform;
    private CanvasGroup _alphaGroup;
    private Transform _neckBone;
    private Dictionary<EBodyPart, GameObject> _bodyParts;
    private int _destroyedLimbs;
    private bool _showLimbs;

    private bool _downed;
    private bool _updateDowned;
    private float _downedTimer;

    private static readonly int _checkLayers = LayerMask.GetMask(["HighPolyCollider", "Terrain", "Player"]);
    private static readonly int _playerLayer = LayerMask.NameToLayer("Player");

    public static FikaHealthBar Create(ObservedPlayer player)
    {
        if (player == null)
        {
            FikaGlobals.LogError("FikaHealthBar::Create: Player was null!");
            return null;
        }

        var healthBar = player.gameObject.AddComponent<FikaHealthBar>();
        healthBar._currentPlayer = player;
        healthBar._mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        healthBar._effectIcons = EFTHardSettings.Instance.StaticIcons.EffectIcons.EffectIcons;
        healthBar._effects = [];
        healthBar._neckBone = player.PlayerBones.Neck;

        healthBar.CreateHealthBar();
        return healthBar;
    }

    public void ClearEffects()
    {
        foreach (var effect in _effects)
        {
            effect.Remove();
        }
        _effects.Clear();
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        if (_currentPlayer != null)
        {
            UpdateScreenSpacePosition();
            if (_updateDowned && _downed)
            {
                UpdateDowned(deltaTime);
            }
            if (FikaPlugin.Instance.Settings.UseOcclusion.Value)
            {
                _counter += deltaTime;
                if (_counter > 1f)
                {
                    _counter = 0f;
                    CheckForOcclusion();
                }
            }
        }
        else
        {
            Destroy(this);
        }
    }

    private void UpdateDowned(float deltaTime)
    {
        _downedTimer -= deltaTime;
        _playerPlate.downedStateTimerScreen.SetText("{0:1}", _downedTimer);

        if (_downedTimer <= 0f)
        {
            _updateDowned = false;
            _playerPlate.downedStateBackgroundScreen.gameObject.SetActive(false);
            _playerPlate.downedStateScreen.gameObject.SetActive(false);
            _playerPlate.downedStateTimerScreen.gameObject.SetActive(false);
        }
    }

    public void ToggleRevive(bool reviving, string nickname)
    {
        if (reviving)
        {
            _playerPlate.downedStateTimerScreen.gameObject.SetActive(false);
            _playerPlate.downedStateScreen.SetText(string.Format(LocaleUtils.UI_REVIVING_BEING_REVIVED_BY.Localized(), nickname));
            _updateDowned = false;
            return;
        }

        _playerPlate.downedStateTimerScreen.gameObject.SetActive(true);
        _playerPlate.downedStateScreen.SetText(LocaleUtils.UI_REVIVING_DOWNED.Localized());
        _updateDowned = true;
    }

    private void CheckForOcclusion()
    {
        var camPos = _camera.transform.position;
        var targetPos = _currentPlayer.PlayerBones.Neck.position;

        if (Physics.Raycast(camPos, targetPos - camPos, out var hitinfo, 800f, _checkLayers))
        {
            if (hitinfo.collider.gameObject.layer != _playerLayer)
            {
                _playerPlate.ScalarObjectScreen.SetActive(false);
                _updatePos = false;
            }
            else
            {
                if (!_playerPlate.ScalarObjectScreen.activeSelf)
                {
                    _playerPlate.ScalarObjectScreen.SetActive(true);
                    _updatePos = true;
                    UpdateScreenSpacePosition();
                }
            }
        }
    }

    private void UpdateScreenSpacePosition()
    {
        if (!_updatePos)
        {
            return;
        }

        var settings = FikaPlugin.Instance.Settings;
        var proceduralAnimation = _mainPlayer.ProceduralWeaponAnimation;

        var opacityMultiplier = 1f;
        if (_mainPlayer.HealthController.IsAlive && proceduralAnimation.IsAiming)
        {
            if (proceduralAnimation.CurrentScope.IsOptic && settings.HideNamePlateInOptic.Value)
            {
                if (_playerPlate.ScalarObjectScreen.activeSelf)
                {
                    _playerPlate.ScalarObjectScreen.SetActive(false);
                }

                return;
            }
            opacityMultiplier = settings.OpacityInADS.Value;
        }

        var cameraPos = _camera.transform.position;
        var offset = _currentPlayer.Position - cameraPos;
        var sqrDist = offset.sqrMagnitude;
        var maxDist = settings.MaxDistanceToShow.Value;

        if (sqrDist > maxDist * maxDist)
        {
            if (_playerPlate.ScalarObjectScreen.activeSelf)
            {
                _playerPlate.ScalarObjectScreen.SetActive(false);
            }

            return;
        }

        if (!_playerPlate.ScalarObjectScreen.activeSelf)
        {
            _playerPlate.ScalarObjectScreen.SetActive(true);
        }

        var targetPosition = _neckBone.position + Vector3.up;
        if (!WorldToScreen.ProjectToCanvas(targetPosition, _mainPlayer,
            _canvasRect, out var canvasPos, settings.NamePlateUseOpticZoom.Value, false))
        {
            _alphaGroup.alpha = 0f;
            return;
        }

        _plateRectTransform.anchoredPosition = canvasPos;

        var distance = Mathf.Sqrt(sqrDist);
        var t = Mathf.InverseLerp(2f, maxDist, distance);

        var distFromCenterMult = 1f;
        if (settings.DecreaseOpacityNotLookingAt.Value)
        {
            var sqrDistFromCenter = canvasPos.sqrMagnitude;
            var maxSqrDist = Mathf.Pow(Mathf.Min(_canvasRect.sizeDelta.x, _canvasRect.sizeDelta.y) * 0.5f, 2f);
            distFromCenterMult = Mathf.Clamp01(1f - (sqrDistFromCenter / maxSqrDist));
        }

        var distanceAlpha = Mathf.Lerp(1f, 0.3f, t);

        var finalAlpha = Mathf.Max(settings.MinimumOpacity.Value, distanceAlpha * opacityMultiplier * distFromCenterMult);

        var healthVisibleMultiplier = settings.HideHealthBar.Value ? 0f : 1f;
        var healthAlpha = Mathf.Max(finalAlpha * healthVisibleMultiplier, 0.15f * healthVisibleMultiplier);

        if (!Mathf.Approximately(_lastFinalAlpha, finalAlpha))
        {
            _lastFinalAlpha = finalAlpha;

            _alphaGroup.alpha = finalAlpha;

            var scaleMultiplier = Mathf.Lerp(0.48f, 0.075f, t * Mathf.Sqrt(t)) * settings.NamePlateScale.Value;
            _plateRectTransform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);

            foreach (var effect in _effects)
            {
                var showAmount = effect.Amount > 1;
                if (effect.TMPText.gameObject.activeSelf != showAmount)
                {
                    effect.TMPText.gameObject.SetActive(showAmount);
                }
            }
        }
    }

    private void CreateHealthBar()
    {
        var uiPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.PlayerUI);
        var uiGameObj = Instantiate(uiPrefab);
        _playerPlate = uiGameObj.GetComponent<PlayerPlateUI>();
        _alphaGroup = _playerPlate.AlphaGroup;
        _plateRectTransform = _playerPlate.ScalarObjectScreen.GetComponent<RectTransform>();
        _playerPlate.SetNameText(_currentPlayer.Profile.Info.MainProfileNickname);
        _camera = CameraClass.Instance.Camera;
        if (FikaPlugin.DevelopersList.ContainsKey(_currentPlayer.Profile.Nickname.ToLower()))
        {
            _playerPlate.playerNameScreen.color = new Color(0, 6f, 1, 1);
            var specialIcons = Resources.Load<ChatSpecialIconSettings>("ChatSpecialIconSettings");
            _playerPlate.bearPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[1].IconSprite;
            _playerPlate.bearPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
            _playerPlate.usecPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[1].IconSprite;
            _playerPlate.usecPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
        }
        else if (FikaPlugin.RespectedPlayersList.ContainsKey(_currentPlayer.Profile.Nickname.ToLower()))
        {
            _playerPlate.playerNameScreen.color = new Color(1, 0.6f, 0, 1);
            var specialIcons = Resources.Load<ChatSpecialIconSettings>("ChatSpecialIconSettings");
            _playerPlate.bearPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[2].IconSprite;
            _playerPlate.bearPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
            _playerPlate.usecPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[2].IconSprite;
            _playerPlate.usecPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
        }
        else
        {
            _playerPlate.playerNameScreen.color = FikaPlugin.Instance.Settings.NamePlateTextColor.Value;
        }
        // Start the plates both disabled, the visibility will be set in the update loop
        _playerPlate.usecPlateScreen.gameObject.SetActive(false);
        _playerPlate.bearPlateScreen.gameObject.SetActive(false);

        SetPlayerPlateFactionVisibility(FikaPlugin.Instance.Settings.UsePlateFactionSide.Value);
        SetPlayerPlateHealthVisibility(FikaPlugin.Instance.Settings.HideHealthBar.Value);

        if (FikaPlugin.Instance.Settings.ShowEffects.Value)
        {
            _currentPlayer.HealthController.EffectAddedEvent += HealthController_EffectAddedEvent;
            _currentPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
            AddAllActiveEffects();
        }

        FikaPlugin.Instance.Settings.UsePlateFactionSide.SettingChanged += UsePlateFactionSide_SettingChanged;
        FikaPlugin.Instance.Settings.HideHealthBar.SettingChanged += HideHealthBar_SettingChanged;
        FikaPlugin.Instance.Settings.UseNamePlates.SettingChanged += UseNamePlates_SettingChanged;
        FikaPlugin.Instance.Settings.UseHealthNumber.SettingChanged += UseHealthNumber_SettingChanged;
        FikaPlugin.Instance.Settings.ShowEffects.SettingChanged += ShowEffects_SettingChanged;
        FikaPlugin.Instance.Settings.ShowBrokenLimbs.SettingChanged += ShowBrokenLimbs_SettingChanged;

        ToggleHealthControllerEvents(!FikaPlugin.Instance.Settings.HideHealthBar.Value);

        _playerPlate.SetHealthNumberText(100);

        _canvasRect = _playerPlate.ScalarObjectScreen.transform.parent.RectTransform();

        _showLimbs = FikaPlugin.Instance.Settings.ShowBrokenLimbs.Value;
        _bodyParts = new Dictionary<EBodyPart, GameObject>()
        {
            [EBodyPart.Common] = _playerPlate.Skeleton,
            [EBodyPart.Head] = _playerPlate.Head,
            [EBodyPart.LeftArm] = _playerPlate.LeftArm,
            [EBodyPart.RightArm] = _playerPlate.RightArm,
            [EBodyPart.LeftLeg] = _playerPlate.LeftLeg,
            [EBodyPart.RightLeg] = _playerPlate.RightLeg,
            [EBodyPart.Chest] = _playerPlate.Chest,
            [EBodyPart.Stomach] = _playerPlate.Stomach
        };
        _bodyParts[EBodyPart.Common].SetActive(false);

        UpdateHealth();
        ToggleNamePlate();
    }

    private void ToggleHealthControllerEvents(bool enabled)
    {
        var healthController = _currentPlayer?.HealthController;
        if (healthController == null)
        {
            return;
        }

        if (enabled)
        {
            healthController.HealthChangedEvent += HealthController_HealthChangedEvent;
            healthController.BodyPartDestroyedEvent += HealthController_BodyPartDestroyedEvent;
            healthController.BodyPartRestoredEvent += HealthController_BodyPartRestoredEvent;
            healthController.DiedEvent += HealthController_DiedEvent;
        }
        else
        {
            healthController.HealthChangedEvent -= HealthController_HealthChangedEvent;
            healthController.BodyPartDestroyedEvent -= HealthController_BodyPartDestroyedEvent;
            healthController.BodyPartRestoredEvent -= HealthController_BodyPartRestoredEvent;
            healthController.DiedEvent -= HealthController_DiedEvent;
        }
    }

    private void ToggleNamePlate()
    {
        var useNamePlates = FikaPlugin.Instance.Settings.UseNamePlates.Value;
        _playerPlate.gameObject.SetActive(useNamePlates);
        enabled = useNamePlates;
    }

    #region events
    private void ShowBrokenLimbs_SettingChanged(object sender, EventArgs e)
    {
        _showLimbs = FikaPlugin.Instance.Settings.ShowBrokenLimbs.Value;
        RefreshLimbs(_showLimbs);
    }

    private void UseHealthNumber_SettingChanged(object sender, EventArgs e)
    {
        UpdateHealth();
    }

    private void HealthController_EffectRemovedEvent(IEffect effect)
    {
        for (var i = 0; i < _effects.Count; i++)
        {
            var currentEffect = _effects[i];
            if (currentEffect.EffectType == effect.Type)
            {
                currentEffect.DecreaseAmount();
                if (currentEffect.Amount == 0)
                {
                    currentEffect.Remove();
                    _effects.Remove(currentEffect);
                }
                break;
            }
        }
    }

    private void HealthController_EffectAddedEvent(IEffect effect)
    {
        AddEffect(effect);
    }

    private void ShowEffects_SettingChanged(object sender, EventArgs e)
    {
        if (FikaPlugin.Instance.Settings.ShowEffects.Value)
        {
            _currentPlayer.HealthController.EffectAddedEvent += HealthController_EffectAddedEvent;
            _currentPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
            AddAllActiveEffects();
        }
        else
        {
            _currentPlayer.HealthController.EffectAddedEvent -= HealthController_EffectAddedEvent;
            _currentPlayer.HealthController.EffectRemovedEvent -= HealthController_EffectRemovedEvent;

            List<HealthBarEffect> tempList = [.. _effects];
            foreach (var effect in tempList)
            {
                effect.Remove();
            }
            _effects.Clear();
            tempList.Clear();
            tempList = null;
        }
    }

    private void HealthController_DiedEvent(EDamageType obj)
    {
        Destroy(this);
    }

    private void HealthController_BodyPartRestoredEvent(EBodyPart bodyPart, ValueStruct arg2)
    {
        if (_showLimbs)
        {
            HandleLimb(bodyPart, false);
        }
        UpdateHealth();
    }

    private void HealthController_BodyPartDestroyedEvent(EBodyPart bodyPart, EDamageType arg2)
    {
        if (_showLimbs)
        {
            HandleLimb(bodyPart, true);
        }
        UpdateHealth();
    }

    private void HealthController_HealthChangedEvent(EBodyPart bodyPart, float arg2, DamageInfoStruct arg3)
    {
        UpdateHealth();
    }

    private void UsePlateFactionSide_SettingChanged(object sender, EventArgs e)
    {
        SetPlayerPlateFactionVisibility(FikaPlugin.Instance.Settings.UsePlateFactionSide.Value);
    }

    private void HideHealthBar_SettingChanged(object sender, EventArgs e)
    {
        SetPlayerPlateHealthVisibility(FikaPlugin.Instance.Settings.HideHealthBar.Value);
    }

    private void UseNamePlates_SettingChanged(object sender, EventArgs e)
    {
        ToggleNamePlate();
    }
    #endregion

    private void AddEffect(IEffect effect)
    {
        for (var i = 0; i < _ignoredTypes.Length; i++)
        {
            if (_ignoredTypes[i] == effect.Type)
            {
                return;
            }
        }

        var found = false;
        foreach (var currentEffect in _effects)
        {
            if (currentEffect.EffectType == effect.Type)
            {
                currentEffect.IncreaseAmount();
                found = true;
            }
        }

        if (found)
        {
            return;
        }

        if (_effectIcons.TryGetValue(effect.Type, out var effectSprite))
        {
            var newEffect = Instantiate(_playerPlate.EffectImageTemplate, _playerPlate.EffectsBackground.transform);
            HealthBarEffect healthBarEffect = new();
            healthBarEffect.Init(newEffect, effect, effectSprite);
            _effects.Add(healthBarEffect);
        }
    }

    private void RefreshLimbs(bool active)
    {
        if (!active)
        {
            foreach (var bodyPart in _bodyParts.Values)
            {
                bodyPart.SetActive(false);
            }
        }
        else
        {
            var shouldActivate = false;
            foreach (var item in _bodyParts.Keys)
            {
                if (item is EBodyPart.Common)
                {
                    continue;
                }

                var destroyed = _currentPlayer.HealthController.IsBodyPartDestroyed(item);
                if (destroyed)
                {
                    shouldActivate = true;
                }

                _bodyParts[item].SetActive(destroyed);
            }

            _bodyParts[EBodyPart.Common].SetActive(shouldActivate);
        }
    }

    private void HandleLimb(EBodyPart bodyPart, bool destroyed)
    {
        _destroyedLimbs += destroyed ? 1 : -1;

        _bodyParts[EBodyPart.Common].SetActive(_destroyedLimbs > 0);
        _bodyParts[bodyPart].SetActive(destroyed);
    }

    private void AddAllActiveEffects()
    {
        foreach (var effect in _currentPlayer.HealthController.GetAllActiveEffects())
        {
            AddEffect(effect);
        }
    }

    /// <summary>
    /// Updates the health on the HealthBar, this is invoked from events on the healthcontroller
    /// </summary>
    private void UpdateHealth()
    {
        var health = _currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true);
        var currentHealth = health.Current;
        var maxHealth = health.Maximum;
        if (FikaPlugin.Instance.Settings.UseHealthNumber.Value)
        {
            if (!_playerPlate.healthNumberBackgroundScreen.gameObject.activeSelf)
            {
                SetPlayerPlateHealthVisibility(false);
            }
            var healthNumberPercentage = (int)Math.Round(currentHealth / maxHealth * 100);
            _playerPlate.SetHealthNumberText(healthNumberPercentage);
        }
        else
        {
            if (!_playerPlate.healthBarBackgroundScreen.gameObject.activeSelf)
            {
                SetPlayerPlateHealthVisibility(false);
            }

            var normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
            _playerPlate.healthBarScreen.DOFillAmount(normalizedHealth, _tweenLength);
            UpdateHealthBarColor(normalizedHealth);
        }
    }

    private void UpdateHealthBarColor(float normalizedHealth)
    {
        var color = Color.Lerp(FikaPlugin.Instance.Settings.LowHealthColor.Value,
            FikaPlugin.Instance.Settings.FullHealthColor.Value, normalizedHealth);
        color.a = _playerPlate.healthBarScreen.color.a; // keep the alpha value unchanged
        _playerPlate.healthBarScreen.color = color;
    }

    private void UpdateColorImage(Image screenObject, float alpha)
    {
        if (screenObject.gameObject.activeInHierarchy)
        {
            var color = screenObject.color;
            color.a = alpha;
            screenObject.color = color;
        }
    }

    private void UpdateColorTextMeshProUGUI(TextMeshProUGUI screenObject, float alpha)
    {
        if (screenObject.gameObject.activeInHierarchy)
        {
            var color = screenObject.color;
            color.a = alpha;
            screenObject.color = color;
        }
    }

    public void ToggleDowned(bool downed)
    {
        SetPlayerPlateHealthVisibility(downed);
        _playerPlate.downedStateBackgroundScreen.gameObject.SetActive(downed);
        _playerPlate.downedStateScreen.gameObject.SetActive(downed);
        _playerPlate.downedStateTimerScreen.gameObject.SetActive(downed);

        if (downed)
        {
            _playerPlate.downedStateScreen.SetText(LocaleUtils.UI_REVIVING_DOWNED.Localized()); // force downed message in case player was being revived before
        }

        _downed = downed;
        var bleedoutTime = FikaPlugin.Instance.Settings.ReviveConfig.BleedoutTime;
        _updateDowned = !Mathf.Approximately(bleedoutTime, 0f);
        if (_updateDowned)
        {
            _downedTimer = bleedoutTime;
        }
    }

    private void SetPlayerPlateHealthVisibility(bool hidden)
    {
        _playerPlate.healthNumberScreen.gameObject.SetActive(!hidden && FikaPlugin.Instance.Settings.UseHealthNumber.Value);
        _playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(!hidden && FikaPlugin.Instance.Settings.UseHealthNumber.Value);
        _playerPlate.healthBarScreen.gameObject.SetActive(!hidden && !FikaPlugin.Instance.Settings.UseHealthNumber.Value);
        _playerPlate.healthBarBackgroundScreen.gameObject.SetActive(!hidden && !FikaPlugin.Instance.Settings.UseHealthNumber.Value);
    }


    private void SetPlayerPlateFactionVisibility(bool visible)
    {
        if (_currentPlayer.Profile.Side == EPlayerSide.Usec)
        {
            _playerPlate.usecPlateScreen.gameObject.SetActive(visible);
        }
        else if (_currentPlayer.Profile.Side == EPlayerSide.Bear)
        {
            _playerPlate.bearPlateScreen.gameObject.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        FikaPlugin.Instance.Settings.UsePlateFactionSide.SettingChanged -= UsePlateFactionSide_SettingChanged;
        FikaPlugin.Instance.Settings.HideHealthBar.SettingChanged -= HideHealthBar_SettingChanged;
        FikaPlugin.Instance.Settings.UseNamePlates.SettingChanged -= UseNamePlates_SettingChanged;
        FikaPlugin.Instance.Settings.UseHealthNumber.SettingChanged -= UseHealthNumber_SettingChanged;
        FikaPlugin.Instance.Settings.ShowEffects.SettingChanged -= ShowEffects_SettingChanged;

        _currentPlayer.HealthController.HealthChangedEvent -= HealthController_HealthChangedEvent;
        _currentPlayer.HealthController.BodyPartDestroyedEvent -= HealthController_BodyPartDestroyedEvent;
        _currentPlayer.HealthController.BodyPartRestoredEvent -= HealthController_BodyPartRestoredEvent;
        _currentPlayer.HealthController.DiedEvent -= HealthController_DiedEvent;
        _currentPlayer.HealthController.EffectAddedEvent -= HealthController_EffectAddedEvent;
        _currentPlayer.HealthController.EffectRemovedEvent -= HealthController_EffectRemovedEvent;

        _playerPlate.gameObject.SetActive(false);
        _effects.Clear();
        Destroy(this);
    }

    private sealed class HealthBarEffect
    {
        public int Amount { get; private set; }

        public Type EffectType;
        public Image EffectImage;
        public TextMeshProUGUI TMPText;

        private GameObject _effectObject;

        public void Init(GameObject initObject, IEffect effect, Sprite effectSprite)
        {
            _effectObject = initObject;
            _effectObject.SetActive(true);
            EffectImage = _effectObject.transform.GetChild(0).GetComponent<Image>();
            EffectImage.sprite = effectSprite;
            TMPText = _effectObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            Amount = 1;
            TMPText.SetText("{0}", Amount);
            TMPText.enabled = false;
            EffectType = effect.Type;
        }

        public void Remove()
        {
            Destroy(EffectImage);
            Destroy(TMPText);
            Destroy(_effectObject);
        }

        public void IncreaseAmount()
        {
            Amount++;
            TMPText.SetText("{0}", Amount);

            if (Amount > 1)
            {
                TMPText.enabled = true;
            }
        }

        public void DecreaseAmount()
        {
            var newValue = Amount - 1;
            Amount = Math.Max(0, newValue);

            if (Amount == 0)
            {
                Remove();
                return;
            }

            if (Amount == 1)
            {
                TMPText.enabled = false;
            }

            TMPText.SetText("{0}", Amount);
        }
    }
}