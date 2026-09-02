// ============================================================
// LODManager.cs
// Updates LOD groups each frame for active entities.
// Mobile: 2-3 LOD levels, aggressive culling.
// ============================================================
using UnityEngine;
using System.Collections.Generic;
using ProjectAria.Core;

namespace ProjectAria.Optimization
{
    public class LODManager : IService
    {
        public Camera MainCamera;
        public float HighDetailDistance = 20f;
        public float MediumDetailDistance = 50f;
        public float LowDetailDistance = 100f;
        public float UpdateInterval = 0.5f;

        private readonly List<LODTarget> _targets = new();
        private float _nextUpdate;

        public void Register(LODTarget t)
        {
            if (t == null || _targets.Contains(t)) return;
            _targets.Add(t);
        }
        public void Unregister(LODTarget t) => _targets.Remove(t);

        public void Tick(float dt)
        {
            if (Time.time < _nextUpdate) return;
            _nextUpdate = Time.time + UpdateInterval;
            if (MainCamera == null) MainCamera = Camera.main;
            if (MainCamera == null) return;
            Vector3 camPos = MainCamera.transform.position;
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                if (t == null) { _targets.RemoveAt(i); i--; continue; }
                float d = Vector3.Distance(camPos, t.transform.position);
                int level = d < HighDetailDistance ? 0
                          : d < MediumDetailDistance ? 1
                          : d < LowDetailDistance ? 2
                          : 3;
                t.SetLOD(level);
                t.gameObject.SetActive(level < 3);
            }
        }
    }

    public class LODTarget : MonoBehaviour
    {
        public GameObject[] LodLevels; // 0=high 1=med 2=low 3=cull
        public void SetLOD(int level)
        {
            for (int i = 0; i < LodLevels.Length; i++)
                if (LodLevels[i] != null) LodLevels[i].SetActive(i == level);
        }
        private void OnEnable() { ServiceLocator.GetOrWait<LODManager>(m => m.Register(this)); }
        private void OnDisable() { ServiceLocator.Get<LODManager>()?.Unregister(this); }
    }
}
