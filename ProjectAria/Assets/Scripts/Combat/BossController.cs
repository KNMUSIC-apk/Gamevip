// ============================================================
// BossController.cs
// Multi-phase boss with pattern-based attacks, enrage, mechanics.
// ============================================================
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.Combat
{
    [CreateAssetMenu(fileName = "BossPattern_", menuName = "Aria/Combat/BossPattern", order = 5)]
    public class BossPattern : ScriptableObject
    {
        public string PatternName;
        public float Weight = 1f;
        public float Cooldown = 4f;
        public float MinDistance = 0f;
        public float MaxDistance = 100f;
        public float TelegraphTime = 1f;
        public float Damage = 20f;
        public float Radius = 5f;
        public bool IsAoe;
        public bool IsCharge;
        public bool IsProjectile;
        public int ProjectileCount = 1;
        public AudioClip CueSound;
    }

    [CreateAssetMenu(fileName = "BossPhase_", menuName = "Aria/Combat/BossPhase", order = 6)]
    public class BossPhase : ScriptableObject
    {
        public int PhaseIndex;
        public float HpThreshold = 1f; // 0..1 of max hp
        public BossPattern[] Patterns;
        public float MoveSpeedMultiplier = 1f;
        public float DamageMultiplier = 1f;
        public bool Enrage;
        public string IntroDialogue;
    }

    public class BossController : Enemy
    {
        public BossPhase[] Phases;
        public Transform[] TelegrapherMarkers;
        public GameObject ProjectilePrefab;
        public AudioSource AudioSource;

        private int _currentPhase = 0;
        private float _phaseStartHpPercent = 1f;
        private BossPattern _activePattern;
        private float _patternStart;
        private bool _isTelegraphing;

        public override void TakeDamage(float amount, Vector3 hitPoint = default)
        {
            base.TakeDamage(amount, hitPoint);
            CheckPhaseTransition();
        }

        private void CheckPhaseTransition()
        {
            float hpPercent = Hp / MaxHp;
            for (int i = Phases.Length - 1; i >= 0; i--)
            {
                if (hpPercent <= Phases[i].HpThreshold && _currentPhase < i)
                {
                    EnterPhase(i);
                    break;
                }
            }
        }

        private void EnterPhase(int index)
        {
            _currentPhase = index;
            var phase = Phases[index];
            MoveSpeed *= phase.MoveSpeedMultiplier;
            if (phase.Enrage) { /* visual fx, music change */ }
            EventBus.Publish(new BossPhaseChangedEvent(EnemyId, index));
        }

        private void Update()
        {
            if (!Alive) return;
            base.Update();
            if (_activePattern == null)
            {
                _activePattern = PickPattern();
                if (_activePattern != null)
                {
                    _isTelegraphing = true;
                    _patternStart = Time.time;
                }
            }
            else
            {
                float elapsed = Time.time - _patternStart;
                if (_isTelegraphing && elapsed >= _activePattern.TelegraphTime)
                {
                    ExecutePattern(_activePattern);
                    _isTelegraphing = false;
                    _patternStart = Time.time;
                }
                else if (!_isTelegraphing && elapsed >= _activePattern.Cooldown)
                {
                    _activePattern = null;
                }
            }
        }

        private BossPattern PickPattern()
        {
            var phase = Phases[_currentPhase];
            if (phase.Patterns == null || phase.Patterns.Length == 0) return null;
            float total = 0f; foreach (var p in phase.Patterns) total += p.Weight;
            float r = Random.value * total;
            float acc = 0f;
            foreach (var p in phase.Patterns)
            {
                acc += p.Weight;
                if (r <= acc) return p;
            }
            return phase.Patterns[0];
        }

        private void ExecutePattern(BossPattern p)
        {
            if (p.IsAoe)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, p.Radius);
                foreach (var h in hits)
                {
                    var dmg = h.GetComponentInParent<IDamageable>();
                    if (dmg != null && dmg.Alive) dmg.TakeDamage(p.Damage, transform.position);
                }
            }
            else if (p.IsProjectile && ProjectilePrefab != null)
            {
                for (int i = 0; i < p.ProjectileCount; i++)
                {
                    var proj = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
                    var bp = proj.GetComponent<Projectile>();
                    if (bp != null) bp.Init(transform.forward, p.Damage, EnemyId, 8f);
                }
            }
            // Charge / melee handled by base Enemy if needed
        }
    }
}
