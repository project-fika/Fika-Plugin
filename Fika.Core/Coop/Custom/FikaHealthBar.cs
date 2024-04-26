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
        private FikaPlayerPlateUI playerPlate;
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
            if (mainPlayer.HealthController.IsAlive && mainPlayer.ProceduralWeaponAnimation.IsAiming)
            {
                if (mainPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic)
                {
                    playerPlate.ScalarObjectScreen.active = false;
                    return;
                }
            }
            else if (playerPlate.ScalarObjectScreen.active == false)
            {
                playerPlate.ScalarObjectScreen.active = true;
            }

            Camera camera = CameraClass.Instance.Camera;

            if (CameraClass.Instance.SSAA != null && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                int outputWidth = CameraClass.Instance.SSAA.GetOutputWidth();
                float inputWidth = CameraClass.Instance.SSAA.GetInputWidth();
                screenScale = outputWidth / inputWidth;
            }

            float distance = Vector3.Distance(CameraClass.Instance.Camera.transform.position, currentPlayer.Position) / 25;
            distance = Mathf.Clamp(distance, 0.6f, 1f);
            Vector3 position;

            position = new(currentPlayer.PlayerBones.Neck.position.x, currentPlayer.PlayerBones.Neck.position.y + (1f * distance), currentPlayer.PlayerBones.Neck.position.z);

            Vector3 screenPoint = camera.WorldToScreenPoint(position);

            if (screenPoint.z > 0)
            {
                playerPlate.ScalarObjectScreen.transform.position = screenScale < 1 ? screenPoint : screenPoint * screenScale;
                playerPlate.ScalarObjectScreen.transform.localScale = (Vector3.one / distance) * FikaPlugin.NamePlateScale.Value;

                float distanceToCenter = Vector3.Distance(screenPoint, new Vector3(Screen.width, Screen.height, 0) / 2);

                #region Alpha Control for Health Bars. This code is ugly so its getting regioned so I can hide my shame.
                if (distanceToCenter < 200)
                {
                    playerPlate.playerNameScreen.color = new Color(playerPlate.playerNameScreen.color.r, playerPlate.playerNameScreen.color.g, playerPlate.playerNameScreen.color.b, Mathf.Max(0.1f, distanceToCenter / 200));
                    playerPlate.healthBarScreen.color = new Color(playerPlate.healthBarScreen.color.r, playerPlate.healthBarScreen.color.g, playerPlate.healthBarScreen.color.b, Mathf.Max(0.1f, distanceToCenter / 200));
                    playerPlate.healthBarBackgroundScreen.color = new Color(playerPlate.healthBarBackgroundScreen.color.r, playerPlate.healthBarBackgroundScreen.color.g, playerPlate.healthBarBackgroundScreen.color.b, Mathf.Clamp(Mathf.Max(0.1f, distanceToCenter / 200), 0f, 0.4392157f));
                    playerPlate.healthNumberBackgroundScreen.color = new Color(playerPlate.healthNumberBackgroundScreen.color.r, playerPlate.healthNumberBackgroundScreen.color.g, playerPlate.healthNumberBackgroundScreen.color.b, Mathf.Clamp(Mathf.Max(0.1f, distanceToCenter / 200), 0f, 0.4392157f));
                    playerPlate.healthNumberScreen.color = new Color(playerPlate.healthNumberScreen.color.r, playerPlate.healthNumberScreen.color.g, playerPlate.healthNumberScreen.color.b, Mathf.Max(0.1f, distanceToCenter / 200));
                    playerPlate.usecPlateScreen.color = new Color(playerPlate.usecPlateScreen.color.r, playerPlate.usecPlateScreen.color.g, playerPlate.usecPlateScreen.color.b, Mathf.Max(0.1f, distanceToCenter / 200));
                    playerPlate.bearPlateScreen.color = new Color(playerPlate.bearPlateScreen.color.r, playerPlate.bearPlateScreen.color.g, playerPlate.bearPlateScreen.color.b, Mathf.Max(0.1f, distanceToCenter / 200));

                }
                else
                {
                    playerPlate.playerNameScreen.color = new Color(playerPlate.playerNameScreen.color.r, playerPlate.playerNameScreen.color.g, playerPlate.playerNameScreen.color.b, 1);
                    playerPlate.healthBarScreen.color = new Color(playerPlate.healthBarScreen.color.r, playerPlate.healthBarScreen.color.g, playerPlate.healthBarScreen.color.b, 1);
                    playerPlate.healthBarBackgroundScreen.color = new Color(playerPlate.healthBarBackgroundScreen.color.r, playerPlate.healthBarBackgroundScreen.color.g, playerPlate.healthBarBackgroundScreen.color.b, 1);
                    playerPlate.healthNumberBackgroundScreen.color = new Color(playerPlate.healthNumberBackgroundScreen.color.r, playerPlate.healthNumberBackgroundScreen.color.g, playerPlate.healthNumberBackgroundScreen.color.b, 1);
                    playerPlate.healthNumberScreen.color = new Color(playerPlate.healthNumberScreen.color.r, playerPlate.healthNumberScreen.color.g, playerPlate.healthNumberScreen.color.b, 1);
                    playerPlate.usecPlateScreen.color = new Color(playerPlate.usecPlateScreen.color.r, playerPlate.usecPlateScreen.color.g, playerPlate.usecPlateScreen.color.b, 1);
                    playerPlate.bearPlateScreen.color = new Color(playerPlate.bearPlateScreen.color.r, playerPlate.bearPlateScreen.color.g, playerPlate.bearPlateScreen.color.b, 1);
                }
            }
            else
            {
                playerPlate.playerNameScreen.color = new Color(playerPlate.playerNameScreen.color.r, playerPlate.playerNameScreen.color.g, playerPlate.playerNameScreen.color.b, 0);
                playerPlate.healthBarScreen.color = new Color(playerPlate.healthBarScreen.color.r, playerPlate.healthBarScreen.color.g, playerPlate.healthBarScreen.color.b, 0);
                playerPlate.healthBarBackgroundScreen.color = new Color(playerPlate.healthBarBackgroundScreen.color.r, playerPlate.healthBarBackgroundScreen.color.g, playerPlate.healthBarBackgroundScreen.color.b, 0);
                playerPlate.healthNumberBackgroundScreen.color = new Color(playerPlate.healthNumberBackgroundScreen.color.r, playerPlate.healthNumberBackgroundScreen.color.g, playerPlate.healthNumberBackgroundScreen.color.b, 0);
                playerPlate.healthNumberScreen.color = new Color(playerPlate.healthNumberScreen.color.r, playerPlate.healthNumberScreen.color.g, playerPlate.healthNumberScreen.color.b, 0);
                playerPlate.usecPlateScreen.color = new Color(playerPlate.usecPlateScreen.color.r, playerPlate.usecPlateScreen.color.g, playerPlate.usecPlateScreen.color.b, 0);
                playerPlate.bearPlateScreen.color = new Color(playerPlate.bearPlateScreen.color.r, playerPlate.bearPlateScreen.color.g, playerPlate.bearPlateScreen.color.b, 0);
            }
            #endregion
        }

        private void CreateHealthBar()
        {
            if (currentPlayer != null)
            {
                GameObject uiPrefab = InternalBundleLoader.Instance.GetAssetBundle("playerui").LoadAsset<GameObject>("PlayerFriendlyUI");
                GameObject uiGameObj = Instantiate(uiPrefab);
                playerPlate = uiGameObj.GetComponent<FikaPlayerPlateUI>();
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

        private void OnDestroy()
        {
            playerPlate.gameObject.SetActive(false);
            Destroy(this);
        }
    }
}