using EFT.Animations;
using EFT.CameraControl;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.Utils;

/// <summary>
/// Class to convert screen space to world space
/// </summary>
public static class WorldToScreen
{
    public static bool GetScreenPoint(Vector3 worldPosition, FikaPlayer mainPlayer, out Vector3 screenPoint, bool useOpticCamera = true, bool skip = false)
    {
        screenPoint = Vector3.zero;

        CameraClass camClass = CameraClass.Instance;
        Camera worldCamera = camClass.Camera;
        if (mainPlayer == null || worldCamera == null)
        {
            return false;
        }

        ProceduralWeaponAnimation weaponAnim = mainPlayer.ProceduralWeaponAnimation;
        bool opticSuccess = false;

        if (useOpticCamera && IsZoomedOpticAiming(weaponAnim))
        {
            opticSuccess = TryProjectOptic(worldPosition, weaponAnim, camClass, out screenPoint);
        }

        if (!opticSuccess)
        {
            screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
        }

        return screenPoint.z > 0f || skip;
    }

    private static bool IsZoomedOpticAiming(ProceduralWeaponAnimation weaponAnim)
    {
        return weaponAnim != null &&
               weaponAnim.IsAiming &&
               weaponAnim.CurrentScope != null &&
               weaponAnim.CurrentScope.IsOptic &&
               GetScopeZoomLevel(weaponAnim) > 1f;
    }

    private static bool TryProjectOptic(Vector3 worldPosition, ProceduralWeaponAnimation weaponAnim, CameraClass camClass, out Vector3 screenPoint)
    {
        screenPoint = Vector3.zero;

        Camera opticCam = camClass.OpticCameraManager.Camera;
        if (opticCam == null)
        {
            return false;
        }

        float renderScale = camClass.SSAA.GetCurrentSSRatio();
        int width = (renderScale > 1) ? Screen.width : camClass.SSAA.GetInputWidth();
        int height = (renderScale > 1) ? Screen.height : camClass.SSAA.GetInputHeight();

        // offset between optic center and screen center
        Vector3 opticCenterScreenPos = GetOpticCenterScreenPosition(weaponAnim, camClass.Camera);
        Vector3 opticCenterOffset = opticCenterScreenPos - new Vector3(width, height, 0f) / 2f;

        // optic camera output needs manual scaling
        Vector3 opticCamOffset = new(
            width / 2f - opticCam.pixelWidth * renderScale / 2f,
            height / 2f - opticCam.pixelHeight * renderScale / 2f,
            0f
        );

        Vector3 initialOpticScreenPoint = opticCam.WorldToScreenPoint(worldPosition) * renderScale;
        Vector3 opticScreenPoint = initialOpticScreenPoint + opticCamOffset;

        if (opticScreenPoint.z <= 0f)
        {
            return false;
        }

        // compensate for optic sway
        screenPoint = opticScreenPoint + opticCenterOffset;
        return true;
    }

    private static float GetScopeZoomLevel(ProceduralWeaponAnimation weaponAnim)
    {
        SightComponent sight = weaponAnim?.CurrentAimingMod;
        if (sight == null)
        {
            return 1f;
        }

        return (sight.ScopeZoomValue != 0) ? sight.ScopeZoomValue : sight.GetCurrentOpticZoom();
    }

    private static Vector3 GetOpticCenterScreenPosition(ProceduralWeaponAnimation weaponAnim, Camera worldCamera)
    {
        if (weaponAnim == null)
        {
            return Vector3.zero;
        }

        OpticSight optic = CameraClass.Instance.OpticCameraManager.CurrentOpticSight;
        if (optic == null)
        {
            return Vector3.zero;
        }

        Transform lens = optic.LensRenderer.transform;
        return worldCamera.WorldToScreenPoint(lens.position);
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
