// ============================================================
// BuildingSystem.cs
// Grid-snap block placement. Preview ghost, rotation, validation.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Inventory;
using ProjectAria.World;

namespace ProjectAria.Building
{
    public class BuildingSystem : MonoBehaviour
    {
        public LayerMask GroundMask = ~0;
        public float MaxPlaceDistance = 6f;
        public GameObject PreviewPrefab;
        public Material GhostValidMat;
        public Material GhostInvalidMat;
        public Color ValidColor = new(0f, 1f, 0f, 0.4f);
        public Color InvalidColor = new(1f, 0f, 0f, 0.4f);

        private GameObject _previewInstance;
        private MeshRenderer _previewRenderer;
        private PlayerController _player;
        private ItemStack _heldItem;
        private bool _canBuild;
        private Vector3Int _lastGridPos;
        private Quaternion _rotation = Quaternion.identity;

        public bool IsBuilding { get; private set; }
        public Vector3Int CurrentGrid { get; private set; }

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        public void EnterBuildMode() { IsBuilding = true; EnsurePreview(); }
        public void ExitBuildMode() { IsBuilding = false; if (_previewInstance) _previewInstance.SetActive(false); }

        public void SetHeldItem(ItemStack stack)
        {
            _heldItem = stack;
        }

        public void TryPlace()
        {
            if (!IsBuilding || !_canBuild) return;
            if (_heldItem.IsEmpty) return;
            var def = ItemDatabase.Get(_heldItem.itemId);
            if (def == null || def.PlaceBlockId == 0) return;
            if (WorldManager.Instance == null) return;
            if (WorldManager.Instance.GetBlockWorld(CurrentGrid) != 0) return;
            WorldManager.Instance.SetBlockWorld(CurrentGrid, def.PlaceBlockId);
            GetComponent<PlayerInventory>()?.RemoveItem(_heldItem.itemId, 1);
            EventBus.Publish(new BlockPlacedEvent(CurrentGrid, def.PlaceBlockId));
        }

        public void RotatePreview()
        {
            _rotation *= Quaternion.Euler(0, 90, 0);
        }

        private void Update()
        {
            if (!IsBuilding) return;
            EnsurePreview();
            UpdatePreview();
        }

        private void EnsurePreview()
        {
            if (_previewInstance != null) return;
            if (PreviewPrefab == null) return;
            _previewInstance = Instantiate(PreviewPrefab);
            _previewRenderer = _previewInstance.GetComponentInChildren<MeshRenderer>();
        }

        private void UpdatePreview()
        {
            if (_previewInstance == null) return;
            _previewInstance.SetActive(true);
            Vector3 origin = _player.transform.position + Vector3.up * 1.5f;
            if (!Physics.Raycast(origin, _player.CameraRoot.forward, out var hit, MaxPlaceDistance, GroundMask))
            {
                _previewInstance.SetActive(false); _canBuild = false; return;
            }
            Vector3 snapped = new(Mathf.Round(hit.point.x), Mathf.Round(hit.point.y), Mathf.Round(hit.point.z));
            _previewInstance.transform.position = snapped;
            _previewInstance.transform.rotation = _rotation;
            CurrentGrid = Vector3Int.RoundToInt(snapped);

            bool empty = WorldManager.Instance != null && WorldManager.Instance.GetBlockWorld(CurrentGrid) == 0;
            bool hasItem = !_heldItem.IsEmpty;
            _canBuild = empty && hasItem;
            if (_previewRenderer != null)
            {
                var mat = new Material(_previewRenderer.sharedMaterial);
                mat.color = _canBuild ? ValidColor : InvalidColor;
                _previewRenderer.material = mat;
            }
        }
    }
}
