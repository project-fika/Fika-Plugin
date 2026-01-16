// © 2026 Lacyway All Rights Reserved

using Comfort.Common;
using DG.Tweening;
using EFT;
using EFT.HealthSystem;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace Fika.Core.Main.Components;

/// <summary>
/// Displays a health bar over another player <br/>
/// Created by: ssh_
/// </summary>
public class FikaHealthBar : MonoBehaviour
{
    /// <summary>
    /// Check for GClass increments, can be checked in <see cref="StaticIcons.EffectSprites"/> method <see cref="ISerializationCallbackReceiver.OnAfterDeserialize"/> <br/><br/>
    /// <see cref="ActiveHealthController.Wound"/>, <see cref="ActiveHealthController.Encumbered"/>, <see cref="ActiveHealthController.OverEncumbered"/>, <br/>
    /// <see cref="ActiveHealthController.MildMusclePain"/>, <see cref="ActiveHealthController.SevereMusclePain"/>
    /// </summary>
    private static readonly List<Type> _ignoredTypes = [typeof(GInterface362), typeof(GInterface364), typeof(GInterface365), typeof(GInterface379), typeof(GInterface380)];

    private const float _tweenLength = 0.25f;

    private ObservedPlayer _currentPlayer;
    private FikaPlayer _mainPlayer;
    private PlayerPlateUI _playerPlate;
    private Dictionary<Type, Sprite> _effectIcons;
    private List<HealthBarEffect> _effects;
    private float _counter;
    private bool _updatePos = true;
    private RectTransform _canvasRect;

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

    protected void Update()
    {
        if (_currentPlayer != null)
        {
            UpdateScreenSpacePosition();
            if (FikaPlugin.Instance.Settings.UseOcclusion.Value)
            {
                _counter += Time.deltaTime;
                if (_counter > 1)
                {
                    _counter = 0;
                    CheckForOcclusion();
                }
            }
        }
        else
        {
            Destroy(this);
        }
    }

    private void CheckForOcclusion()
    {
        var camPos = CameraClass.Instance.Camera.transform.position;
        var targetPos = _currentPlayer.PlayerBones.Neck.position;
        var layer = LayerMask.GetMask(["HighPolyCollider", "Terrain", "Player"]);

        if (Physics.Raycast(camPos, targetPos - camPos, out var hitinfo, 800f, layer))
        {
            if (LayerMask.LayerToName(hitinfo.collider.gameObject.layer) != "Player")
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

        var opacityMultiplier = 1f;
        var proceduralWeaponAnimation = _mainPlayer.ProceduralWeaponAnimation;
        if (_mainPlayer.HealthController.IsAlive && proceduralWeaponAnimation.IsAiming)
        {
            if (proceduralWeaponAnimation.CurrentScope.IsOptic && FikaPlugin.Instance.Settings.HideNamePlateInOptic.Value)
            {
                _playerPlate.ScalarObjectScreen.SetActive(false);
                return;
            }

            opacityMultiplier = FikaPlugin.Instance.Settings.OpacityInADS.Value;
        }

        var cameraInstance = CameraClass.Instance;
        var camera = cameraInstance.Camera;

        var distance = Vector3.Distance(camera.transform.position, _currentPlayer.Position);
        if (distance > FikaPlugin.Instance.Settings.MaxDistanceToShow.Value)
        {
            _playerPlate.ScalarObjectScreen.SetActive(false);
            return;
        }

        _playerPlate.ScalarObjectScreen.SetActive(true);

        var targetPosition = _currentPlayer.PlayerBones.Neck.position + (Vector3.up * 1f);

        if (!WorldToScreen.ProjectToCanvas(targetPosition, _mainPlayer, _canvasRect, out var canvasPos, FikaPlugin.Instance.Settings.NamePlateUseOpticZoom.Value, false))
        {
            // behind camera, hide all elements
            UpdateColorTextMeshProUGUI(_playerPlate.playerNameScreen, 0);
            UpdateColorImage(_playerPlate.healthBarScreen, 0);
            foreach (var effect in _effects)
            {
                UpdateColorImage(effect.EffectImage, 0);
                if (effect.Amount > 1)
                {
                    UpdateColorTextMeshProUGUI(effect.TMPText, 0);
                }
            }

            UpdateColorTextMeshProUGUI(_playerPlate.healthNumberScreen, 0);
            UpdateColorImage(_playerPlate.healthBarBackgroundScreen, 0);
            UpdateColorImage(_playerPlate.healthNumberBackgroundScreen, 0);
            UpdateColorImage(_playerPlate.usecPlateScreen, 0);
            UpdateColorImage(_playerPlate.bearPlateScreen, 0);
            return;
        }

        _playerPlate.ScalarObjectScreen.GetComponent<RectTransform>().anchoredPosition = canvasPos;

        var distFromCenterMultiplier = 1f;
        if (FikaPlugin.Instance.Settings.DecreaseOpacityNotLookingAt.Value)
        {
            var sqrDistFromCenter = canvasPos.sqrMagnitude;
            var maxSqrDistFromCenter = Mathf.Pow(Mathf.Min(_canvasRect.sizeDelta.x, _canvasRect.sizeDelta.y) / 2f, 2);
            distFromCenterMultiplier = Mathf.Clamp01(1 - (sqrDistFromCenter / maxSqrDistFromCenter));
        }

        var t = Mathf.InverseLerp(2f, FikaPlugin.Instance.Settings.MaxDistanceToShow.Value, distance);
        var scaleMultiplier = Mathf.Lerp(0.48f, 0.075f, Mathf.Pow(t, 1.5f)); // a = near player, b = far player
        scaleMultiplier *= FikaPlugin.Instance.Settings.NamePlateScale.Value;
        _playerPlate.ScalarObjectScreen.transform.localScale = Vector3.one * scaleMultiplier;

        var distanceAlpha = Mathf.Lerp(1f, 0.3f, t);
        var finalAlpha = Mathf.Max(FikaPlugin.Instance.Settings.MinimumOpacity.Value, distanceAlpha * opacityMultiplier * distFromCenterMultiplier);

        var backgroundAlpha = Mathf.Clamp(finalAlpha * 0.44f, 0.1f, 0.44f);
        var healthAlphaMultiplier = FikaPlugin.Instance.Settings.HideHealthBar.Value ? 0f : 1f;

        UpdateColorTextMeshProUGUI(_playerPlate.playerNameScreen, finalAlpha);
        UpdateColorImage(_playerPlate.healthBarScreen, Mathf.Max(finalAlpha * healthAlphaMultiplier, 0.15f));

        foreach (var effect in _effects)
        {
            UpdateColorImage(effect.EffectImage, finalAlpha);
            if (effect.Amount > 1)
            {
                UpdateColorTextMeshProUGUI(effect.TMPText, finalAlpha);
            }
        }

        UpdateColorTextMeshProUGUI(_playerPlate.healthNumberScreen, finalAlpha * healthAlphaMultiplier);
        UpdateColorImage(_playerPlate.healthBarBackgroundScreen, backgroundAlpha * healthAlphaMultiplier);
        UpdateColorImage(_playerPlate.healthNumberBackgroundScreen, backgroundAlpha * healthAlphaMultiplier);
        UpdateColorImage(_playerPlate.usecPlateScreen, finalAlpha);
        UpdateColorImage(_playerPlate.bearPlateScreen, finalAlpha);
    }


    private void CreateHealthBar()
    {
        var uiPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.PlayerUI);
        var uiGameObj = Instantiate(uiPrefab);
        _playerPlate = uiGameObj.GetComponent<PlayerPlateUI>();
        _playerPlate.SetNameText(_currentPlayer.Profile.Info.MainProfileNickname);
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

        _playerPlate.gameObject.SetActive(FikaPlugin.Instance.Settings.UseNamePlates.Value);

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

        _currentPlayer.HealthController.HealthChangedEvent += HealthController_HealthChangedEvent;
        _currentPlayer.HealthController.BodyPartDestroyedEvent += HealthController_BodyPartDestroyedEvent;
        _currentPlayer.HealthController.BodyPartRestoredEvent += HealthController_BodyPartRestoredEvent;
        _currentPlayer.HealthController.DiedEvent += HealthController_DiedEvent;

        _playerPlate.SetHealthNumberText("100%");

        _canvasRect = _playerPlate.ScalarObjectScreen.transform.parent.RectTransform();

        UpdateHealth();
    }

    #region events
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

    private void AddEffect(IEffect effect)
    {
        if (_ignoredTypes.Contains(effect.Type))
        {
            return;
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

    private void AddAllActiveEffects()
    {
        var currentEffects = _currentPlayer.HealthController.GetAllActiveEffects();
        foreach (var effect in currentEffects)
        {
            AddEffect(effect);
        }
    }

    private void HealthController_DiedEvent(EDamageType obj)
    {
        Destroy(this);
    }

    private void HealthController_BodyPartRestoredEvent(EBodyPart arg1, ValueStruct arg2)
    {
        UpdateHealth();
    }

    private void HealthController_BodyPartDestroyedEvent(EBodyPart arg1, EDamageType arg2)
    {
        UpdateHealth();
    }

    private void HealthController_HealthChangedEvent(EBodyPart arg1, float arg2, DamageInfoStruct arg3)
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
        _playerPlate.gameObject.SetActive(FikaPlugin.Instance.Settings.UseNamePlates.Value);
    }
    #endregion

    /// <summary>
    /// Updates the health on the HealthBar, this is invoked from events on the healthcontroller
    /// </summary>
    private void UpdateHealth()
    {
        var currentHealth = _currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true).Current;
        var maxHealth = _currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true).Maximum;
        if (FikaPlugin.Instance.Settings.UseHealthNumber.Value)
        {
            if (!_playerPlate.healthNumberBackgroundScreen.gameObject.activeSelf)
            {
                SetPlayerPlateHealthVisibility(false);
            }
            var healthNumberPercentage = (int)Math.Round(currentHealth / maxHealth * 100);
            _playerPlate.SetHealthNumberText($"{healthNumberPercentage}%");
        }
        else
        {
            if (!_playerPlate.healthBarBackgroundScreen.gameObject.activeSelf)
            {
                SetPlayerPlateHealthVisibility(false);
            }

            var normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
            _playerPlate.healthBarScreen.DOFillAmount(Mathf.Clamp01(currentHealth / maxHealth), _tweenLength);
            //_playerPlate.healthBarScreen.fillAmount = normalizedHealth;
            UpdateHealthBarColor(normalizedHealth);
        }
    }

    private void UpdateHealthBarColor(float normalizedHealth)
    {
        var color = Color.Lerp(FikaPlugin.Instance.Settings.LowHealthColor.Value,
            FikaPlugin.Instance.Settings.FullHealthColor.Value, normalizedHealth);
        color.a = _playerPlate.healthBarScreen.color.a; // Keep the alpha value unchanged
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

    protected void OnDestroy()
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

    private class HealthBarEffect
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
            TMPText.text = Amount.ToString();
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
            TMPText.text = Amount.ToString();

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

            TMPText.text = Amount.ToString();
        }
    }
}