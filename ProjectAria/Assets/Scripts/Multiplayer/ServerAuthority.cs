// ============================================================
// ServerAuthority.cs
// Ensures that gameplay-affecting state (block place/break, damage,
// inventory changes, NPC behavior) is validated by the server.
// Anti-cheat hooks: rate limiting, distance checks, sanity checks.
// ============================================================
using UnityEngine;
using Unity.Netcode;
using ProjectAria.Core;
using ProjectAria.World;
using ProjectAria.Inventory;

namespace ProjectAria.Multiplayer
{
    public class ServerAuthority : MonoBehaviour
    {
        public static ServerAuthority Instance { get; private set; }
        public float MaxBlockPlaceDistance = 8f;
        public float MaxAttackDistance = 5f;
        public int MaxBlocksPerSecond = 12;
        public int MaxAttacksPerSecond = 6;

        private float _blockCooldown;
        private float _attackCooldown;
        private int _blocksThisSecond;
        private int _attacksThisSecond;
        private float _lastSecondReset;

        public bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        public bool TryApproveBlockPlace(ulong clientId, Vector3Int pos, int blockId)
        {
            if (!IsServer) return false;
            if (Time.time - _blockCooldown < 1f / MaxBlocksPerSecond) return false;
            if (Time.time - _lastSecondReset > 1f) { _blocksThisSecond = 0; _attacksThisSecond = 0; _lastSecondReset = Time.time; }
            if (_blocksThisSecond >= MaxBlocksPerSecond) return false;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var obj = client.PlayerObject;
                if (obj != null)
                {
                    float d = Vector3.Distance(obj.transform.position, pos);
                    if (d > MaxBlockPlaceDistance) return false;
                }
            }
            // Block id range check
            if (BlockDatabase.Get(blockId) == null) return false;
            _blockCooldown = Time.time;
            _blocksThisSecond++;
            return true;
        }

        public bool TryApproveAttack(ulong clientId, ulong targetId, float damage)
        {
            if (!IsServer) return false;
            if (damage < 0 || damage > 1000) return false;
            if (Time.time - _attackCooldown < 1f / MaxAttacksPerSecond) return false;
            if (Time.time - _lastSecondReset > 1f) { _blocksThisSecond = 0; _attacksThisSecond = 0; _lastSecondReset = Time.time; }
            if (_attacksThisSecond >= MaxAttacksPerSecond) return false;
            // Distance
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
                NetworkManager.Singleton.ConnectedClients.TryGetValue(targetId, out var target))
            {
                if (client.PlayerObject != null && target.PlayerObject != null)
                {
                    float d = Vector3.Distance(client.PlayerObject.transform.position, target.PlayerObject.transform.position);
                    if (d > MaxAttackDistance) return false;
                }
            }
            _attackCooldown = Time.time;
            _attacksThisSecond++;
            return true;
        }

        public bool TryApproveInventoryChange(ulong clientId, int itemId, int amountDelta)
        {
            if (!IsServer) return false;
            if (itemId < 0) return false;
            if (Mathf.Abs(amountDelta) > 9999) return false; // sanity
            return true;
        }
    }
}
