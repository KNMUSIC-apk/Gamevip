// ============================================================
// MinimapUI.cs
// Top-down minimap rendered with RenderTexture, with markers.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using ProjectAria.Core;
using ProjectAria.World;

namespace ProjectAria.UI
{
    public class MinimapUI : MonoBehaviour
    {
        public Camera MapCamera;
        public RawImage MapImage;
        public RectTransform MarkerRoot;
        public GameObject MarkerPrefab;
        public int Resolution = 256;
        public float WorldSize = 64f;
        public float Height = 80f;

        private RenderTexture _rt;
        private readonly System.Collections.Generic.Dictionary<int, RectTransform> _markers = new();

        private void Start()
        {
            if (MapCamera == null) return;
            _rt = new RenderTexture(Resolution, Resolution, 16) { filterMode = FilterMode.Bilinear };
            MapCamera.targetTexture = _rt;
            if (MapImage != null) MapImage.texture = _rt;
        }

        public void Refresh(Vector3 playerPos)
        {
            if (MapCamera != null)
            {
                MapCamera.orthographicSize = WorldSize * 0.5f;
                MapCamera.transform.position = new Vector3(playerPos.x, playerPos.y + Height, playerPos.z);
                MapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        public void AddMarker(int id, Vector3 worldPos, Color color)
        {
            if (MarkerPrefab == null) return;
            if (!_markers.TryGetValue(id, out var rt))
            {
                var go = Instantiate(MarkerPrefab, MarkerRoot);
                rt = go.GetComponent<RectTransform>();
                _markers[id] = rt;
            }
            // Project world pos to minimap UV space
            if (MapCamera == null) return;
            Vector3 screen = MapCamera.WorldToViewportPoint(worldPos);
            rt.anchoredPosition = new Vector2((screen.x - 0.5f) * MapImage.rectTransform.rect.width,
                                              (screen.y - 0.5f) * MapImage.rectTransform.rect.height);
            var img = rt.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private void OnDestroy()
        {
            if (_rt != null) { _rt.Release(); Object.Destroy(_rt); }
        }
    }
}
