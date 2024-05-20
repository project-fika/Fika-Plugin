using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.InventoryLogic;
using Fika.Core.Bundles;
using Fika.Core.Coop.Players;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Fika.Core.Coop.Factories;

public static class PingFactory
{
    public enum EPingType : byte
    {
        Point,
        Player,
        DeadBody,
        LootItem,
        LootContainer,
        Door,
        Interactable
    }

    public static AbstractPing FromPingType(EPingType type, GameObject gameObject)
    {
        return type switch
        {
            EPingType.Point => gameObject.AddComponent<PointPing>(),
            EPingType.Player => gameObject.AddComponent<PlayerPing>(),
            EPingType.DeadBody => gameObject.AddComponent<DeadBodyPing>(),
            EPingType.LootItem => gameObject.AddComponent<LootItemPing>(),
            EPingType.LootContainer => gameObject.AddComponent<LootContainerPing>(),
            EPingType.Door => gameObject.AddComponent<DoorPing>(),
            EPingType.Interactable => gameObject.AddComponent<InteractablePing>(),
            _ => null
        };
    }

    public abstract class AbstractPing : MonoBehaviour
    {
        internal static readonly AssetBundle pingBundle;

        protected Image image;
        protected Vector3 hitPoint;
        private float screenScale = 1f;
        private Color _pingColor = Color.white;
        private CoopPlayer mainPlayer;

        static AbstractPing()
        {
            pingBundle = InternalBundleLoader.Instance.GetAssetBundle("ping");
        }

        protected void Awake()
        {
            image = GetComponentInChildren<Image>();
            image.color = Color.clear;
            mainPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                Destroy(gameObject);
                FikaPlugin.Instance.FikaLogger.LogError("Ping::Awake: Could not find MainPlayer!");
            }
            Destroy(gameObject, FikaPlugin.PingTime.Value);
        }

        protected void Update()
        {
            if (mainPlayer.HealthController.IsAlive && mainPlayer.ProceduralWeaponAnimation.IsAiming)
            {
                if (mainPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic && !FikaPlugin.ShowPingDuringOptics.Value)
                {
                    image.color = Color.clear;
                    return;
                }
            }

            Camera camera = CameraClass.Instance.Camera;
            Vector3 screenPoint = Vector3.zero;

            if (mainPlayer.ProceduralWeaponAnimation.IsAiming && mainPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic)
            {
                //SightComponent currentSightComponent = mainPlayer.ProceduralWeaponAnimation.CurrentScope.Mod;
                SightComponent currentSightComponent = mainPlayer.ProceduralWeaponAnimation.CurrentAimingMod;

                if (currentSightComponent != null)
                {
                    float opticZoomLevel = currentSightComponent.GetCurrentOpticZoom();

                    //only calculate optic screen point if zoom is higher than 1
                    if (opticZoomLevel > 1.0f)
                    {
                        Camera opticCamera = CameraClass.Instance.OpticCameraManager.Camera;

                        if (opticCamera != null)
                        {
                            //calculate where the optic is relative to the center of the screen due to weapon sway
                            OpticSight currentOptic = mainPlayer.ProceduralWeaponAnimation.HandsContainer.Weapon.GetComponentInChildren<OpticSight>();

                            if (currentOptic != null)
                            {
                                Vector3 opticCenterScreenPosition = camera.WorldToScreenPoint(currentOptic.LensRenderer.transform.position);
                                Vector3 opticCenterScreenOffset = opticCenterScreenPosition - (new Vector3(Screen.width, Screen.height, 0) / 2);

                                //TO-DO?: calculate optic radius & don't use optic camera to render if distance from middle of screen is > radius

                                //calculate where the zoomed in camera & world camera correspond to each other
                                float opticScale = Screen.height / opticCamera.scaledPixelHeight;
                                Vector3 opticCameraOffset = new Vector3(x: (camera.pixelWidth / 2 - opticCamera.pixelWidth / 2), y: (camera.pixelHeight / 2 - opticCamera.pixelHeight / 2), z: 0);
                                Vector3 scopeScreenPoint = (opticCamera.WorldToScreenPoint(hitPoint) + opticCameraOffset) * opticScale;

                                screenPoint = scopeScreenPoint + opticCenterScreenOffset;
                                //screenPoint = scopeScreenPoint;
                            }
                        }
                    }
                }
            }

            //no optic, not aiming, or optic is not zoomed (thus no pip camera component)
            if (screenPoint ==  Vector3.zero)
            {
                screenPoint = camera.WorldToScreenPoint(hitPoint);
            }

            if (CameraClass.Instance.SSAA != null && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                int outputWidth = CameraClass.Instance.SSAA.GetOutputWidth();
                float inputWidth = CameraClass.Instance.SSAA.GetInputWidth();
                screenScale = outputWidth / inputWidth;
            }

            if (screenPoint.z > 0)
            {
                float distanceToCenter = Vector3.Distance(screenPoint, new Vector3(x: Screen.width, y: Screen.height, 0) / 2);

                if (distanceToCenter < 200)
                {
                    image.color = new Color(_pingColor.r, _pingColor.g, _pingColor.b, Mathf.Max(0.05f, distanceToCenter / 200));
                }
                else
                {
                    image.color = _pingColor;
                }

                image.transform.position = screenScale < 1 ? screenPoint : screenPoint * screenScale;
            }
        }

        public virtual void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            hitPoint = point;
            transform.position = point;
            _pingColor = pingColor;

            float distance = Mathf.Clamp(Vector3.Distance(CameraClass.Instance.Camera.transform.position, transform.position) / 100, 0.4f, 0.6f);
            float pingSize = FikaPlugin.PingSize.Value;
            image.rectTransform.localScale = new Vector3(pingSize, pingSize, pingSize) * distance;
        }
    }

    public class InteractablePing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            image.sprite = pingBundle.LoadAsset<Sprite>("PingPoint");
        }
    }

    public class PlayerPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            //Player player = (Player)userObject;
            image.sprite = pingBundle.LoadAsset<Sprite>("PingPlayer");
        }
    }

    public class LootContainerPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            //LootableContainer lootableContainer = userObject as LootableContainer;
            image.sprite = pingBundle.LoadAsset<Sprite>("PingLootableContainer");
        }
    }

    public class DoorPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            image.sprite = pingBundle.LoadAsset<Sprite>("PingDoor");
        }
    }

    public class PointPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            image.sprite = pingBundle.LoadAsset<Sprite>("PingPoint");
        }
    }

    public class DeadBodyPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, Color.white); // White since this icon is already red...
            transform.localScale *= 0.5f;
            image.sprite = pingBundle.LoadAsset<Sprite>("PingDeadBody");
        }
    }

    public class LootItemPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            //LootItem lootItem = userObject as LootItem;
            image.sprite = pingBundle.LoadAsset<Sprite>("PingLootItem");
        }
    }
}