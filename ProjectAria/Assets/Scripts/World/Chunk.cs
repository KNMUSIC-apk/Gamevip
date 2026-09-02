// ============================================================
// Chunk.cs
// 16x16x128 voxel chunk. Stores block IDs in flat int[] array.
// Builds a single mesh combining all visible faces (greedy culling).
// Async-ready: build runs in background thread, mesh apply on main.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.World
{
    public class Chunk
    {
        public const int Width = 16;
        public const int Height = 128;
        public const int Area = Width * Height;
        public const int Volume = Width * Height * Width;

        public Vector2Int Coord;
        public Vector3 WorldPosition => new(Coord.x * Width, 0, Coord.y * Width);
        public GameObject GameObject { get; set; }
        public MeshFilter MeshFilter { get; set; }
        public MeshRenderer MeshRenderer { get; set; }
        public MeshCollider MeshCollider { get; set; }
        public bool IsDirty { get; set; } = true;
        public bool IsGenerated { get; set; }
        public bool IsMeshBuilt { get; set; }
        public bool IsVisible { get; set; }
        public float LastTouched { get; set; }

        public int[] Blocks = new int[Volume];
        public byte[] Light = new byte[Volume]; // 0..15 sunlight + 4 bits for block light

        public int GetBlock(int x, int y, int z)
        {
            if ((uint)x >= Width || (uint)y >= Height || (uint)z >= Width) return 0;
            return Blocks[Index(x, y, z)];
        }
        public void SetBlock(int x, int y, int z, int id)
        {
            if ((uint)x >= Width || (uint)y >= Height || (uint)z >= Width) return;
            Blocks[Index(x, y, z)] = id;
            IsDirty = true;
        }

        public static int Index(int x, int y, int z) => (y * Width + z) * Width + x;

        public void ApplyMesh(MeshData data, Material mat)
        {
            if (GameObject == null) return;
            if (MeshFilter.sharedMesh != null) Object.Destroy(MeshFilter.sharedMesh);
            var mesh = new Mesh { name = $"Chunk_{Coord.x}_{Coord.y}" };
            mesh.SetVertices(data.Vertices);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.SetNormals(data.Normals);
            mesh.SetUVs(0, data.UVs);
            mesh.SetColors(data.Colors);
            mesh.RecalculateBounds();
            MeshFilter.sharedMesh = mesh;
            if (MeshCollider != null) MeshCollider.sharedMesh = mesh;
            if (MeshRenderer != null && mat != null) MeshRenderer.sharedMaterial = mat;
            IsMeshBuilt = true;
        }

        public void Unload()
        {
            if (MeshFilter != null && MeshFilter.sharedMesh != null) Object.Destroy(MeshFilter.sharedMesh);
            if (GameObject != null) Object.Destroy(GameObject);
        }
    }

    public class MeshData
    {
        public List<Vector3> Vertices = new();
        public List<int> Triangles = new();
        public List<Vector3> Normals = new();
        public List<Vector2> UVs = new();
        public List<Color> Colors = new();
    }
}
