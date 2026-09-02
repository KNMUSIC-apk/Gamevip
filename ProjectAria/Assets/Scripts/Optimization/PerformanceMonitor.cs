// ============================================================
// PerformanceMonitor.cs
// Tracks FPS, frame time, draw calls, triangles, GC alloc.
// Adjusts quality if FPS drops below target for sustained period.
// ============================================================
using UnityEngine;
using UnityEngine.Profiling;
using ProjectAria.Core;

namespace ProjectAria.Optimization
{
    public class PerformanceMonitor : MonoBehaviour
    {
        public float SampleInterval = 1f;
        public float LowFpsThreshold = 0.85f; // of target
        public int LowFpsFramesToTrigger = 5;
        public bool AutoQualityAdjust = true;

        public float AvgFps { get; private set; }
        public float LastFrameMs { get; private set; }
        public int DrawCalls { get; private set; }
        public int Triangles { get; private set; }
        public int SetPassCalls { get; private set; }
        public long GcAllocPerFrame { get; private set; }

        private float _accumulator;
        private int _frames;
        private int _lowFpsStreak;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            LastFrameMs = dt * 1000f;
            _accumulator += dt;
            _frames++;
            GcAllocPerFrame = System.GC.GetAllocatedBytesForCurrentThread();

            if (_accumulator >= SampleInterval)
            {
                AvgFps = _frames / _accumulator;
                DrawCalls = UnityStatsDrawCalls();
                Triangles = UnityStatsTriangles();
                SetPassCalls = UnityStatsSetPassCalls();
                _accumulator = 0f;
                _frames = 0;

                int target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
                if (AvgFps < target * LowFpsThreshold) _lowFpsStreak++;
                else _lowFpsStreak = 0;

                if (AutoQualityAdjust && _lowFpsStreak >= LowFpsFramesToTrigger)
                {
                    ReduceQuality();
                    _lowFpsStreak = 0;
                }
            }
        }

        private void ReduceQuality()
        {
            var s = SettingsManager.Current;
            if (s.graphicsTier > GraphicsTier.VeryLow)
            {
                s.graphicsTier--;
                s.renderDistanceChunks = Mathf.Max(2, s.renderDistanceChunks - 1);
                SettingsManager.Apply();
                SettingsManager.Save();
                Debug.Log($"[PerformanceMonitor] Auto-reduced quality to {s.graphicsTier}");
            }
        }

        // UnityStats is internal; reflection fallback to Unity Profiler API
        private static int UnityStatsDrawCalls() { try { return (int)typeof(UnityEditor.UnityStats).GetProperty("drawCalls", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null); } catch { return 0; } }
        private static int UnityStatsTriangles() { try { return (int)typeof(UnityEditor.UnityStats).GetProperty("triangles", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null); } catch { return 0; } }
        private static int UnityStatsSetPassCalls() { try { return (int)typeof(UnityEditor.UnityStats).GetProperty("setPassCalls", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null); } catch { return 0; } }
    }
}
