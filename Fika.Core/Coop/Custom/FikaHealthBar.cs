// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Players;
using Fika.Core.Utils;
using System;
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
        private int frameCounter = 0;
        private readonly int throttleInterval = 60; // throttle to 1 update per 60 frames

        protected void Awake()
        {
            currentPlayer = GetComponent<ObservedCoopPlayer>();
            mainPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            CreateHealthBar();
        }

        protected void Update()
        {
            if (currentPlayer != null)
            {
                bool throttleUpdate = IsThrottleUpdate();
                if (throttleUpdate)
                {
                    // Handling the visibility of elements
                    if (!FikaPlugin.UseNamePlates.Value)
                    {
                        playerPlate.gameObject.SetActive(false);
                        return;
                    }
                    else if (playerPlate.gameObject.active == false)
                    {
                        playerPlate.gameObject.SetActive(true);
                    }
                    SetPlayerPlateFactionVisibility(FikaPlugin.UsePlateFactionSide.Value);
                    SetPlayerPlateHealthVisibility(FikaPlugin.HideHealthBar.Value);
                }
                // Finally, update the screen space position
                UpdateScreenSpacePosition(throttleUpdate);
                // Destroy if this player is dead
                if (!currentPlayer.HealthController.IsAlive)
                {
                    Destroy(this);
                }
            }
            else
            {
                Destroy(this);
            }
        }

        private void UpdateScreenSpacePosition(bool throttleUpdate)
        {
            // ADS opacity handling
            float opacityMultiplier = 1f;
            ProceduralWeaponAnimation proceduralWeaponAnimation = mainPlayer.ProceduralWeaponAnimation;
            if (mainPlayer.HealthController.IsAlive && proceduralWeaponAnimation.IsAiming)
            {
                if (proceduralWeaponAnimation.CurrentScope.IsOptic && FikaPlugin.HideNamePlateInOptic.Value)
                {
                    playerPlate.ScalarObjectScreen.active = false;
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
                playerPlate.ScalarObjectScreen.active = false;
                return;
            }

            // If we're here, we can show the name plate
            playerPlate.ScalarObjectScreen.active = true;

            float processedDistance = Mathf.Clamp(sqrDistance / 625, 0.6f, 1f);
            Vector3 position = new(currentPlayer.PlayerBones.Neck.position.x, currentPlayer.PlayerBones.Neck.position.y + (1f * processedDistance), currentPlayer.PlayerBones.Neck.position.z);

            if (!WorldToScreen.GetScreenPoint(position, mainPlayer, out Vector3 screenPoint))
            {
                UpdateColorTextMeshProUGUI(playerPlate.playerNameScreen, 0);
                UpdateColorImage(playerPlate.healthBarScreen, 0);
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
            float lerpValue = Mathf.Clamp01((sqrDistance - halfMaxDistanceToShow) / (halfMaxDistanceToShow));
            alpha = Mathf.LerpUnclamped(alpha, 0, lerpValue);
            float namePlateScaleMult = Mathf.LerpUnclamped(1f, 0.5f, lerpValue);
            namePlateScaleMult = Mathf.Clamp(namePlateScaleMult * FikaPlugin.NamePlateScale.Value, FikaPlugin.MinimumNamePlateScale.Value * FikaPlugin.NamePlateScale.Value, FikaPlugin.NamePlateScale.Value);

            playerPlate.ScalarObjectScreen.transform.localScale = (Vector3.one / processedDistance) * namePlateScaleMult;

            alpha *= opacityMultiplier;
            alpha *= distFromCenterMultiplier;
            alpha = Mathf.Max(FikaPlugin.MinimumOpacity.Value, alpha);

            float backgroundOpacity = Mathf.Clamp(alpha, 0f, 0.44f);
            float healthAlphaMultiplier = FikaPlugin.HideHealthBar.Value ? 0 : 1f;

            UpdateColorTextMeshProUGUI(playerPlate.playerNameScreen, alpha);
            UpdateColorImage(playerPlate.healthBarScreen, alpha * healthAlphaMultiplier);
            UpdateColorTextMeshProUGUI(playerPlate.healthNumberScreen, alpha * healthAlphaMultiplier);
            UpdateColorImage(playerPlate.healthBarBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
            UpdateColorImage(playerPlate.healthNumberBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
            UpdateColorImage(playerPlate.usecPlateScreen, alpha);
            UpdateColorImage(playerPlate.bearPlateScreen, alpha);
        }

        private void CreateHealthBar()
        {
            if (currentPlayer != null)
            {
                GameObject uiPrefab = InternalBundleLoader.Instance.GetAssetBundle("playerui").LoadAsset<GameObject>("PlayerFriendlyUI");
                GameObject uiGameObj = Instantiate(uiPrefab);
                playerPlate = uiGameObj.GetComponent<PlayerPlateUI>();
                playerPlate.SetNameText(currentPlayer.Profile.Info.MainProfileNickname);
                if (FikaPlugin.DevelopersList.ContainsKey(currentPlayer.Profile.Nickname.ToLower()))
                {
                    playerPlate.playerNameScreen.color = new Color(0, 0.6091f, 1, 1);
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
                // Start the plates both disabled, the visibility will be set in the update loop
                playerPlate.usecPlateScreen.gameObject.SetActive(false);
                playerPlate.bearPlateScreen.gameObject.SetActive(false);
            }

            currentPlayer.HealthController.HealthChangedEvent += HealthController_HealthChangedEvent;
            currentPlayer.HealthController.BodyPartDestroyedEvent += HealthController_BodyPartDestroyedEvent;
            currentPlayer.HealthController.BodyPartRestoredEvent += HealthController_BodyPartRestoredEvent;

            UpdateHealth();
        }

        private void HealthController_BodyPartRestoredEvent(EBodyPart arg1, EFT.HealthSystem.ValueStruct arg2)
        {
            UpdateHealth();
        }

        private void HealthController_BodyPartDestroyedEvent(EBodyPart arg1, EDamageType arg2)
        {
            UpdateHealth();
        }

        private void HealthController_HealthChangedEvent(EBodyPart arg1, float arg2, DamageInfo arg3)
        {
            UpdateHealth();
        }

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
                    playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(true);
                    playerPlate.healthBarBackgroundScreen.gameObject.SetActive(false);
                }
                int healthNumberPercentage = (int)Math.Round((currentHealth / maxHealth) * 100);
                playerPlate.SetHealthNumberText($"{healthNumberPercentage}%");
            }
            else
            {
                if (!playerPlate.healthBarBackgroundScreen.gameObject.activeSelf)
                {
                    playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(false);
                    playerPlate.healthBarBackgroundScreen.gameObject.SetActive(true);
                }

                float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
                playerPlate.healthBarScreen.fillAmount = normalizedHealth;
                UpdateHealthBarColor(normalizedHealth);
            }
        }

        private void UpdateHealthBarColor(float normalizedHealth)
        {
            Color color = Color.Lerp(Color.red, Color.green, normalizedHealth);
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

        private void UpdateColorTextMeshProUGUI(TMPro.TextMeshProUGUI screenObject, float alpha)
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

        private bool IsThrottleUpdate()
        {
            // For throttling updates to various elements
            frameCounter++;
            bool throttleUpdate = frameCounter >= throttleInterval;
            if (throttleUpdate)
            {
                frameCounter = 0;
            }
            return throttleUpdate;
        }

        private void OnDestroy()
        {
            currentPlayer.HealthController.HealthChangedEvent -= HealthController_HealthChangedEvent;
            currentPlayer.HealthController.BodyPartDestroyedEvent -= HealthController_BodyPartDestroyedEvent;
            currentPlayer.HealthController.BodyPartRestoredEvent -= HealthController_BodyPartRestoredEvent;
            playerPlate.gameObject.SetActive(false);
            Destroy(this);
        }
    }
}