// ============================================================
// MobileControlsUI.cs
// Bridges UI buttons to PlayerInput. Handles layout (default/left-handed/custom).
// Players can drag/resize/transparent each control via Settings panel.
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAria.Core;
using ProjectAria.Player;
using ProjectAria.Inventory;
using ProjectAria.Building;

namespace ProjectAria.Controls
{
    public class MobileControlsUI : MonoBehaviour
    {
        public VirtualJoystick Joystick;
        public SmartActionButton SmartAction;
        public MobileButton AttackButton, JumpButton, DodgeButton, InteractButton, BuildButton, UseButton;
        public HotbarUI Hotbar;
        public RectTransform LeftSideRoot, RightSideRoot;
        public RectTransform ControlLayoutRoot;
        public PlayerInput PlayerInput;
        public PlayerController Player;
        public PlayerInventory PlayerInv;
        public BuildingSystem Building;

        private void OnEnable()
        {
            if (SmartAction != null) SmartAction.OnPressed += OnSmartAction;
            if (AttackButton != null) AttackButton.OnPressed += () => { if (PlayerInput != null) ((MonoBehaviour)PlayerInput).SendMessage("SimulateAttack", SendMessageOptions.DontRequireReceiver); };
            if (JumpButton != null) JumpButton.OnPressed += () => { if (Player != null) Player.JumpRequest(); };
            if (DodgeButton != null) DodgeButton.OnPressed += () => { if (Player != null) Player.DodgeRequest(); };
            if (InteractButton != null) InteractButton.OnPressed += () => { if (PlayerInput != null) ((MonoBehaviour)PlayerInput).SendMessage("SimulateInteract", SendMessageOptions.DontRequireReceiver); };
            if (BuildButton != null) BuildButton.OnPressed += () => { if (Building != null) Building.EnterBuildMode(); };
            if (UseButton != null) UseButton.OnPressed += () => { if (Player != null) Player.UseHeldItem(); };

            ApplyLayout();
        }

        private void OnDisable()
        {
            if (SmartAction != null) SmartAction.OnPressed -= OnSmartAction;
        }

        private void OnSmartAction()
        {
            switch (SmartAction.Current)
            {
                case SmartAction.Attack: if (PlayerInput != null) ((MonoBehaviour)PlayerInput).SendMessage("SimulateAttack"); break;
                case SmartAction.Mine: if (Player != null) Player.TryMine(); break;
                case SmartAction.Interact: if (PlayerInput != null) ((MonoBehaviour)PlayerInput).SendMessage("SimulateInteract"); break;
                case SmartAction.Talk: if (PlayerInput != null) ((MonoBehaviour)PlayerInput).SendMessage("SimulateInteract"); break;
                case SmartAction.Build: if (Building != null) Building.EnterBuildMode(); break;
            }
        }

        public void ApplyLayout()
        {
            var layout = SettingsManager.Current.controlLayout;
            bool leftHanded = layout == ControlLayout.LeftHanded;
            if (Joystick != null && Joystick.transform.parent != (leftHanded ? RightSideRoot : LeftSideRoot))
                Joystick.transform.SetParent(leftHanded ? RightSideRoot : LeftSideRoot, false);
            if (SmartAction != null && SmartAction.transform.parent != (leftHanded ? LeftSideRoot : RightSideRoot))
                SmartAction.transform.SetParent(leftHanded ? LeftSideRoot : RightSideRoot, false);

            // Apply opacity
            if (Joystick != null) Joystick.SetOpacity(SettingsManager.Current.joystickOpacity);
            float btnOp = SettingsManager.Current.buttonOpacity;
            float sc = SettingsManager.Current.buttonScale;
            SetButtonOp(AttackButton, btnOp, sc);
            SetButtonOp(JumpButton, btnOp, sc);
            SetButtonOp(DodgeButton, btnOp, sc);
            SetButtonOp(InteractButton, btnOp, sc);
            SetButtonOp(BuildButton, btnOp, sc);
            SetButtonOp(UseButton, btnOp, sc);
            SetButtonOp(SmartAction, btnOp, sc);
        }

        private void SetButtonOp(MobileButton b, float op, float sc)
        {
            if (b == null) return;
            var cg = b.GetComponent<CanvasGroup>();
            if (cg == null) cg = b.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = op;
            b.transform.localScale = new Vector3(sc, sc, 1f);
        }

        public void ToggleLeftHanded()
        {
            SettingsManager.Current.controlLayout = SettingsManager.Current.controlLayout == ControlLayout.LeftHanded
                ? ControlLayout.Default : ControlLayout.LeftHanded;
            SettingsManager.Save();
            ApplyLayout();
        }
    }
}
