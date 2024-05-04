// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Players;
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
                if (!FikaPlugin.UseNamePlates.Value)
                {
                    playerPlate.gameObject.SetActive(false);
                    return;
                }
                else if (playerPlate.gameObject.active == false)
                {
                    playerPlate.gameObject.SetActive(true);
                }
                UpdateScreenSpacePosition();
                if (!FikaPlugin.HideHealthBar.Value) {
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
                        if (!playerPlate.healthBarBackgroundScreen.gameObject.active)
                        {
                            playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(false);
                            playerPlate.healthBarBackgroundScreen.gameObject.SetActive(true);
                        }

                        float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
                        playerPlate.healthBarScreen.fillAmount = normalizedHealth;
                        UpdateHealthBarColor(normalizedHealth);
                    }
                }
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

        private void UpdateScreenSpacePosition()
        {
            Camera camera = CameraClass.Instance.Camera;
            
            float opacityMultiplier = 1f;
            if (mainPlayer.HealthController.IsAlive && mainPlayer.ProceduralWeaponAnimation.IsAiming)
            {
                // Scope scaling and positioning
                if (mainPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic)
                {
                    if (FikaPlugin.HideNamePlateInOptic.Value) {
                        playerPlate.ScalarObjectScreen.active = false;
                        return;
                    }
                }
                // Opacity in ADS
                opacityMultiplier = FikaPlugin.OpacityInADS.Value;
            }

            float sqrDistance = (camera.transform.position - currentPlayer.Position).sqrMagnitude;
            float maxDistanceToShow = Mathf.Pow(FikaPlugin.MaxDistanceToShow.Value, 2);
            if (sqrDistance > maxDistanceToShow)
            {
                // Disable the nameplate if the player is too far away
                playerPlate.ScalarObjectScreen.active = false;
                return;
            }
            
            if (playerPlate.ScalarObjectScreen.active == false)
            {
                playerPlate.ScalarObjectScreen.active = true;
            }

            float processedDistance = Mathf.Clamp(sqrDistance / 625, 0.6f, 1f);
            Vector3 position;

            position = new(currentPlayer.PlayerBones.Neck.position.x, currentPlayer.PlayerBones.Neck.position.y + (1f * processedDistance), currentPlayer.PlayerBones.Neck.position.z);

            Vector3 screenPoint = camera.WorldToScreenPoint(position);

            if (CameraClass.Instance.SSAA != null && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                int outputWidth = CameraClass.Instance.SSAA.GetOutputWidth();
                float inputWidth = CameraClass.Instance.SSAA.GetInputWidth();
                screenScale = outputWidth / inputWidth;
            }

            if (screenPoint.z > 0)
            {
                playerPlate.ScalarObjectScreen.transform.position = screenScale < 1 ? screenPoint : screenPoint * screenScale;

                // Less opaque when not looking at the player
                float distFromCenterMultiplier = 1f;
                if (FikaPlugin.DecreaseOpacityNotLookingAt.Value)
                {
                    Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                    float sqrDistFromCenter = (screenCenter - screenPoint).sqrMagnitude;
                    float maxSqrDistFromCenter = Mathf.Pow(Mathf.Min(Screen.width, Screen.height) / 2, 2);
                    distFromCenterMultiplier = Mathf.Clamp01(1 - (sqrDistFromCenter / maxSqrDistFromCenter));
                }

                float alpha = 1f;
                float namePlateScaleMult = FikaPlugin.NamePlateScale.Value;
                float lerpValue = (sqrDistance - maxDistanceToShow / 2) / (maxDistanceToShow / 2);
                alpha = Mathf.Lerp(alpha, 0, lerpValue);
                namePlateScaleMult = Mathf.Lerp(1f, 0.5f, lerpValue);
                namePlateScaleMult = Mathf.Clamp(namePlateScaleMult * FikaPlugin.NamePlateScale.Value, FikaPlugin.MinimumNamePlateScale.Value * FikaPlugin.NamePlateScale.Value, FikaPlugin.NamePlateScale.Value);

                // Setting the nameplate scale
                playerPlate.ScalarObjectScreen.transform.localScale = (Vector3.one / processedDistance) * namePlateScaleMult;

                // Setting the overall nameplate alpha
                alpha *= opacityMultiplier;
                alpha *= distFromCenterMultiplier;
                alpha = Mathf.Max(FikaPlugin.MinimumOpacity.Value, alpha);

                float backgroundOpacity = Mathf.Clamp(alpha, 0f, 0.44f);

                float healthAlphaMultiplier = 1f;
                if (FikaPlugin.HideHealthBar.Value)
                {
                    healthAlphaMultiplier = 0;
                }

                UpdateColorTextMeshProUGUI(playerPlate.playerNameScreen, alpha);
                UpdateColorImage(playerPlate.healthBarScreen, alpha * healthAlphaMultiplier);
                UpdateColorTextMeshProUGUI(playerPlate.healthNumberScreen, alpha * healthAlphaMultiplier);
                UpdateColorImage(playerPlate.healthBarBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
                UpdateColorImage(playerPlate.healthNumberBackgroundScreen, backgroundOpacity * healthAlphaMultiplier);
                UpdateColorImage(playerPlate.usecPlateScreen, alpha);
                UpdateColorImage(playerPlate.bearPlateScreen, alpha);
            }
            else
            {
                // Hide the nameplate if the player is behind the camera
                UpdateColorTextMeshProUGUI(playerPlate.playerNameScreen, 0);
                UpdateColorImage(playerPlate.healthBarScreen, 0);
                UpdateColorTextMeshProUGUI(playerPlate.healthNumberScreen, 0);
                UpdateColorImage(playerPlate.healthBarBackgroundScreen, 0);
                UpdateColorImage(playerPlate.healthNumberBackgroundScreen, 0);
                UpdateColorImage(playerPlate.usecPlateScreen, 0);
                UpdateColorImage(playerPlate.bearPlateScreen, 0);
            }
        }

        private void CreateHealthBar()
        {
            if (currentPlayer != null)
            {
                GameObject uiPrefab = InternalBundleLoader.Instance.GetAssetBundle("playerui").LoadAsset<GameObject>("PlayerFriendlyUI");
                GameObject uiGameObj = Instantiate(uiPrefab);
                playerPlate = uiGameObj.GetComponent<PlayerPlateUI>();
                playerPlate.SetNameText(currentPlayer.Profile.Info.MainProfileNickname);
                if (FikaPlugin.UsePlateFactionSide.Value)
                {
                    if (currentPlayer.Profile.Side == EPlayerSide.Usec)
                    {
                        playerPlate.usecPlateScreen.gameObject.SetActive(true);
                    }
                    else if (currentPlayer.Profile.Side == EPlayerSide.Bear)
                    {
                        playerPlate.bearPlateScreen.gameObject.SetActive(true);
                    }
                }
                if (FikaPlugin.HideHealthBar.Value)
                {
                    playerPlate.healthBarScreen.gameObject.SetActive(false);
                    playerPlate.healthNumberScreen.gameObject.SetActive(false);
                    playerPlate.healthBarBackgroundScreen.gameObject.SetActive(false);
                    playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(false);
                }
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
            }
        }

        private void UpdateHealthBarColor(float normalizedHealth)
        {
            Color color = Color.Lerp(Color.red, Color.green, normalizedHealth);
            color.a = playerPlate.healthBarScreen.color.a; // Keep the alpha value unchanged
            playerPlate.healthBarScreen.color = color;
        }

        private void UpdateColorImage(UnityEngine.UI.Image screenObject, float alpha) 
        {
            if (screenObject.gameObject.activeInHierarchy)
            {
                var color = screenObject.color;
                color.a = alpha;
                screenObject.color = color;
            }
        }
        
        private void UpdateColorTextMeshProUGUI(TMPro.TextMeshProUGUI screenObject, float alpha)
        {
            if (screenObject.gameObject.activeInHierarchy)
            {
                var color = screenObject.color;
                color.a = alpha;
                screenObject.color = color;
            }
        }

        private void OnDestroy()
        {
            playerPlate.gameObject.SetActive(false);
            Destroy(this);
        }
    }
}