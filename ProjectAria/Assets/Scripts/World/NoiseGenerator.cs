// ============================================================
// NoiseGenerator.cs
// Wraps Unity's Mathf.PerlinNoise (CPU-only, mobile-friendly)
// plus seeded hash for biome sampling and structure placement.
// ============================================================
using UnityEngine;

namespace ProjectAria.World
{
    public class NoiseGenerator
    {
        private readonly int _seed;
        private readonly System.Random _rng;
        private readonly Vector2 _offset;

        public NoiseGenerator(int seed)
        {
            _seed = seed;
            _rng = new System.Random(seed);
            _offset = new Vector2(_rng.Next(-10000, 10000), _rng.Next(-10000, 10000));
        }

        public int Seed => _seed;

        // 2D perlin in [-1, 1] approximately
        public float Perlin2D(float x, float y, float scale, int octaves, float persistence, float lacunarity)
        {
            float amp = 1f, freq = 1f, sum = 0f, max = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float px = (x + _offset.x) * scale * freq;
                float py = (y + _offset.y) * scale * freq;
                float n = Mathf.PerlinNoise(px, py) * 2f - 1f;
                sum += n * amp;
                max += amp;
                amp *= persistence;
                freq *= lacunarity;
            }
            return sum / Mathf.Max(0.001f, max);
        }

        public float FBM2D(float x, float y, float scale, int octaves = 4)
            => Perlin2D(x, y, scale, octaves, 0.5f, 2f);

        public int RangeInt(int min, int max) => _rng.Next(min, max + 1);
        public float RangeFloat(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

        // Mulberry32 deterministic RNG
        private uint _state;
        public uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
