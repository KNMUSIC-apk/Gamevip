// ============================================================
// Enemy.cs
// Basic hostile entity. AI, aggro, attack, death, drops.
// ============================================================
using UnityEngine;
using UnityEngine.AI;
using ProjectAria.Core;
using ProjectAria.Optimization;

namespace ProjectAria.Combat
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        public string DisplayName = "Enemy";
        public float MaxHp = 50f;
        public float Hp { get; private set; }
        public float AttackDamage = 5f;
        public float AttackRange = 1.5f;
        public float AttackCooldown = 1.5f;
        public float MoveSpeed = 3f;
        public float AggroRange = 8f;
        public float LoseAggroRange = 15f;
        public int LootXp = 10;
        public LootDrop[] LootTable;
        public int EnemyId;
        public int Level = 1;

        public int PlayerId { get => EnemyId; set => EnemyId = value; }
        public bool Alive => Hp > 0;

        private NavMeshAgent _agent;
        private float _lastAttack;
        private Transform _target;
        private Animator _anim;

        private void Awake()
        {
            Hp = MaxHp;
            _agent = GetComponent<NavMeshAgent>();
            _anim = GetComponentInChildren<Animator>();
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default)
        {
            if (!Alive) return;
            float dmg = amount * DifficultyRules.DamageTaken;
            Hp -= dmg;
            EventBus.Publish(new EntityDamagedEvent(EnemyId, dmg, hitPoint));
            if (Hp <= 0) Die();
        }

        public void SetTarget(Transform t) { _target = t; }

        private void Update()
        {
            if (!Alive) return;
            if (_target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _target = p.transform;
                else return;
            }
            float d = Vector3.Distance(transform.position, _target.position);
            if (d < AggroRange)
            {
                _agent.isStopped = false;
                _agent.speed = MoveSpeed;
                _agent.SetDestination(_target.position);
                if (d < AttackRange && Time.time > _lastAttack + AttackCooldown)
                {
                    DoAttack();
                    _lastAttack = Time.time;
                }
            }
            else if (d > LoseAggroRange)
            {
                _agent.isStopped = true;
            }
            if (_anim != null) _anim.SetFloat("Speed", _agent.velocity.magnitude);
        }

        private void DoAttack()
        {
            if (_anim != null) _anim.SetTrigger("Attack");
            // Apply damage to player if in melee range
            var player = _target.GetComponent<PlayerStats>();
            if (player != null && Vector3.Distance(transform.position, _target.position) <= AttackRange + 0.2f)
                player.TakeDamage(AttackDamage, transform.position);
        }

        private void Die()
        {
            EventBus.Publish(new EntityKilledEvent(EnemyId, -1));
            // Drop loot
            for (int i = 0; i < LootTable.Length; i++)
            {
                if (Random.value < LootTable[i].Chance)
                {
                    SpawnLoot(LootTable[i].ItemId, LootTable[i].Amount);
                }
            }
            if (_anim != null) _anim.SetTrigger("Die");
            ObjectPool pool = ServiceLocator.Get<ObjectPool>();
            if (pool != null) pool.DespawnAfter(gameObject, 3f);
            else Destroy(gameObject, 3f);
        }

        private void SpawnLoot(int itemId, int amount)
        {
            var def = Inventory.ItemDatabase.Get(itemId);
            if (def == null || def.WorldPrefab == null) return;
            var pool = ServiceLocator.Get<ObjectPool>();
            GameObject go;
            if (pool != null) go = pool.Spawn(def.WorldPrefab, transform.position, Quaternion.identity);
            else go = Instantiate(def.WorldPrefab, transform.position, Quaternion.identity);
            var pickup = go.GetComponent<Inventory.WorldItem>();
            if (pickup != null) pickup.SetItem(itemId, amount);
        }
    }

    [System.Serializable]
    public struct LootDrop
    {
        public int ItemId;
        public int Amount;
        [Range(0, 1)] public float Chance;
    }
}
