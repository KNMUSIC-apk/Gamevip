// ============================================================
// WorldManagerRunner.cs
// MonoBehaviour wrapper around WorldManager service.
// Place on a scene GameObject; assigns Player + ChunkMaterial.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Optimization;
using ProjectAria.World;

namespace ProjectAria.World
{
    public class WorldManagerRunner : MonoBehaviour
    {
        public Transform Player;
        public Material ChunkMaterial;
        public int InitialRenderDistance = 6;

        private WorldManager _wm;

        private void Start()
        {
            if (ServiceLocator.Get<WorldManager>() != null) return;
            _wm = new WorldManager();
            int seed = GameManager.Instance != null ? GameManager.Instance.Seed : 0;
            _wm.Init(seed, Player);
            _wm.ChunkMaterial = ChunkMaterial;
            _wm.RenderDistance = InitialRenderDistance;
            ServiceLocator.Register<WorldManager>(_wm);
        }

        private void Update()
        {
            _wm?.Update();
        }

        private void OnDestroy()
        {
            if (_wm != null) ServiceLocator.Unregister<WorldManager>();
        }
    }
}
