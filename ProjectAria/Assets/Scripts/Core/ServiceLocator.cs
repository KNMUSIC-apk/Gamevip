// ============================================================
// ServiceLocator.cs
// Lightweight runtime service container. Systems register themselves
// at boot; other systems retrieve them by interface.
// For larger projects, swap for VContainer/Zenject later.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAria.Core
{
    public interface IService { }

    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new(32);
        private static readonly Dictionary<Type, List<object>> _buffered = new(); // for late-resolved requests

        public static void Register<T>(T service) where T : class, IService
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Overwriting existing service: {type.Name}");
                _services[type] = service;
            }
            else
            {
                _services[type] = service;
            }

            // Drain any buffered Get requests for this type
            if (_buffered.TryGetValue(type, out var waiters))
            {
                foreach (var w in waiters) ((Action<T>)w)?.Invoke(service);
                _buffered.Remove(type);
            }
        }

        public static void Unregister<T>() where T : class, IService
        {
            _services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class, IService
        {
            if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
            return null;
        }

        /// <summary>
        /// If service isn't yet registered, queues the callback to be invoked
        /// the moment it does register. Use sparingly — prefer explicit init order.
        /// </summary>
        public static void GetOrWait<T>(Action<T> onReady) where T : class, IService
        {
            var existing = Get<T>();
            if (existing != null) { onReady?.Invoke(existing); return; }
            var type = typeof(T);
            if (!_buffered.TryGetValue(type, out var list))
            {
                list = new List<object>(2);
                _buffered[type] = list;
            }
            list.Add(onReady);
        }

        public static void Clear()
        {
            _services.Clear();
            _buffered.Clear();
        }
    }
}
