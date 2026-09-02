// ============================================================
// CombatSystem.cs
// Hit detection, weapons, dodge, parry, magic, skills.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Inventory;

namespace ProjectAria.Combat
{
    public enum AttackType { Light, Heavy, Charged, Ranged, Magic }

    public class CombatSystem : MonoBehaviour
    {
        public float LightAttackCost = 5f;
        public float HeavyAttackCost = 12f;
        public float DodgeStamina = 15f;
        public float DodgeDistance = 4f;
        public float DodgeDuration = 0.25f;
        public float ParryWindow = 0.2f;
        public float AttackRange = 2.5f;
        public float AttackArc = 80f;
        public LayerMask EnemyMask = ~0;

        public bool IsAttacking { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsParryable { get; private set; }
        public float LastAttackTime { get; private set; }
        public IDamageable CurrentTarget { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private PlayerInventory _inv;
        private Animator _anim;
        private float _dodgeEnd;
        private float _parryEnd;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _inv = GetComponent<PlayerInventory>();
            _anim = GetComponentInChildren<Animator>();
        }

        public void TryAttack()
        {
            if (IsDodging) return;
            if (!_stats.TryUseStamina(LightAttackCost)) return;
            IsAttacking = true;
            LastAttackTime = Time.time;
            // Find target
            var hits = Physics.OverlapSphere(transform.position + transform.forward, AttackRange, EnemyMask);
            IDamageable bestTarget = null;
            float bestAngle = float.MaxValue;
            foreach (var h in hits)
            {
                var d = h.GetComponentInParent<IDamageable>();
                if (d == null || !d.Alive) continue;
                Vector3 to = (d.PlayerId >= 0 ? h.transform.position - transform.position : Vector3.zero);
                float angle = Vector3.Angle(transform.forward, to);
                if (angle < AttackArc && angle < bestAngle) { bestAngle = angle; bestTarget = d; }
            }
            CurrentTarget = bestTarget;
            if (CurrentTarget != null)
            {
                var stack = _inv.SelectedItem;
                var def = ItemDatabase.Get(stack.itemId);
                int damage = def != null ? def.Damage : 5;
                damage = Mathf.RoundToInt(damage * DifficultyRules.DamageDealt);
                CurrentTarget.TakeDamage(damage, transform.position + transform.forward);
            }
            if (_anim != null) _anim.SetTrigger("Attack");
            Invoke(nameof(ResetAttack), 0.4f);
        }

        public void TryHeavy()
        {
            if (IsDodging) return;
            if (!_stats.TryUseStamina(HeavyAttackCost)) return;
            // Similar to TryAttack but bigger radius / cost
        }

        public void TryDodge(Vector3 dir)
        {
            if (IsDodging) return;
            if (!_stats.TryUseStamina(DodgeStamina)) return;
            IsDodging = true;
            _dodgeEnd = Time.time + DodgeDuration;
            if (_anim != null) _anim.SetTrigger("Dodge");
        }

        public void TryParry()
        {
            IsParryable = true;
            _parryEnd = Time.time + ParryWindow;
        }

        public void CastSkill(string skillId)
        {
            // Stub: forward to skill tree executor
            if (_anim != null) _anim.SetTrigger("Cast");
        }

        private void ResetAttack() { IsAttacking = false; CurrentTarget = null; }

        private void Update()
        {
            if (IsDodging && Time.time > _dodgeEnd)
            {
                IsDodging = false;
            }
            if (IsParryable && Time.time > _parryEnd)
            {
                IsParryable = false;
            }
        }
    }
}
