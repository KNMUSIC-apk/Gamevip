// ============================================================
// MobileButton.cs
// Generic mobile button with optional hold behavior and visual feedback.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectAria.Core;

namespace ProjectAria.Controls
{
    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image Icon;
        public Color DefaultColor = Color.white;
        public Color PressedColor = new(0.7f, 0.7f, 0.7f, 1f);
        public bool HoldMode = false;
        public bool IsPressed { get; private set; }
        public event System.Action OnPressed;
        public event System.Action OnReleased;
        public event System.Action OnClicked;

        private Image _bg;

        private void Awake()
        {
            _bg = GetComponent<Image>();
        }

        public void OnPointerDown(PointerEventData e)
        {
            IsPressed = true;
            if (_bg != null) _bg.color = PressedColor;
            OnPressed?.Invoke();
            if (!HoldMode) OnClicked?.Invoke();
        }

        public void OnPointerUp(PointerEventData e)
        {
            IsPressed = false;
            if (_bg != null) _bg.color = DefaultColor;
            OnReleased?.Invoke();
            if (HoldMode) OnClicked?.Invoke();
        }
    }
}
