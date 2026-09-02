// ============================================================
// PlayerController.cs
// Character movement + camera + interaction. Mobile-friendly.
// Cinemachine-ready (use CinemachineFreeLook virtual cam).
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Combat;
using ProjectAria.World;

namespace ProjectAria.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public partial class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 4.5f;
        public float SprintSpeed = 7.5f;
        public float CrouchSpeed = 2.5f;
        public float JumpHeight = 1.3f;
        public float Gravity = -20f;
        public float LookSensitivityX = 1f;
        public float LookSensitivityY = 1f;
        public float LookXLimit = 85f;
        public Transform CameraRoot;
        public LayerMask InteractionMask = ~0;
        public float InteractRange = 3.5f;

        public bool InBuildMode { get; private set; }
        public bool InCombatLockOn { get; private set; }

        private CharacterController _cc;
        private PlayerStats _stats;
        private PlayerInput _input;
        private PlayerInteraction _interaction;
        private PlayerInventory _inventory;
        private CombatSystem _combat;

        private Vector3 _velocity;
        private float _camXRot;
        private float _lookYaw;
        private float _lookPitch;
        private Vector3 _lastSafePosition;

        public Vector3 AimPoint { get; private set; }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _stats = GetComponent<PlayerStats>();
            _input = GetComponent<PlayerInput>();
            _interaction = GetComponent<PlayerInteraction>();
            _inventory = GetComponent<PlayerInventory>();
            _combat = GetComponent<CombatSystem>();
            if (CameraRoot == null && Camera.main != null) CameraRoot = Camera.main.transform;
            _lastSafePosition = transform.position;
        }

        private void Update()
        {
            if (_stats == null || !_stats.Alive) return;

            HandleLook();
            HandleMove();
            HandleActions();
            UpdateAim();

            // Save safe position periodically
            if (_cc.isGrounded) _lastSafePosition = transform.position;
        }

        private void HandleLook()
        {
            if (CameraRoot == null) return;
            Vector2 look = _input.Look;
            float sensY = SettingsManager.Current.invertY ? -1f : 1f;
            _lookYaw += look.x * 0.1f * LookSensitivityX * SettingsManager.Current.mouseSensitivity;
            _lookPitch -= look.y * 0.1f * LookSensitivityY * sensY * SettingsManager.Current.mouseSensitivity;
            _lookPitch = Mathf.Clamp(_lookPitch, -LookXLimit, LookXLimit);
            CameraRoot.localRotation = Quaternion.Euler(_lookPitch, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, _lookYaw, 0f);
        }

        private void HandleMove()
        {
            Vector2 m = _input.Move;
            float speed = _input.SprintHeld && _stats.TryUseStamina(0.05f) ? SprintSpeed
                        : _input.CrouchHeld ? CrouchSpeed : WalkSpeed;
            Vector3 inputDir = new Vector3(m.x, 0f, m.y);
            if (CameraRoot != null)
                inputDir = CameraRoot.forward * m.y + CameraRoot.right * m.x;
            inputDir.y = 0f;
            inputDir = inputDir.normalized * speed;

            // Jump
            if (_input.JumpPressed && _cc.isGrounded && _stats.TryUseStamina(8f))
                _velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            // Gravity
            if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f;
            _velocity.y += Gravity * Time.deltaTime;

            Vector3 finalMove = inputDir + new Vector3(0f, _velocity.y, 0f);
            _cc.Move(finalMove * Time.deltaTime);
        }

        private void HandleActions()
        {
            if (_input.AttackPressed && _combat != null) _combat.TryAttack();
            if (_input.DodgePressed && _stats.TryUseStamina(15f) && _combat != null) _combat.TryDodge(transform.forward);
            if (_input.InteractPressed && _interaction != null) _interaction.TryInteract();
            if (_input.BuildModePressed) InBuildMode = !InBuildMode;
            if (_input.HotbarSlot >= 0 && _inventory != null) _inventory.SelectHotbar(_input.HotbarSlot);
        }

        private void UpdateAim()
        {
            if (CameraRoot == null) { AimPoint = transform.position + transform.forward * 5f; return; }
            Ray ray = new Ray(CameraRoot.position, CameraRoot.forward);
            if (Physics.Raycast(ray, out var hit, 50f, InteractionMask))
                AimPoint = hit.point;
            else
                AimPoint = ray.GetPoint(20f);
        }

        public Vector3 GetLastSafePosition() => _lastSafePosition;
        public void Teleport(Vector3 pos) { _cc.enabled = false; transform.position = pos; _cc.enabled = true; }
    }
}
