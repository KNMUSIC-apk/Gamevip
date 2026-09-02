// ============================================================
// PlayerInteraction.cs
// Raycast / sphere overlap to find interactable targets.
// Targets: NPCs, items, blocks (mine), doors, beds, chests.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.World;

namespace ProjectAria.Player
{
    public interface IInteractable
    {
        string DisplayName { get; }
        bool CanInteract(PlayerController player);
        void OnInteract(PlayerController player);
        Transform Transform { get; }
    }

    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteraction : MonoBehaviour
    {
        public float Radius = 2.5f;
        public LayerMask Mask = ~0;
        public KeyCode DebugKey = KeyCode.E; // legacy fallback

        private PlayerController _player;

        private void Awake() => _player = GetComponent<PlayerController>();

        public IInteractable GetBestTarget()
        {
            // Check block first (closer range)
            Vector3 origin = _player.transform.position + Vector3.up * 1.5f;
            float range = _player.InteractRange;
            if (Physics.Raycast(origin, _player.CameraRoot.forward, out var hit, range, Mask))
            {
                var block = hit.collider.GetComponent<IInteractable>();
                if (block != null) return block;
            }
            // Sphere overlap for nearby NPCs/items
            var hits = Physics.OverlapSphere(_player.transform.position, Radius, Mask);
            IInteractable best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                var it = h.GetComponentInParent<IInteractable>();
                if (it == null) continue;
                if (!it.CanInteract(_player)) continue;
                float d = Vector3.Distance(_player.transform.position, it.Transform.position);
                if (d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }

        public bool TryInteract()
        {
            var target = GetBestTarget();
            if (target == null) return false;
            target.OnInteract(_player);
            return true;
        }

        public Vector3Int? GetTargetBlock()
        {
            Vector3 origin = _player.transform.position + Vector3.up * 1.5f;
            if (Physics.Raycast(origin, _player.CameraRoot.forward, out var hit, _player.InteractRange))
            {
                var chunk = hit.collider.GetComponentInParent<Chunk>();
                if (chunk != null)
                {
                    var pos = hit.point - hit.normal * 0.01f;
                    return Vector3Int.RoundToInt(pos);
                }
            }
            return null;
        }
    }
}
