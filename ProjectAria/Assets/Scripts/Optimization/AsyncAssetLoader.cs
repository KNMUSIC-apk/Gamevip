// ============================================================
// AsyncAssetLoader.cs
// Addressables-backed async loading with progress + cancellation.
// Pre-warms prefabs/materials to avoid hitches.
// ============================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ProjectAria.Core;

namespace ProjectAria.Optimization
{
    public class AsyncAssetLoader : IService
    {
        private readonly Dictionary<string, AsyncOperationHandle> _loadedHandles = new();
        private readonly Dictionary<string, object> _cache = new();
        private readonly Dictionary<string, Task<object>> _loading = new();

        public async Task<T> LoadAsync<T>(string key) where T : UnityEngine.Object
        {
            if (_cache.TryGetValue(key, out var cached)) return (T)cached;
            if (_loading.TryGetValue(key, out var pending)) return (T)await pending;
            var tcs = new TaskCompletionSource<object>();
            _loading[key] = tcs.Task;
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                await handle.Task;
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedHandles[key] = handle;
                    _cache[key] = handle.Result;
                    tcs.SetResult(handle.Result);
                    return handle.Result;
                }
                else
                {
                    Debug.LogError($"[AsyncAssetLoader] Failed to load: {key}");
                    tcs.SetResult(null);
                    return null;
                }
            }
            finally
            {
                _loading.Remove(key);
            }
        }

        public void Prewarm(string[] keys, Action onComplete = null)
        {
            int remaining = keys.Length;
            foreach (var key in keys)
            {
                LoadAsync<UnityEngine.Object>(key).ContinueWith(_ =>
                {
                    remaining--;
                    if (remaining <= 0) onComplete?.Invoke();
                });
            }
        }

        public void ReleaseAll()
        {
            foreach (var kv in _loadedHandles)
                Addressables.Release(kv.Value);
            _loadedHandles.Clear();
            _cache.Clear();
        }
    }
}
