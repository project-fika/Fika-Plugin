using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.UI;
using Fika.Core.Utils;
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
        GameObject prefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.Ping);
        GameObject pingGameObject = UnityEngine.Object.Instantiate(prefab);
        AbstractPing abstractPing = FromPingType(pingType, pingGameObject);
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
                string localizedName = localeId.Localized();
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
        private float _screenScale = 1f;
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
            if (_mainPlayer.HealthController.IsAlive && _mainPlayer.ProceduralWeaponAnimation.IsAiming)
            {
                if (_mainPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic && !FikaPlugin.ShowPingDuringOptics.Value)
                {
                    _image.color = Color.clear;
                    if (_displayRange)
                    {
                        _rangeText.color = Color.clear;
                    }
                    return;
                }
            }

            if (CameraClass.Instance.SSAA != null && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                int outputWidth = CameraClass.Instance.SSAA.GetOutputWidth();
                float inputWidth = CameraClass.Instance.SSAA.GetInputWidth();
                _screenScale = outputWidth / inputWidth;
            }

            /*
			* Positioning based on https://github.com/Omti90/Off-Screen-Target-Indicator-Tutorial/blob/main/Scripts/TargetIndicator.cs
			*/

            if (WorldToScreen.GetScreenPoint(_hitPoint, _mainPlayer, out Vector3 screenPoint, FikaPlugin.PingUseOpticZoom.Value, true))
            {
                float distanceToCenter = Vector3.Distance(screenPoint, new Vector3(Screen.width, Screen.height, 0) / 2);

                if (distanceToCenter < 200)
                {
                    float alpha = Mathf.Max(FikaPlugin.PingMinimumOpacity.Value, distanceToCenter / 200);
                    Color newColor = new(_pingColor.r, _pingColor.g, _pingColor.b, alpha);
                    _image.color = newColor;
                    if (_displayRange)
                    {
                        _rangeText.color = Color.white.SetAlpha(alpha);
                    }
                }
                else
                {
                    _image.color = _pingColor;
                    if (_displayRange)
                    {
                        _rangeText.color = Color.white;
                    }
                }

                if (screenPoint.z >= 0f
                    & screenPoint.x <= _canvasRect.rect.width * _canvasRect.localScale.x
                    & screenPoint.y <= _canvasRect.rect.height * _canvasRect.localScale.x
                    & screenPoint.x >= 0f
                    & screenPoint.y >= 0f)
                {
                    screenPoint.z = 0f;
                    WorldToScreen.TargetOutOfSight(false, screenPoint, _image.rectTransform, _canvasRect);
                }

                else if (screenPoint.z >= 0f)
                {
                    screenPoint = WorldToScreen.OutOfRangeindicatorPositionB(screenPoint, _canvasRect, 20f);
                    WorldToScreen.TargetOutOfSight(true, screenPoint, _image.rectTransform, _canvasRect);
                }
                else
                {
                    screenPoint *= -1f;

                    screenPoint = WorldToScreen.OutOfRangeindicatorPositionB(screenPoint, _canvasRect, 20f);
                    WorldToScreen.TargetOutOfSight(true, screenPoint, _image.rectTransform, _canvasRect);

                }

                _image.transform.position = _screenScale < 1 ? screenPoint : screenPoint * _screenScale;
                if (_displayRange)
                {
                    int distance = (int)CameraClass.Instance.Distance(_hitPoint);
                    _rangeText.text = $"[{distance}m]";
                }
            }
        }

        public virtual void Initialize(ref Vector3 point, Object userObject, Color pingColor)
        {
            _hitPoint = point;
            transform.position = point;
            _pingColor = pingColor;

            float distance = Mathf.Clamp(Vector3.Distance(CameraClass.Instance.Camera.transform.position, transform.position) / 100, 0.4f, 0.6f);
            float pingSize = FikaPlugin.PingSize.Value;
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