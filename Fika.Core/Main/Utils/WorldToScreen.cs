using EFT.Animations;
using EFT.CameraControl;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using UnityEngine;

namespace Fika.Core.Main.Utils
{
    /// <summary>
    /// Class to convert screen space to world space
    /// </summary>
    public static class WorldToScreen
    {
        public static bool GetScreenPoint(Vector3 worldPosition, CoopPlayer mainPlayer, out Vector3 screenPoint, bool useOpticCamera = true, bool skip = false)
        {
            CameraClass worldCameraInstance = CameraClass.Instance;
            Camera worldCamera = worldCameraInstance.Camera;

            screenPoint = Vector3.zero;

            if (mainPlayer == null || worldCamera == null)
            {
                return false;
            }

            ProceduralWeaponAnimation weaponAnimation = mainPlayer.ProceduralWeaponAnimation;

            if (useOpticCamera && weaponAnimation != null)
            {
                if (weaponAnimation.IsAiming && weaponAnimation.CurrentScope.IsOptic)
                {
                    if (GetScopeZoomLevel(weaponAnimation) > 1f)
                    {
                        Camera opticCamera = worldCameraInstance.OpticCameraManager.Camera;

                        float renderScale = worldCameraInstance.SSAA.GetCurrentSSRatio();

                        int width = worldCameraInstance.SSAA.GetInputWidth();
                        int height = worldCameraInstance.SSAA.GetInputHeight();

                        if (renderScale > 1)
                        {
                            width = Screen.width;
                            height = Screen.height;
                        }

                        //get difference between center of optic & center of screen
                        Vector3 opticCenterScreenPosition = GetOpticCenterScreenPosition(weaponAnimation, worldCamera);
                        Vector3 opticCenterScreenOffset = opticCenterScreenPosition - (new Vector3(width, height, 0f) / 2);

                        //worldCamera uses DLSS/FSR/SSAA scaled resolution, opticCamera does not so it must be manually scaled
                        Vector3 opticCameraOffset = new(width / 2 - opticCamera.pixelWidth * renderScale / 2, height / 2 - opticCamera.pixelHeight * renderScale / 2, 0);

                        //must manually scale output of opticCamera.WorldToScreenPoint
                        Vector3 initialOpticScreenPoint = opticCamera.WorldToScreenPoint(worldPosition) * renderScale;

                        //the scaled point on the screen offset for the zoom & position of the optic camera
                        Vector3 opticScreenPoint = (initialOpticScreenPoint + opticCameraOffset);

                        if (opticScreenPoint.z > 0f)
                        {
                            //since optic sways & is not always centered offset the point to compensate
                            screenPoint = opticScreenPoint + opticCenterScreenOffset;
                        }
                    }
                }
            }

            // Not able to find a zoomed optic screen point
            if (screenPoint == Vector3.zero)
            {
                screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            }

            if (screenPoint.z > 0f)
            {
                return true;
            }

            return skip;
        }

        private static float GetScopeZoomLevel(ProceduralWeaponAnimation weaponAnimation)
        {
            SightComponent weaponSight = weaponAnimation.CurrentAimingMod;

            if (weaponSight == null)
            {
                return 1f;
            }

            if (weaponSight.ScopeZoomValue != 0)
            {
                return weaponSight.ScopeZoomValue;
            }

            return weaponSight.GetCurrentOpticZoom();
        }

        private static Vector3 GetOpticCenterScreenPosition(ProceduralWeaponAnimation weaponAnimation, Camera worldCamera)
        {
            if (weaponAnimation == null)
            {
                return Vector3.zero;
            }

            OpticSight currentOptic = CameraClass.Instance.OpticCameraManager.CurrentOpticSight;
            if (currentOptic == null)
            {
                return Vector3.zero;
            }

            Transform lensTransform = currentOptic.LensRenderer.transform;
            return worldCamera.WorldToScreenPoint(lensTransform.position);
        }

        public static void TargetOutOfSight(bool outOfSight, Vector3 indicatorPosition, RectTransform rectTransform, RectTransform canvasRect)
        {
            if (outOfSight)
            {
                rectTransform.rotation = Quaternion.Euler(RotationOutOfSightTargetindicator(indicatorPosition, canvasRect));
            }
            else
            {
                rectTransform.rotation = Quaternion.identity;
            }
        }

        public static Vector3 OutOfRangeindicatorPositionB(Vector3 indicatorPosition, RectTransform canvasRect, float outOfSightOffset)
        {
            indicatorPosition.z = 0f;

            Vector3 canvasCenter = new Vector3(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f, 0f) * canvasRect.localScale.x;
            indicatorPosition -= canvasCenter;

            float divX = (canvasRect.rect.width / 2f - outOfSightOffset) / Mathf.Abs(indicatorPosition.x);
            float divY = (canvasRect.rect.height / 2f - outOfSightOffset) / Mathf.Abs(indicatorPosition.y);

            if (divX < divY)
            {
                float angle = Vector3.SignedAngle(Vector3.right, indicatorPosition, Vector3.forward);
                indicatorPosition.x = Mathf.Sign(indicatorPosition.x) * (canvasRect.rect.width * 0.5f - outOfSightOffset) * canvasRect.localScale.x;
                indicatorPosition.y = Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPosition.x;
            }
            else
            {
                float angle = Vector3.SignedAngle(Vector3.up, indicatorPosition, Vector3.forward);

                indicatorPosition.y = Mathf.Sign(indicatorPosition.y) * (canvasRect.rect.height / 2f - outOfSightOffset) * canvasRect.localScale.y;
                indicatorPosition.x = -Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPosition.y;
            }

            indicatorPosition += canvasCenter;
            return indicatorPosition;
        }

        public static Vector3 RotationOutOfSightTargetindicator(Vector3 indicatorPosition, RectTransform canvasRect)
        {
            Vector3 canvasCenter = new Vector3(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f, 0f) * canvasRect.localScale.x;
            float angle = Vector3.SignedAngle(Vector3.up, indicatorPosition - canvasCenter, Vector3.forward);
            return new Vector3(0f, 0f, angle);
        }
    }
}
