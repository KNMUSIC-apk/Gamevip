// ============================================================
// PlayerInput.cs
// Bridges Unity Input System actions → pure data.
// Used by PlayerController and UI. Touch + KB/M + Gamepad unified.
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ProjectAria.Core;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

namespace ProjectAria.Player
{
    public class PlayerInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool BuildModePressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool MapPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public int HotbarSlot { get; private set; } = -1;

        [SerializeField] private InputActionAsset _actions;

        private InputAction _move, _look, _jump, _attack, _dodge, _interact, _buildMode, _sprint, _crouch, _inventory, _map, _pause;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (_actions == null) return;
            _move = _actions.FindAction("Player/Move", true);
            _look = _actions.FindAction("Player/Look", true);
            _jump = _actions.FindAction("Player/Jump", true);
            _attack = _actions.FindAction("Player/Attack", true);
            _dodge = _actions.FindAction("Player/Dodge", true);
            _interact = _actions.FindAction("Player/Interact", true);
            _buildMode = _actions.FindAction("Player/BuildMode", true);
            _sprint = _actions.FindAction("Player/Sprint", true);
            _crouch = _actions.FindAction("Player/Crouch", true);
            _inventory = _actions.FindAction("UI/Inventory", true);
            _map = _actions.FindAction("UI/Map", true);
            _pause = _actions.FindAction("UI/Pause", true);

            _actions.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            _actions?.Disable();
        }

        private void Update()
        {
            Move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            Look = _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;
            JumpHeld = _jump != null && _jump.IsPressed();
            JumpPressed = _jump != null && _jump.WasPressedThisFrame();
            AttackPressed = _attack != null && _attack.WasPressedThisFrame();
            DodgePressed = _dodge != null && _dodge.WasPressedThisFrame();
            InteractPressed = _interact != null && _interact.WasPressedThisFrame();
            BuildModePressed = _buildMode != null && _buildMode.WasPressedThisFrame();
            SprintHeld = _sprint != null && _sprint.IsPressed();
            CrouchHeld = _crouch != null && _crouch.IsPressed();
            InventoryPressed = _inventory != null && _inventory.WasPressedThisFrame();
            MapPressed = _map != null && _map.WasPressedThisFrame();
            PausePressed = _pause != null && _pause.WasPressedThisFrame();
        }

        public void SetHotbarSlot(int slot) => HotbarSlot = slot;
    }
}
