// ============================================================
// AudioManager.cs
// Adaptive audio. Music layers (explore/combat/boss) that crossfade
// based on game state. Ambient loops by biome. SFX pool.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.Audio
{
    public enum MusicState { None, Explore, Combat, Boss, Peaceful, Menu }

    public class AudioManager : IService
    {
        public AudioSource MusicSourceA, MusicSourceB;
        public AudioSource AmbientSource;
        public AudioSource[] SfxPool;
        private int _sfxIndex;

        public MusicState CurrentState { get; private set; } = MusicState.Explore;
        public AudioClip CurrentMusic { get; private set; }
        public AudioClip CurrentAmbient { get; private set; }

        private readonly Dictionary<MusicState, AudioClip> _musicTable = new();
        private readonly Dictionary<string, AudioClip> _ambientTable = new();

        public float MasterVolume => SettingsManager.Current.masterVolume;
        public float MusicVolume => SettingsManager.Current.musicVolume;
        public float SfxVolume => SettingsManager.Current.sfxVolume;
        public float AmbientVolume => SettingsManager.Current.ambientVolume;

        public void RegisterMusic(MusicState state, AudioClip clip) => _musicTable[state] = clip;
        public void RegisterAmbient(string id, AudioClip clip) => _ambientTable[id] = clip;

        public void SetState(MusicState state)
        {
            if (state == CurrentState) return;
            CurrentState = state;
            if (_musicTable.TryGetValue(state, out var clip)) PlayMusicCrossfade(clip);
        }

        public void SetBiomeAmbient(string biomeId)
        {
            if (_ambientTable.TryGetValue(biomeId, out var clip) && clip != CurrentAmbient)
            {
                CurrentAmbient = clip;
                if (AmbientSource != null)
                {
                    AmbientSource.Stop();
                    AmbientSource.clip = clip;
                    AmbientSource.loop = true;
                    AmbientSource.volume = AmbientVolume * MasterVolume;
                    AmbientSource.Play();
                }
            }
        }

        private void PlayMusicCrossfade(AudioClip newClip)
        {
            if (MusicSourceA == null || MusicSourceB == null) return;
            var playing = MusicSourceA.isPlaying ? MusicSourceA : MusicSourceB;
            var silent = MusicSourceA.isPlaying ? MusicSourceB : MusicSourceA;
            silent.clip = newClip;
            silent.loop = true;
            silent.volume = 0f;
            silent.Play();
            CurrentMusic = newClip;
            // Crossfade 2 sec
            LeanTweenHelper.FadeAudio(playing, 0f, 2f);
            LeanTweenHelper.FadeAudio(silent, MusicVolume * MasterVolume, 2f);
        }

        public void PlaySfx(AudioClip clip, Vector3 pos = default, float volume = 1f)
        {
            if (clip == null || SfxPool == null || SfxPool.Length == 0) return;
            var src = SfxPool[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % SfxPool.Length;
            src.transform.position = pos;
            src.clip = clip;
            src.volume = volume * SfxVolume * MasterVolume;
            src.PlayOneShot(clip);
        }

        public void UpdateVolumes()
        {
            float mv = MusicVolume * MasterVolume;
            if (MusicSourceA != null) MusicSourceA.volume = mv;
            if (MusicSourceB != null) MusicSourceB.volume = mv;
            if (AmbientSource != null) AmbientSource.volume = AmbientVolume * MasterVolume;
        }
    }

    // Lightweight fallback for crossfading without a tween library
    public static class LeanTweenHelper
    {
        private class FadeTask { public AudioSource src; public float from, to, duration, t; }
        private static readonly List<FadeTask> _tasks = new();

        public static void FadeAudio(AudioSource src, float to, float duration)
        {
            if (src == null) return;
            foreach (var t in _tasks) if (t.src == src) { t.to = to; t.duration = duration; t.t = 0f; return; }
            _tasks.Add(new FadeTask { src = src, from = src.volume, to = to, duration = Mathf.Max(0.01f, duration), t = 0f });
        }

        // Call once per frame from a manager
        public static void Tick(float dt)
        {
            for (int i = _tasks.Count - 1; i >= 0; i--)
            {
                var t = _tasks[i];
                t.t += dt;
                float k = Mathf.Clamp01(t.t / t.duration);
                t.src.volume = Mathf.Lerp(t.from, t.to, k);
                if (k >= 1f) _tasks.RemoveAt(i);
            }
        }
    }
}
