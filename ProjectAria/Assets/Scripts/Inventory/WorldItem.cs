// ============================================================
// WorldItem.cs
// Item drop in the world. Auto-pickup on touch, magnet animation.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Player;

namespace ProjectAria.Inventory
{
    [RequireComponent(typeof(Collider))]
    public class WorldItem : MonoBehaviour
    {
        public int ItemId { get; private set; }
        public int Amount { get; private set; }
        public float MagnetRange = 2f;
        public float MagnetSpeed = 8f;
        public float LifeTime = 60f;
        public bool DespawnOnPickup = true;

        private float _spawnTime;
        private Transform _target;
        private bool _attracted;
        private PlayerInventory _targetInv;

        public void SetItem(int id, int amount)
        {
            ItemId = id;
            Amount = amount;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _spawnTime > LifeTime) { Destroy(gameObject); return; }

            if (_target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _target = p.transform;
            }
            if (_target == null) return;

            float d = Vector3.Distance(transform.position, _target.position);
            if (d < MagnetRange || _attracted)
            {
                _attracted = true;
                if (_targetInv == null) _targetInv = _target.GetComponent<PlayerInventory>();
                transform.position = Vector3.MoveTowards(transform.position, _target.position, MagnetSpeed * Time.deltaTime);
                if (d < 0.5f)
                {
                    if (_targetInv != null && _targetInv.AddItem(ItemId, Amount))
                    {
                        EventBus.Publish(new ItemPickedUpEvent(ItemId, Amount));
                        if (DespawnOnPickup) Destroy(gameObject);
                    }
                }
            }
            // Bob & spin
            transform.Rotate(Vector3.up, 60f * Time.deltaTime);
            transform.position += Vector3.up * Mathf.Sin(Time.time * 2f) * 0.005f;
        }
    }
}
