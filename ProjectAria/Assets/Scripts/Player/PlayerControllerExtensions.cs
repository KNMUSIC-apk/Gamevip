// ============================================================
// PlayerControllerExtensions.cs
// Adds methods invoked by MobileControlsUI and other systems.
// Kept separate to keep PlayerController.cs focused.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Inventory;
using ProjectAria.World;

namespace ProjectAria.Player
{
    public partial class PlayerController
    {
        public void JumpRequest()
        {
            // Hook for mobile Jump button
            if (_cc != null && _cc.isGrounded && _stats != null && _stats.TryUseStamina(8f))
                _velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }

        public void DodgeRequest()
        {
            if (_combat != null) _combat.TryDodge(transform.forward);
        }

        public void UseHeldItem()
        {
            var stack = _inventory != null ? _inventory.SelectedItem : default;
            if (stack.IsEmpty) return;
            var def = ItemDatabase.Get(stack.itemId);
            if (def == null) return;
            if (def.Category == ItemCategory.Consumable || def.Category == ItemCategory.Food)
            {
                if (def.HealAmount > 0) _stats.Heal(def.HealAmount);
                if (def.HungerRestored > 0) _stats.AddHunger(def.HungerRestored);
                if (def.StaminaRestored > 0)
                {
                    _stats.Data.stamina = Mathf.Min(_stats.Data.maxStamina, _stats.Data.stamina + def.StaminaRestored);
                }
                if (def.TemperatureRestored > 0) _stats.AddTemperature(def.TemperatureRestored);
                _inventory.RemoveItem(stack.itemId, 1);
            }
        }

        public void TryMine()
        {
            if (WorldManager.Instance == null) return;
            if (Physics.Raycast(_player_CameraRay(), out var hit, 5f))
            {
                var chunk = hit.collider.GetComponentInParent<Chunk>();
                if (chunk != null)
                {
                    var pos = hit.point - hit.normal * 0.01f;
                    var grid = Vector3Int.RoundToInt(pos);
                    int blockId = WorldManager.Instance.GetBlockWorld(grid);
                    if (blockId != 0)
                    {
                        var def = BlockDatabase.Get(blockId);
                        if (def != null && def.Mineable)
                        {
                            WorldManager.Instance.SetBlockWorld(grid, 0);
                            // Drop item
                            if (def.DropsItself)
                            {
                                if (def.DropItemId > 0)
                                    _inventory?.AddItem(def.DropItemId, 1);
                            }
                            EventBus.Publish(new BlockBrokenEvent(grid, blockId));
                        }
                    }
                }
            }
        }

        private Ray _player_CameraRay()
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 dir = CameraRoot != null ? CameraRoot.forward : transform.forward;
            return new Ray(origin, dir);
        }
    }
}
