// ============================================================
// Projectile.cs
// Homing / straight projectile with lifetime.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Optimization;

namespace ProjectAria.Combat
{
    public class Projectile : MonoBehaviour
    {
        public float Speed = 12f;
        public float Lifetime = 5f;
        public int Damage = 10f;
        public int SourceEntityId;
        public bool Homing = false;
        public Transform Target;
        public LayerMask HitMask = ~0;
        public GameObject HitEffect;
        public AudioClip HitSound;

        private float _spawnTime;
        private Vector3 _dir;
        private int _hitMaskEnemy;

        public void Init(Vector3 direction, float damage, int source, float speed = -1f, float life = -1f)
        {
            _dir = direction.normalized;
            Damage = Mathf.RoundToInt(damage);
            SourceEntityId = source;
            if (speed > 0) Speed = speed;
            if (life > 0) Lifetime = life;
            _spawnTime = Time.time;
        }

        public void SetTarget(Transform t) { Target = t; Homing = true; }

        private void Update()
        {
            if (Time.time - _spawnTime > Lifetime) { Despawn(); return; }
            if (Homing && Target != null)
            {
                Vector3 toT = (Target.position - transform.position).normalized;
                _dir = Vector3.Slerp(_dir, toT, 5f * Time.deltaTime);
            }
            transform.rotation = Quaternion.LookRotation(_dir);
            transform.position += _dir * Speed * Time.deltaTime;

            if (Physics.Raycast(transform.position, _dir, out var hit, Speed * Time.deltaTime * 1.2f, HitMask))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.Alive && dmg.PlayerId != SourceEntityId)
                {
                    dmg.TakeDamage(Damage, hit.point);
                }
                if (HitEffect != null) Instantiate(HitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Despawn();
            }
        }

        private void Despawn()
        {
            var pool = ServiceLocator.Get<ObjectPool>();
            if (pool != null && gameObject.GetComponent<Pooled>() != null) pool.Despawn(gameObject);
            else Destroy(gameObject);
        }
    }
}
