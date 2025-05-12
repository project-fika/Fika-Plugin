// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.HealthSystem;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fika.Core.Coop.Custom
{
    /// <summary>
    /// Displays a health bar over another player <br/>
    /// Created by: ssh_
    /// </summary>
    public class FikaHealthBar : MonoBehaviour
    {
        private ObservedCoopPlayer currentPlayer;
        private CoopPlayer mainPlayer;
        private PlayerPlateUI playerPlate;
        private float screenScale = 1f;
        private Dictionary<Type, Sprite> effectIcons;
        private List<HealthBarEffect> effects;
        private List<Type> ignoredTypes;
        private float counter = 0;
        private bool updatePos = true;

        public static FikaHealthBar Create(ObservedCoopPlayer player)
        {
            if (player == null)
            {
                FikaPlugin.Instance.FikaLogger.LogError("FikaHealthBar::Create: Player was null!");
                return null;
            }

            FikaHealthBar healthBar = player.gameObject.AddComponent<FikaHealthBar>();
            healthBar.currentPlayer = player;
            healthBar.mainPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            healthBar.effectIcons = EFTHardSettings.Instance.StaticIcons.EffectIcons.EffectIcons;
            healthBar.effects = [];
            // Check for GClass increments, can be checked in EFT.UI.StaticIcons.EffectSprites method UnityEngine.ISerializationCallbackReceiver.OnAfterDeserialize
            // Wound, Encumbered, OverEncumbered, MildMusclePlain, SevereMusclePain
            healthBar.ignoredTypes = [typeof(GInterface336), typeof(GInterface338), typeof(GInterface339), typeof(GInterface353), typeof(GInterface354)];
            healthBar.CreateHealthBar();
            return healthBar;
        }

        public void ClearEffects()
        {
            foreach (HealthBarEffect effect in effects)
            {
                effect.Remove();
            }
            effects.Clear();
        }

        protected void Update()
        {
            if (currentPlayer != null)
            {
                UpdateScreenSpacePosition();
                if (FikaPlugin.UseOcclusion.Value)
                {
                    counter += Time.deltaTime;
                    if (counter > 1)
                    {
                        counter = 0;
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
            Vector3 targetPos = currentPlayer.PlayerBones.Neck.position;
            int layer = LayerMask.GetMask(["HighPolyCollider", "Terrain", "Player"]);

            if (Physics.Raycast(camPos, targetPos - camPos, out RaycastHit hitinfo, 800f, layer))
            {
                if (LayerMask.LayerToName(hitinfo.collider.gameObject.layer) != "Player")
                {
                    playerPlate.ScalarObjectScreen.SetActive(false);
                    updatePos = false;
                }
                else
                {
                    if (!playerPlate.ScalarObjectScreen.activeSelf)
                    {
                        playerPlate.ScalarObjectScreen.SetActive(true);
                        updatePos = true;
                        UpdateScreenSpacePosition();
                    }
                }
            }
        }

        private void UpdateScreenSpacePosition()
        {
            if (!updatePos)
            {
                return;
            }

            // ADS opacity handling
            float opacityMultiplier = 1f;
            ProceduralWeaponAnimation proceduralWeaponAnimation = mainPlayer.ProceduralWeaponAnimation;
            if (mainPlayer.HealthController.IsAlive && proceduralWeaponAnimation.IsAiming)
            {
                if (proceduralWeaponAnimation.CurrentScope.IsOptic && FikaPlugin.HideNamePlateInOptic.Value)
                {
                    playerPlate.ScalarObjectScreen.SetActive(false);
                    return;
                }
                opacityMultiplier = FikaPlugin.OpacityInADS.Value;
            }
            CameraClass cameraInstance = CameraClass.Instance;
            Camera camera = cameraInstance.Camera;

            // Distance check
            Vector3 direction = camera.transform.position - currentPlayer.Position;
            float sqrDistance = direction.sqrMagnitude;
            float maxDistanceToShow = FikaPlugin.MaxDistanceToShow.Value * FikaPlugin.MaxDistanceToShow.Value;
            if (sqrDistance > maxDistanceToShow)
            {
                playerPlate.ScalarObjectScreen.SetActive(false);
                return;
            }

            // If we're here, we can show the name plate
            playerPlate.ScalarObjectScreen.SetActive(true);

            float processedDistance = Mathf.Clamp(sqrDistance / 625, 0.6f, 1f);
            Vector3 position = new(currentPlayer.PlayerBones.Neck.position.x, currentPlayer.PlayerBones.Neck.position.y + (1f * processedDistance), currentPlayer.PlayerBones.Neck.position.z);

            if (!WorldToScreen.GetScreenPoint(position, mainPlayer, out Vector3 screenPoint, FikaPlugin.NamePlateUseOpticZoom.Value, false))
            {
                UpdateColorTextMeshProUGUI(playerPlate.playerNameScreen, 0);
                UpdateColorImage(playerPlate.healthBarScreen, 0);
                foreach (HealthBarEffect effect in effects)
                {
                    UpdateColorImage(effect.EffectImage, 0);
                }
                UpdateColorTextMeshProUGUI(playerPlate.healthNumberScreen, 0);
                UpdateColorImage(playerPlate.healthBarBackgroundScreen, 0);
                UpdateColorImage(playerPlate.healthNumberBackgroundScreen, 0);
                UpdateColorImage(playerPlate.usecPlateScreen, 0);
                UpdateColorImage(playerPlate.bearPlateScreen, 0);
                return;
            }

            SSAA ssaa = cameraInstance.SSAA;
            bool isSSAAEnabled = ssaa != null && ssaa.isActiveAndEnabled;
            if (isSSAAEnabled)
            {
                int outputWidth = ssaa.GetOutputWidth();
                float inputWidth = ssaa.GetInputWidth();
                screenScale = outputWidth / inputWidth;
            }

            playerPlate.ScalarObjectScreen.transform.position = screenScale < 1 ? screenPoint : screenPoint * screenScale;

            float distFromCenterMultiplier = 1f;
            if (FikaPlugin.DecreaseOpacityNotLookingAt.Value)
            {
                float screenWidth = isSSAAEnabled ? ssaa.GetOutputWidth() : Screen.width;
                float screenHeight = isSSAAEnabled ? ssaa.GetOutputHeight() : Screen.height;
                Vector3 screenCenter = new(screenWidth / 2, screenHeight / 2, 0);
                Vector3 playerPosition = playerPlate.ScalarObjectScreen.transform.position;
                float sqrDistFromCenter = (screenCenter - playerPosition).sqrMagnitude;
                float minScreenSizeHalf = Mathf.Min(screenWidth, screenHeight) / 2;
                float maxSqrDistFromCenter = minScreenSizeHalf * minScreenSizeHalf;
                distFromCenterMultiplier = Mathf.Clamp01(1 - (sqrDistFromCenter / maxSqrDistFromCenter));
            }

            float alpha = 1f;
            float halfMaxDistanceToShow = maxDistanceToShow / 2;
            float lerpValue = Mathf.Clamp01((sqrDistance - halfMaxDistanceToShow) / halfMaxDistanceToShow);
            alpha = Mathf.LerpUnclamped(alpha, 0, lerpValue);
            float namePlateScaleMult = Mathf.LerpUnclamped(1f, 0.5f, lerpValue);
            namePlateScaleMult = Mathf.Clamp(namePlateScaleMult * FikaPlugin.NamePlateScale.Value, FikaPlugin.MinimumNamePlateScale.Value * FikaPlugin.NamePlateScale.Value, FikaPlugin.NamePlateScale.Value);

            playerPlate.ScalarObjectScreen.transform.localScale = Vector3.one / processedDistance * namePlateScaleMult;

            alpha *= opacityMultiplier;
            alpha *= distFromCenterMultiplier;
            alpha = Mathf.Max(FikaPlugin.MinimumOpacity.Value, alpha);

            float backgroundOpacity = Mathf.Clamp(alpha, 0f, 0.44f);
            float healthAlphaMultiplier = FikaPlugin.HideHealthBar.Value ? 0 : 1f;

            UpdateColorTextMeshProUGUI(playerPlate.playerNameScreen, alpha);
            UpdateColorImage(playerPlate.healthBarScreen, alpha * healthAlphaMultiplier);
            foreach (HealthBarEffect effect in effects)
            {
                UpdateColorImage(effect.EffectImage, alpha);
            }
            UpdateColorTextMeshProUGUI(playerPlate.healthNumberScreen, alpha * healthAlphaMultiplier);
            UpdateColorImage(playerPlate.healthBarBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
            UpdateColorImage(playerPlate.healthNumberBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
            UpdateColorImage(playerPlate.usecPlateScreen, alpha);
            UpdateColorImage(playerPlate.bearPlateScreen, alpha);
        }

        private void CreateHealthBar()
        {
            GameObject uiPrefab = InternalBundleLoader.Instance.GetFikaAsset<GameObject>(InternalBundleLoader.EFikaAsset.PlayerUI);
            GameObject uiGameObj = Instantiate(uiPrefab);
            playerPlate = uiGameObj.GetComponent<PlayerPlateUI>();
            playerPlate.SetNameText(currentPlayer.Profile.Info.MainProfileNickname);
            if (FikaPlugin.DevelopersList.ContainsKey(currentPlayer.Profile.Nickname.ToLower()))
            {
                playerPlate.playerNameScreen.color = new Color(0, 6f, 1, 1);
                ChatSpecialIconSettings specialIcons = Resources.Load<ChatSpecialIconSettings>("ChatSpecialIconSettings");
                playerPlate.bearPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[1].IconSprite;
                playerPlate.bearPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
                playerPlate.usecPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[1].IconSprite;
                playerPlate.usecPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
            }
            else if (FikaPlugin.RespectedPlayersList.ContainsKey(currentPlayer.Profile.Nickname.ToLower()))
            {
                playerPlate.playerNameScreen.color = new Color(1, 0.6f, 0, 1);
                ChatSpecialIconSettings specialIcons = Resources.Load<ChatSpecialIconSettings>("ChatSpecialIconSettings");
                playerPlate.bearPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[2].IconSprite;
                playerPlate.bearPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
                playerPlate.usecPlateScreen.GetComponent<Image>().sprite = specialIcons.IconsSettings[2].IconSprite;
                playerPlate.usecPlateScreen.transform.localPosition = new Vector3(0f, 24.9f, 0);
            }
            else
            {
                playerPlate.playerNameScreen.color = FikaPlugin.NamePlateTextColor.Value;
            }
            // Start the plates both disabled, the visibility will be set in the update loop
            playerPlate.usecPlateScreen.gameObject.SetActive(false);
            playerPlate.bearPlateScreen.gameObject.SetActive(false);

            SetPlayerPlateFactionVisibility(FikaPlugin.UsePlateFactionSide.Value);
            SetPlayerPlateHealthVisibility(FikaPlugin.HideHealthBar.Value);

            playerPlate.gameObject.SetActive(FikaPlugin.UseNamePlates.Value);

            if (FikaPlugin.ShowEffects.Value)
            {
                currentPlayer.HealthController.EffectAddedEvent += HealthController_EffectAddedEvent;
                currentPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
                AddAllActiveEffects();
            }

            FikaPlugin.UsePlateFactionSide.SettingChanged += UsePlateFactionSide_SettingChanged;
            FikaPlugin.HideHealthBar.SettingChanged += HideHealthBar_SettingChanged;
            FikaPlugin.UseNamePlates.SettingChanged += UseNamePlates_SettingChanged;
            FikaPlugin.UseHealthNumber.SettingChanged += UseHealthNumber_SettingChanged;
            FikaPlugin.ShowEffects.SettingChanged += ShowEffects_SettingChanged;

            currentPlayer.HealthController.HealthChangedEvent += HealthController_HealthChangedEvent;
            currentPlayer.HealthController.BodyPartDestroyedEvent += HealthController_BodyPartDestroyedEvent;
            currentPlayer.HealthController.BodyPartRestoredEvent += HealthController_BodyPartRestoredEvent;
            currentPlayer.HealthController.DiedEvent += HealthController_DiedEvent;

            playerPlate.SetHealthNumberText("100%");

            UpdateHealth();
        }

        #region events
        private void UseHealthNumber_SettingChanged(object sender, EventArgs e)
        {
            UpdateHealth();
        }

        private void HealthController_EffectRemovedEvent(IEffect effect)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                HealthBarEffect currentEffect = effects[i];
                if (currentEffect.EffectType == effect.Type)
                {
                    currentEffect.DecreaseAmount();
                    if (currentEffect.Amount == 0)
                    {
                        currentEffect.Remove();
                        effects.Remove(currentEffect);
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
            if (ignoredTypes.Contains(effect.Type))
            {
                return;
            }

            bool found = false;
            foreach (HealthBarEffect currentEffect in effects)
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

            if (effectIcons.TryGetValue(effect.Type, out Sprite effectSprite))
            {
                GameObject newEffect = Instantiate(playerPlate.EffectImageTemplate, playerPlate.EffectsBackground.transform);
                HealthBarEffect healthBarEffect = new();
                healthBarEffect.Init(newEffect, effect, effectSprite);
                effects.Add(healthBarEffect);
            }
        }

        private void ShowEffects_SettingChanged(object sender, EventArgs e)
        {
            if (FikaPlugin.ShowEffects.Value)
            {
                currentPlayer.HealthController.EffectAddedEvent += HealthController_EffectAddedEvent;
                currentPlayer.HealthController.EffectRemovedEvent += HealthController_EffectRemovedEvent;
                AddAllActiveEffects();
            }
            else
            {
                currentPlayer.HealthController.EffectAddedEvent -= HealthController_EffectAddedEvent;
                currentPlayer.HealthController.EffectRemovedEvent -= HealthController_EffectRemovedEvent;

                List<HealthBarEffect> tempList = [.. effects];
                foreach (HealthBarEffect effect in tempList)
                {
                    effect.Remove();
                }
                effects.Clear();
                tempList.Clear();
                tempList = null;
            }
        }

        private void AddAllActiveEffects()
        {
            IEnumerable<IEffect> currentEffects = currentPlayer.HealthController.GetAllActiveEffects();
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
            playerPlate.gameObject.SetActive(FikaPlugin.UseNamePlates.Value);
        }
        #endregion

        /// <summary>
        /// Updates the health on the HealthBar, this is invoked from events on the healthcontroller
        /// </summary>
        private void UpdateHealth()
        {
            float currentHealth = currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true).Current;
            float maxHealth = currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true).Maximum;
            if (FikaPlugin.UseHealthNumber.Value)
            {
                if (!playerPlate.healthNumberBackgroundScreen.gameObject.activeSelf)
                {
                    SetPlayerPlateHealthVisibility(false);
                }
                int healthNumberPercentage = (int)Math.Round(currentHealth / maxHealth * 100);
                playerPlate.SetHealthNumberText($"{healthNumberPercentage}%");
            }
            else
            {
                if (!playerPlate.healthBarBackgroundScreen.gameObject.activeSelf)
                {
                    SetPlayerPlateHealthVisibility(false);
                }

                float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
                playerPlate.healthBarScreen.fillAmount = normalizedHealth;
                UpdateHealthBarColor(normalizedHealth);
            }
        }

        private void UpdateHealthBarColor(float normalizedHealth)
        {
            Color color = Color.Lerp(FikaPlugin.LowHealthColor.Value,
                FikaPlugin.FullHealthColor.Value, normalizedHealth);
            color.a = playerPlate.healthBarScreen.color.a; // Keep the alpha value unchanged
            playerPlate.healthBarScreen.color = color;
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
            playerPlate.healthNumberScreen.gameObject.SetActive(!hidden && FikaPlugin.UseHealthNumber.Value);
            playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(!hidden && FikaPlugin.UseHealthNumber.Value);
            playerPlate.healthBarScreen.gameObject.SetActive(!hidden && !FikaPlugin.UseHealthNumber.Value);
            playerPlate.healthBarBackgroundScreen.gameObject.SetActive(!hidden && !FikaPlugin.UseHealthNumber.Value);
        }


        private void SetPlayerPlateFactionVisibility(bool visible)
        {
            if (currentPlayer.Profile.Side == EPlayerSide.Usec)
            {
                playerPlate.usecPlateScreen.gameObject.SetActive(visible);
            }
            else if (currentPlayer.Profile.Side == EPlayerSide.Bear)
            {
                playerPlate.bearPlateScreen.gameObject.SetActive(visible);
            }
        }

        protected void OnDestroy()
        {
            FikaPlugin.UsePlateFactionSide.SettingChanged -= UsePlateFactionSide_SettingChanged;
            FikaPlugin.HideHealthBar.SettingChanged -= HideHealthBar_SettingChanged;
            FikaPlugin.UseNamePlates.SettingChanged -= UseNamePlates_SettingChanged;
            FikaPlugin.UseHealthNumber.SettingChanged -= UseHealthNumber_SettingChanged;
            FikaPlugin.ShowEffects.SettingChanged -= ShowEffects_SettingChanged;

            currentPlayer.HealthController.HealthChangedEvent -= HealthController_HealthChangedEvent;
            currentPlayer.HealthController.BodyPartDestroyedEvent -= HealthController_BodyPartDestroyedEvent;
            currentPlayer.HealthController.BodyPartRestoredEvent -= HealthController_BodyPartRestoredEvent;
            currentPlayer.HealthController.DiedEvent -= HealthController_DiedEvent;
            currentPlayer.HealthController.EffectAddedEvent -= HealthController_EffectAddedEvent;
            currentPlayer.HealthController.EffectRemovedEvent -= HealthController_EffectRemovedEvent;

            playerPlate.gameObject.SetActive(false);
            effects.Clear();
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
}