// ============================================================
// VirtualJoystick.cs
// Touch-driven virtual joystick. Drag from center, output [-1,1].
// Customizable size, opacity, position. Mobile-first.
// ============================================================
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectAria.Core;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

namespace ProjectAria.Controls
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Handle;
        public RectTransform Background;
        public float Radius = 120f;
        public bool Dynamic = true; // if true, joystick appears at touch position

        public Vector2 Value { get; private set; }
        public bool Active { get; private set; }

        private CanvasGroup _bgGroup;
        private CanvasGroup _handleGroup;
        private RectTransform _rect;
        private int _pointerId = -1;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (Background == null) Background = _rect;
            if (Handle == null && transform.childCount > 0) Handle = transform.GetChild(0) as RectTransform;
            _bgGroup = Background.GetComponent<CanvasGroup>() ?? Background.gameObject.AddComponent<CanvasGroup>();
            _handleGroup = Handle.GetComponent<CanvasGroup>() ?? Handle.gameObject.AddComponent<CanvasGroup>();
            _bgGroup.alpha = SettingsManager.Current.joystickOpacity;
            _handleGroup.alpha = SettingsManager.Current.joystickOpacity;
            _bgGroup.blocksRaycasts = false;
            _handleGroup.blocksRaycasts = false;
            CenterKnob();
        }

        private void OnEnable() { EnhancedTouchSupport.Enable(); }
        private void OnDisable() { EnhancedTouchSupport.Disable(); Active = false; Value = Vector2.zero; }

        public void OnPointerDown(PointerEventData e)
        {
            if (Active) return;
            Active = true;
            _pointerId = e.pointerId;
            if (Dynamic)
            {
                _rect.position = e.position;
            }
            CenterKnob();
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Active) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Background, e.position, e.pressEventCamera, out local);
            Vector2 dir = local;
            float mag = dir.magnitude;
            if (mag > Radius) dir = dir.normalized * Radius;
            Handle.anchoredPosition = dir;
            Value = dir / Radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId && _pointerId != -1) return;
            Active = false;
            _pointerId = -1;
            CenterKnob();
            Value = Vector2.zero;
        }

        private void CenterKnob()
        {
            if (Handle != null) Handle.anchoredPosition = Vector2.zero;
        }

        public void SetOpacity(float alpha)
        {
            if (_bgGroup != null) _bgGroup.alpha = alpha;
            if (_handleGroup != null) _handleGroup.alpha = alpha;
        }

        public void SetScale(float scale)
        {
            _rect.localScale = new Vector3(scale, scale, 1f);
        }

        public void SetPosition(Vector2 screenPos)
        {
            _rect.position = screenPos;
        }
    }
}
