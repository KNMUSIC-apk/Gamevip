// ============================================================
// SmartActionButton.cs
// Context-sensitive action button. Auto-swaps label/icon based on what's
// in front: Attack / Interact / Mine / Build / Talk / Pickup / Open.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectAria.Core;
using ProjectAria.Player;
using ProjectAria.World;

namespace ProjectAria.Controls
{
    public enum SmartAction
    {
        None, Attack, Interact, Mine, Build, Talk, Pickup, Open, Use, Cast
    }

    public class SmartActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image IconImage;
        public Text LabelText;
        public Sprite AttackIcon, InteractIcon, MineIcon, BuildIcon, TalkIcon, PickupIcon, OpenIcon, UseIcon;
        public Color DefaultColor = Color.white;
        public Color PressedColor = new(0.7f, 0.7f, 0.7f, 1f);

        public SmartAction Current { get; private set; } = SmartAction.None;
        public event System.Action OnPressed;

        private Image _bg;
        private bool _down;

        private void Awake()
        {
            _bg = GetComponent<Image>();
        }

        private void Update()
        {
            // Each frame, determine context. Cheap raycast.
            if (Camera.main == null) return;
            Ray ray = new(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out var hit, 5f))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.Alive && hit.collider.GetComponentInParent<Combat.Enemy>() != null)
                {
                    Set(SmartAction.Attack, "Attack", AttackIcon);
                    return;
                }
                var interact = hit.collider.GetComponentInParent<IInteractable>();
                if (interact != null)
                {
                    string label = interact.DisplayName;
                    if (interact is NPC.NPCController) Set(SmartAction.Talk, "Talk", TalkIcon);
                    else Set(SmartAction.Interact, "Open", OpenIcon);
                    return;
                }
                var block = hit.collider.GetComponentInParent<Chunk>();
                if (block != null)
                {
                    Set(SmartAction.Mine, "Mine", MineIcon);
                    return;
                }
            }
            Set(SmartAction.None, "", null);
        }

        private void Set(SmartAction action, string label, Sprite icon)
        {
            Current = action;
            if (LabelText != null) LabelText.text = label;
            if (IconImage != null)
            {
                IconImage.sprite = icon;
                IconImage.enabled = icon != null;
            }
        }

        public void OnPointerDown(PointerEventData e)
        {
            _down = true;
            if (_bg != null) _bg.color = PressedColor;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_down) return;
            _down = false;
            if (_bg != null) _bg.color = DefaultColor;
            if (Current != SmartAction.None) OnPressed?.Invoke();
        }
    }
}
