using EFT.Animations;
using EFT.CameraControl;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using UnityEngine;

namespace Fika.Core.Utils
{
    public static class WorldToScreen
    {
        public static bool GetScreenPoint(Vector3 worldPosition, CoopPlayer mainPlayer, out Vector3 screenPoint)
        {
            CameraClass worldCameraInstance = CameraClass.Instance;
            Camera worldCamera = worldCameraInstance.Camera;

            screenPoint = Vector3.zero;

            if (mainPlayer == null || worldCamera == null)
            {
                return false;
            }

            ProceduralWeaponAnimation weaponAnimation = mainPlayer.ProceduralWeaponAnimation;

            if (weaponAnimation != null)
            {
                if (weaponAnimation.IsAiming && weaponAnimation.CurrentScope.IsOptic)
                {
                    Camera opticCamera = worldCameraInstance.OpticCameraManager.Camera;

                    if (GetScopeZoomLevel(weaponAnimation) > 1f)
                    {
                        Vector3 opticCenterScreenPosition = GetOpticCenterScreenPosition(weaponAnimation, worldCamera);
                        Vector3 opticCenterScreenOffset = opticCenterScreenPosition - (new Vector3(Screen.width, Screen.height, 0f) / 2);

                        float opticScale = (Screen.height / opticCamera.scaledPixelHeight);
                        Vector3 opticCameraOffset = new Vector3((worldCamera.pixelWidth / 2 - opticCamera.pixelWidth / 2), (worldCamera.pixelHeight / 2 - opticCamera.pixelHeight / 2), 0);
                        Vector3 opticScreenPoint = (opticCamera.WorldToScreenPoint(worldPosition) + opticCameraOffset) * opticScale;

                        if (opticScreenPoint.z > 0f)
                        {
                            screenPoint = opticScreenPoint + opticCenterScreenOffset;
                        }
                    }
                }
            }

            //not able to find a zoomed optic screen point
            if (screenPoint == Vector3.zero)
            {
                screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            }

            if (screenPoint.z > 0f)
            {
                return true;
            }

            return false;
        }

        private static float GetScopeZoomLevel(ProceduralWeaponAnimation weaponAnimation)
        {
            SightComponent weaponSight = weaponAnimation.CurrentAimingMod;

            if (weaponSight == null)
            {
                return 1f;
            }

            return weaponSight.GetCurrentOpticZoom();
        }

        private static Vector3 GetOpticCenterScreenPosition(ProceduralWeaponAnimation weaponAnimation, Camera worldCamera)
        {
            if (weaponAnimation == null)
            {
                return Vector3.zero;
            }

            OpticSight currentOptic = weaponAnimation.HandsContainer.Weapon.GetComponentInChildren<OpticSight>();
            if (currentOptic == null)
            {
                return Vector3.zero;
            }

            Transform lensTransform = currentOptic.LensRenderer.transform;
            return worldCamera.WorldToScreenPoint(lensTransform.position);
        }
    }
}
