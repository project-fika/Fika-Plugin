// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Animations;
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
    /// <see cref="ActiveHealthController-MildMusclePlain"/>, <see cref="ActiveHealthController.SevereMusclePain"/>
    /// </summary>
    private static readonly List<Type> _ignoredTypes = [typeof(GInterface345), typeof(GInterface347), typeof(GInterface348), typeof(GInterface362), typeof(GInterface363)];

    private ObservedPlayer _currentPlayer;
    private FikaPlayer _mainPlayer;
    private PlayerPlateUI _playerPlate;
    private float _screenScale = 1f;
    private Dictionary<Type, Sprite> _effectIcons;
    private List<HealthBarEffect> _effects;
    private float _counter = 0;
    private bool _updatePos = true;

    public static FikaHealthBar Create(ObservedPlayer player)
    {
        if (player == null)
        {
            FikaPlugin.Instance.FikaLogger.LogError("FikaHealthBar::Create: Player was null!");
            return null;
        }

        FikaHealthBar healthBar = player.gameObject.AddComponent<FikaHealthBar>();
        healthBar._currentPlayer = player;
        healthBar._mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        healthBar._effectIcons = EFTHardSettings.Instance.StaticIcons.EffectIcons.EffectIcons;
        healthBar._effects = [];

        healthBar.CreateHealthBar();
        return healthBar;
    }

    public void ClearEffects()
    {
        foreach (HealthBarEffect effect in _effects)
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
            if (FikaPlugin.UseOcclusion.Value)
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
        Vector3 camPos = CameraClass.Instance.Camera.transform.position;
        Vector3 targetPos = _currentPlayer.PlayerBones.Neck.position;
        int layer = LayerMask.GetMask(["HighPolyCollider", "Terrain", "Player"]);

        if (Physics.Raycast(camPos, targetPos - camPos, out RaycastHit hitinfo, 800f, layer))
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

        // ADS opacity handling
        float opacityMultiplier = 1f;
        ProceduralWeaponAnimation proceduralWeaponAnimation = _mainPlayer.ProceduralWeaponAnimation;
        if (_mainPlayer.HealthController.IsAlive && proceduralWeaponAnimation.IsAiming)
        {
            if (proceduralWeaponAnimation.CurrentScope.IsOptic && FikaPlugin.HideNamePlateInOptic.Value)
            {
                _playerPlate.ScalarObjectScreen.SetActive(false);
                return;
            }
            opacityMultiplier = FikaPlugin.OpacityInADS.Value;
        }
        CameraClass cameraInstance = CameraClass.Instance;
        Camera camera = cameraInstance.Camera;

        // Distance check
        Vector3 direction = camera.transform.position - _currentPlayer.Position;
        float sqrDistance = direction.sqrMagnitude;
        float maxDistanceToShow = FikaPlugin.MaxDistanceToShow.Value * FikaPlugin.MaxDistanceToShow.Value;
        if (sqrDistance > maxDistanceToShow)
        {
            _playerPlate.ScalarObjectScreen.SetActive(false);
            return;
        }

        // If we're here, we can show the name plate
        _playerPlate.ScalarObjectScreen.SetActive(true);

        float processedDistance = Mathf.Clamp(sqrDistance / 625, 0.6f, 1f);
        Vector3 position = new(_currentPlayer.PlayerBones.Neck.position.x, _currentPlayer.PlayerBones.Neck.position.y + 1f * processedDistance, _currentPlayer.PlayerBones.Neck.position.z);

        if (!WorldToScreen.GetScreenPoint(position, _mainPlayer, out Vector3 screenPoint, FikaPlugin.NamePlateUseOpticZoom.Value, false))
        {
            UpdateColorTextMeshProUGUI(_playerPlate.playerNameScreen, 0);
            UpdateColorImage(_playerPlate.healthBarScreen, 0);
            foreach (HealthBarEffect effect in _effects)
            {
                UpdateColorImage(effect.EffectImage, 0);
            }
            UpdateColorTextMeshProUGUI(_playerPlate.healthNumberScreen, 0);
            UpdateColorImage(_playerPlate.healthBarBackgroundScreen, 0);
            UpdateColorImage(_playerPlate.healthNumberBackgroundScreen, 0);
            UpdateColorImage(_playerPlate.usecPlateScreen, 0);
            UpdateColorImage(_playerPlate.bearPlateScreen, 0);
            return;
        }

        SSAA ssaa = cameraInstance.SSAA;
        bool isSSAAEnabled = ssaa != null && ssaa.isActiveAndEnabled;
        if (isSSAAEnabled)
        {
            int outputWidth = ssaa.GetOutputWidth();
            float inputWidth = ssaa.GetInputWidth();
            _screenScale = outputWidth / inputWidth;
        }

        _playerPlate.ScalarObjectScreen.transform.position = _screenScale < 1 ? screenPoint : screenPoint * _screenScale;

        float distFromCenterMultiplier = 1f;
        if (FikaPlugin.DecreaseOpacityNotLookingAt.Value)
        {
            float screenWidth = isSSAAEnabled ? ssaa.GetOutputWidth() : Screen.width;
            float screenHeight = isSSAAEnabled ? ssaa.GetOutputHeight() : Screen.height;
            Vector3 screenCenter = new(screenWidth / 2, screenHeight / 2, 0);
            Vector3 playerPosition = _playerPlate.ScalarObjectScreen.transform.position;
            float sqrDistFromCenter = (screenCenter - playerPosition).sqrMagnitude;
            float minScreenSizeHalf = Mathf.Min(screenWidth, screenHeight) / 2;
            float maxSqrDistFromCenter = minScreenSizeHalf * minScreenSizeHalf;
            distFromCenterMultiplier = Mathf.Clamp01(1 - sqrDistFromCenter / maxSqrDistFromCenter);
        }

        float alpha = 1f;
        float halfMaxDistanceToShow = maxDistanceToShow / 2;
        float lerpValue = Mathf.Clamp01((sqrDistance - halfMaxDistanceToShow) / halfMaxDistanceToShow);
        alpha = Mathf.LerpUnclamped(alpha, 0, lerpValue);
        float namePlateScaleMult = Mathf.LerpUnclamped(1f, 0.5f, lerpValue);
        namePlateScaleMult = Mathf.Clamp(namePlateScaleMult * FikaPlugin.NamePlateScale.Value, FikaPlugin.MinimumNamePlateScale.Value * FikaPlugin.NamePlateScale.Value, FikaPlugin.NamePlateScale.Value);

        _playerPlate.ScalarObjectScreen.transform.localScale = Vector3.one / processedDistance * namePlateScaleMult;

        alpha *= opacityMultiplier;
        alpha *= distFromCenterMultiplier;
        alpha = Mathf.Max(FikaPlugin.MinimumOpacity.Value, alpha);

        float backgroundOpacity = Mathf.Clamp(alpha, 0f, 0.44f);
        float healthAlphaMultiplier = FikaPlugin.HideHealthBar.Value ? 0 : 1f;

        UpdateColorTextMeshProUGUI(_playerPlate.playerNameScreen, alpha);
        UpdateColorImage(_playerPlate.healthBarScreen, alpha * healthAlphaMultiplier);
        foreach (HealthBarEffect effect in _effects)
        {
            UpdateColorImage(effect.EffectImage, alpha);
        }
        UpdateColorTextMeshProUGUI(_playerPlate.healthNumberScreen, alpha * healthAlphaMultiplier);
        UpdateColorImage(_playerPlate.healthBarBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
        UpdateColorImage(_playerPlate.healthNumberBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
        UpdateColorImage(_playerPlate.usecPlateScreen, alpha);
        UpdateColorImage(_playerPlate.bearPlateScreen, alpha);
    }

    private void CreateHealthBar()
    {
        GameObject uiPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.PlayerUI);
        GameObject uiGameObj = Instantiate(uiPrefab);
        _playerPlate = uiGameObj.GetComponent<PlayerPlateUI>();
        _playerPlate.SetNameText(_currentPlayer.Profile.Info.MainProfileNickname);
        if (FikaPlugin.DevelopersList.ContainsKey(_currentPlayer.Profile.Nickname.ToLower()))
        {
            _playerPlate.playerNameScreen.color = new Color(0, 6f, 1, 1);
            ChatSpecialIconSettings specialIcons = Resources.Load<ChatSpecialIconSettings>("ChatSpecialIconSettings");
            _playerPlate.bearPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[1].IconSprite;
            _playerPlate.bearPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
            _playerPlate.usecPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[1].IconSprite;
            _playerPlate.usecPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
        }
        else if (FikaPlugin.RespectedPlayersList.ContainsKey(_currentPlayer.Profile.Nickname.ToLower()))
        {
            _playerPlate.playerNameScreen.color = new Color(1, 0.6f, 0, 1);
            ChatSpecialIconSettings specialIcons = Resources.Load<ChatSpecialIconSettings>("ChatSpecialIconSettings");
            _playerPlate.bearPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[2].IconSprite;
            _playerPlate.bearPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
            _playerPlate.usecPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[2].IconSprite;
            _playerPlate.usecPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
        }
        else
        {
            _playerPlate.playerNameScreen.color = FikaPlugin.NamePlateTextColor.Value;
        }
        // Start the plates both disabled, the visibility will be set in the update loop
        _playerPlate.usecPlateScreen.gameObject.SetActive(false);
        _playerPlate.bearPlateScreen.gameObject.SetActive(false);

        SetPlayerPlateFactionVisibility(FikaPlugin.UsePlateFactionSide.Value);
        SetPlayerPlateHealthVisibility(FikaPlugin.HideHealthBar.Value);

        _playerPlate.gameObject.SetActive(FikaPlugin.UseNamePlates.Value);

        if (FikaPlugin.ShowEffects.Value)
        {
            _currentPlayer.HealthController.EffectAddedEvent += HealthController_EffectAddedEvent;
            _currentPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
            AddAllActiveEffects();
        }

        FikaPlugin.UsePlateFactionSide.SettingChanged += UsePlateFactionSide_SettingChanged;
        FikaPlugin.HideHealthBar.SettingChanged += HideHealthBar_SettingChanged;
        FikaPlugin.UseNamePlates.SettingChanged += UseNamePlates_SettingChanged;
        FikaPlugin.UseHealthNumber.SettingChanged += UseHealthNumber_SettingChanged;
        FikaPlugin.ShowEffects.SettingChanged += ShowEffects_SettingChanged;

        _currentPlayer.HealthController.HealthChangedEvent += HealthController_HealthChangedEvent;
        _currentPlayer.HealthController.BodyPartDestroyedEvent += HealthController_BodyPartDestroyedEvent;
        _currentPlayer.HealthController.BodyPartRestoredEvent += HealthController_BodyPartRestoredEvent;
        _currentPlayer.HealthController.DiedEvent += HealthController_DiedEvent;

        _playerPlate.SetHealthNumberText("100%");

        UpdateHealth();
    }

    #region events
    private void UseHealthNumber_SettingChanged(object sender, EventArgs e)
    {
        UpdateHealth();
    }

    private void HealthController_EffectRemovedEvent(IEffect effect)
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            HealthBarEffect currentEffect = _effects[i];
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

        bool found = false;
        foreach (HealthBarEffect currentEffect in _effects)
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

        if (_effectIcons.TryGetValue(effect.Type, out Sprite effectSprite))
        {
            GameObject newEffect = Instantiate(_playerPlate.EffectImageTemplate, _playerPlate.EffectsBackground.transform);
            HealthBarEffect healthBarEffect = new();
            healthBarEffect.Init(newEffect, effect, effectSprite);
            _effects.Add(healthBarEffect);
        }
    }

    private void ShowEffects_SettingChanged(object sender, EventArgs e)
    {
        if (FikaPlugin.ShowEffects.Value)
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
            foreach (HealthBarEffect effect in tempList)
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
        IEnumerable<IEffect> currentEffects = _currentPlayer.HealthController.GetAllActiveEffects();
        foreach (IEffect effect in currentEffects)
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
        SetPlayerPlateFactionVisibility(FikaPlugin.UsePlateFactionSide.Value);
    }

    private void HideHealthBar_SettingChanged(object sender, EventArgs e)
    {
        SetPlayerPlateHealthVisibility(FikaPlugin.HideHealthBar.Value);
    }

    private void UseNamePlates_SettingChanged(object sender, EventArgs e)
    {
        _playerPlate.gameObject.SetActive(FikaPlugin.UseNamePlates.Value);
    }
    #endregion

    /// <summary>
    /// Updates the health on the HealthBar, this is invoked from events on the healthcontroller
    /// </summary>
    private void UpdateHealth()
    {
        float currentHealth = _currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true).Current;
        float maxHealth = _currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true).Maximum;
        if (FikaPlugin.UseHealthNumber.Value)
        {
            if (!_playerPlate.healthNumberBackgroundScreen.gameObject.activeSelf)
            {
                SetPlayerPlateHealthVisibility(false);
            }
            int healthNumberPercentage = (int)Math.Round(currentHealth / maxHealth * 100);
            _playerPlate.SetHealthNumberText($"{healthNumberPercentage}%");
        }
        else
        {
            if (!_playerPlate.healthBarBackgroundScreen.gameObject.activeSelf)
            {
                SetPlayerPlateHealthVisibility(false);
            }

            float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
            _playerPlate.healthBarScreen.fillAmount = normalizedHealth;
            UpdateHealthBarColor(normalizedHealth);
        }
    }

    private void UpdateHealthBarColor(float normalizedHealth)
    {
        Color color = Color.Lerp(FikaPlugin.LowHealthColor.Value,
            FikaPlugin.FullHealthColor.Value, normalizedHealth);
        color.a = _playerPlate.healthBarScreen.color.a; // Keep the alpha value unchanged
        _playerPlate.healthBarScreen.color = color;
    }

    private void UpdateColorImage(Image screenObject, float alpha)
    {
        if (screenObject.gameObject.activeInHierarchy)
        {
            Color color = screenObject.color;
            color.a = alpha;
            screenObject.color = color;
        }
    }

    private void UpdateColorTextMeshProUGUI(TextMeshProUGUI screenObject, float alpha)
    {
        if (screenObject.gameObject.activeInHierarchy)
        {
            Color color = screenObject.color;
            color.a = alpha;
            screenObject.color = color;
        }
    }

    private void SetPlayerPlateHealthVisibility(bool hidden)
    {
        _playerPlate.healthNumberScreen.gameObject.SetActive(!hidden && FikaPlugin.UseHealthNumber.Value);
        _playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(!hidden && FikaPlugin.UseHealthNumber.Value);
        _playerPlate.healthBarScreen.gameObject.SetActive(!hidden && !FikaPlugin.UseHealthNumber.Value);
        _playerPlate.healthBarBackgroundScreen.gameObject.SetActive(!hidden && !FikaPlugin.UseHealthNumber.Value);
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
        FikaPlugin.UsePlateFactionSide.SettingChanged -= UsePlateFactionSide_SettingChanged;
        FikaPlugin.HideHealthBar.SettingChanged -= HideHealthBar_SettingChanged;
        FikaPlugin.UseNamePlates.SettingChanged -= UseNamePlates_SettingChanged;
        FikaPlugin.UseHealthNumber.SettingChanged -= UseHealthNumber_SettingChanged;
        FikaPlugin.ShowEffects.SettingChanged -= ShowEffects_SettingChanged;

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

        private GameObject effectObject;
        private TextMeshProUGUI tmpText;

        public void Init(GameObject initObject, IEffect effect, Sprite effectSprite)
        {
            effectObject = initObject;
            effectObject.SetActive(true);
            EffectImage = effectObject.transform.GetChild(0).GetComponent<Image>();
            EffectImage.sprite = effectSprite;
            tmpText = effectObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            Amount = 1;
            tmpText.text = Amount.ToString();
            tmpText.enabled = false;
            EffectType = effect.Type;
        }

        public void Remove()
        {
            Destroy(EffectImage);
            Destroy(tmpText);
            Destroy(effectObject);
        }

        public void IncreaseAmount()
        {
            Amount++;
            tmpText.text = Amount.ToString();

            if (Amount > 1)
            {
                tmpText.enabled = true;
            }
        }

        public void DecreaseAmount()
        {
            int newValue = Amount - 1;
            Amount = Math.Max(0, newValue);

            if (Amount == 0)
            {
                Remove();
                return;
            }

            if (Amount == 1)
            {
                tmpText.enabled = false;
            }

            tmpText.text = Amount.ToString();
        }
    }
}