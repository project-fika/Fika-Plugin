using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Object = System.Object;

namespace Fika.Core.Main.Factories;

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
        var prefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.Ping);
        var pingGameObject = UnityEngine.Object.Instantiate(prefab);
        var abstractPing = FromPingType(pingType, pingGameObject);
        if (abstractPing != null)
        {
            abstractPing.Initialize(ref location, null, pingColor);
            Singleton<GUISounds>.Instance.PlayUISound(GetPingSound());
            if (string.IsNullOrEmpty(localeId))
            {
                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVE_PING.Localized(), FikaUIGlobals.ColorizeText(FikaUIGlobals.EColor.GREEN, nickname)),
                            ENotificationDurationType.Default, ENotificationIconType.Friend);
            }
            else
            {
                var localizedName = localeId.Localized();
                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.RECEIVE_PING_OBJECT.Localized(),
                    [FikaUIGlobals.ColorizeText(FikaUIGlobals.EColor.GREEN, nickname), FikaUIGlobals.ColorizeText(FikaUIGlobals.EColor.BLUE, localizedName)]),
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
        //internal static readonly AssetBundle pingBundle;
        internal static readonly Dictionary<InternalBundleLoader.EFikaSprite, Sprite> sprites;

        protected Image _image;
        protected Vector3 _hitPoint;
        private RectTransform _canvasRect;
        private TextMeshProUGUI _rangeText;
        private bool _displayRange;
        private Color _pingColor = Color.white;
        private FikaPlayer _mainPlayer;

        static AbstractPing()
        {
            sprites = InternalBundleLoader.Instance.GetFikaSprites();
        }

        protected void Awake()
        {
            _image = GetComponentInChildren<Image>();
            _image.color = Color.clear;
            _mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            _canvasRect = GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
            _rangeText = GetComponentInChildren<TextMeshProUGUI>(true);
            _rangeText.color = Color.clear;
            _displayRange = FikaPlugin.ShowPingRange.Value;
            _rangeText.gameObject.SetActive(_displayRange);
            if (_mainPlayer == null)
            {
                Destroy(gameObject);
                FikaPlugin.Instance.FikaLogger.LogError("Ping::Awake: Could not find MainPlayer!");
            }
            Destroy(gameObject, FikaPlugin.PingTime.Value);
        }

        protected void Update()
        {
            if (_mainPlayer.HealthController.IsAlive && _mainPlayer.ProceduralWeaponAnimation.IsAiming
                && _mainPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic && !FikaPlugin.ShowPingDuringOptics.Value)
            {
                _image.color = Color.clear;
                if (_displayRange)
                {
                    _rangeText.color = Color.clear;
                }

                return;
            }

            const float edgePadding = 20f;
            var cam = CameraClass.Instance.Camera;
            var targetPos = _hitPoint;

            // vector from camera to target
            var toTarget = targetPos - cam.transform.position;
            var behindCamera = Vector3.Dot(cam.transform.forward, toTarget.normalized) <= 0f;

            // project normally if in front
            Vector2 canvasPos;
            if (!behindCamera)
            {
                WorldToScreen.ProjectToCanvas(targetPos, _mainPlayer, _canvasRect, out canvasPos,
                    FikaPlugin.PingUseOpticZoom.Value, true);
            }
            else
            {
                // compute direction in camera plane if behind
                var local = cam.transform.InverseTransformPoint(targetPos);
                var dir = new Vector2(local.x, local.y); // X=horizontal, Y=vertical
                if (dir.sqrMagnitude < 0.001f)
                {
                    dir = Vector2.up; // fallback to avoid zero vector
                }

                dir.Normalize();

                // clamp to canvas edge
                var halfWidth = (_canvasRect.sizeDelta.x / 2f) - edgePadding;
                var halfHeight = (_canvasRect.sizeDelta.y / 2f) - edgePadding;

                // scale so the ping sits on the edge
                var scaleX = halfWidth / Mathf.Abs(dir.x);
                var scaleY = halfHeight / Mathf.Abs(dir.y);
                var scale = Mathf.Min(scaleX, scaleY);

                canvasPos = dir * scale;
            }

            // distance-based alpha
            var distanceToCenter = canvasPos.magnitude;
            var alpha = distanceToCenter < 200f
                ? Mathf.Max(FikaPlugin.PingMinimumOpacity.Value, distanceToCenter / 200f)
                : 1f;

            _image.color = new Color(_pingColor.r, _pingColor.g, _pingColor.b, alpha);
            if (_displayRange)
            {
                _rangeText.color = Color.white.SetAlpha(alpha);
            }

            // clamp to canvas edges (safety)
            var halfW = (_canvasRect.sizeDelta.x / 2f) - edgePadding;
            var halfH = (_canvasRect.sizeDelta.y / 2f) - edgePadding;
            canvasPos.x = Mathf.Clamp(canvasPos.x, -halfW, halfW);
            canvasPos.y = Mathf.Clamp(canvasPos.y, -halfH, halfH);

            _image.rectTransform.anchoredPosition = canvasPos;

            if (_displayRange)
            {
                _rangeText.text = $"[{CameraClass.Instance.Distance(_hitPoint):F1}m]";
            }
        }

        public virtual void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            _hitPoint = point;
            transform.position = point;
            _pingColor = pingColor;

            var distance = Mathf.Clamp(Vector3.Distance(CameraClass.Instance.Camera.transform.position, transform.position) / 100, 0.4f, 0.6f);
            var pingSize = FikaPlugin.PingSize.Value;
            Vector3 scaledSize = new(pingSize, pingSize, pingSize);
            if (FikaPlugin.PingScaleWithDistance.Value)
            {
                scaledSize *= distance;
            }
            else
            {
                scaledSize *= 0.5f;
            }
            _image.rectTransform.localScale = scaledSize;
        }
    }

    public class InteractablePing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            _image.sprite = sprites[InternalBundleLoader.EFikaSprite.PingPoint];
        }
    }

    public class PlayerPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            _image.sprite = sprites[InternalBundleLoader.EFikaSprite.PingPlayer];
        }
    }

    public class LootContainerPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            _image.sprite = sprites[InternalBundleLoader.EFikaSprite.PingLootableContainer];
        }
    }

    public class DoorPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            _image.sprite = sprites[InternalBundleLoader.EFikaSprite.PingDoor];
        }
    }

    public class PointPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            _image.sprite = sprites[InternalBundleLoader.EFikaSprite.PingPoint];
        }
    }

    public class DeadBodyPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, Color.white); // White since this icon is already red...
            transform.localScale *= 0.5f;
            _image.sprite = sprites[InternalBundleLoader.EFikaSprite.PingDeadBody];
        }
    }

    public class LootItemPing : AbstractPing
    {
        public override void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            base.Initialize(ref point, userObject, pingColor);
            _image.sprite = sprites[InternalBundleLoader.EFikaSprite.PingLootItem];
        }
    }
}