using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Players;
using Fika.Core.Utils;
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

    public static void ReceivePing(Vector3 location, EPingType pingType, Color pingColor, string nickname, string localeId)
    {
        GameObject prefab = AbstractPing.pingBundle.LoadAsset<GameObject>("BasePingPrefab");
        GameObject pingGameObject = UnityEngine.Object.Instantiate(prefab);
        AbstractPing abstractPing = FromPingType(pingType, pingGameObject);
        if (abstractPing != null)
        {
            abstractPing.Initialize(ref location, null, pingColor);
            Singleton<GUISounds>.Instance.PlayUISound(GetPingSound());
            if (string.IsNullOrEmpty(localeId))
            {
                NotificationManagerClass.DisplayMessageNotification($"Received a ping from {ColorUtils.ColorizeText(Colors.GREEN, nickname)}",
                            ENotificationDurationType.Default, ENotificationIconType.Friend);
            }
            else
            {
                string localizedName = localeId.Localized();
                NotificationManagerClass.DisplayMessageNotification($"{ColorUtils.ColorizeText(Colors.GREEN, nickname)} has pinged {LocaleUtils.GetPrefix(localizedName)} {ColorUtils.ColorizeText(Colors.BLUE, localizedName)}",
                            ENotificationDurationType.Default, ENotificationIconType.Friend);
            }
        }
        else
        {
            FikaPlugin.Instance.FikaLogger.LogError($"Received {pingType} from {nickname} but factory failed to handle it");
        }
    }

    public static EUISoundType GetPingSound()
    {
        return FikaPlugin.PingSound.Value switch
        {
            FikaPlugin.EPingSound.InsuranceInsured => EUISoundType.InsuranceInsured,
            FikaPlugin.EPingSound.SubQuestComplete => EUISoundType.QuestSubTrackComplete,
            FikaPlugin.EPingSound.ButtonClick => EUISoundType.ButtonClick,
            FikaPlugin.EPingSound.ButtonHover => EUISoundType.ButtonOver,
            FikaPlugin.EPingSound.InsuranceItemInsured => EUISoundType.InsuranceItemOnInsure,
            FikaPlugin.EPingSound.MenuButtonBottom => EUISoundType.ButtonBottomBarClick,
            FikaPlugin.EPingSound.ErrorMessage => EUISoundType.ErrorMessage,
            FikaPlugin.EPingSound.InspectWindow => EUISoundType.MenuInspectorWindowOpen,
            FikaPlugin.EPingSound.InspectWindowClose => EUISoundType.MenuInspectorWindowClose,
            FikaPlugin.EPingSound.MenuEscape => EUISoundType.MenuEscape,
            _ => EUISoundType.QuestSubTrackComplete,
        };
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

            if (CameraClass.Instance.SSAA != null && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                int outputWidth = CameraClass.Instance.SSAA.GetOutputWidth();
                float inputWidth = CameraClass.Instance.SSAA.GetInputWidth();
                screenScale = outputWidth / inputWidth;
            }

            if (WorldToScreen.GetScreenPoint(hitPoint, mainPlayer, out Vector3 screenPoint, FikaPlugin.PingUseOpticZoom.Value))
            {
                float distanceToCenter = Vector3.Distance(screenPoint, new Vector3(Screen.width, Screen.height, 0) / 2);

                if (distanceToCenter < 200)
                {
                    image.color = new Color(_pingColor.r, _pingColor.g, _pingColor.b, Mathf.Max(FikaPlugin.PingMinimumOpacity.Value, distanceToCenter / 200));
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
            Vector3 scaledSize = new(pingSize, pingSize, pingSize);
            if (FikaPlugin.PingScaleWithDistance.Value == true)
            {
                scaledSize *= distance;
            }
            else
            {
                scaledSize *= 0.5f;
            }
            image.rectTransform.localScale = scaledSize;
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