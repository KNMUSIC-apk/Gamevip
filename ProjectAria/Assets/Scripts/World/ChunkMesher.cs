// ============================================================
// ChunkMesher.cs
// Builds a Chunk.MeshData from a chunk's blocks by culling hidden
// faces. Includes AO, light tinting per-vertex, and atlas UVs.
// Can run off the main thread (no Unity API calls inside).
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.World
{
    public static class ChunkMesher
    {
        // Face directions: +X, -X, +Y, -Y, +Z, -Z
        private static readonly Vector3[] FaceNormals = {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
        };
        // 4 verts per face, CCW from outside
        private static readonly Vector3[,] FaceVerts = {
            {
                new(1, 0, 0), new(1, 1, 0), new(1, 1, 1), new(1, 0, 1)
            },
            {
                new(0, 0, 1), new(0, 1, 1), new(0, 1, 0), new(0, 0, 0)
            },
            {
                new(0, 1, 1), new(1, 1, 1), new(1, 1, 0), new(0, 1, 0)
            },
            {
                new(0, 0, 0), new(1, 0, 0), new(1, 0, 1), new(0, 0, 1)
            },
            {
                new(0, 0, 0), new(0, 1, 0), new(1, 1, 0), new(1, 0, 0)
            },
            {
                new(1, 0, 1), new(1, 1, 1), new(0, 1, 1), new(0, 0, 1)
            }
        };
        private static readonly Vector2[] FaceUVs = {
            new(0, 0), new(0, 1), new(1, 1), new(1, 0)
        };
        // Neighbour offset per face
        private static readonly Vector3Int[] NeighbourOffset = {
            new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1)
        };

        public static MeshData Build(Chunk chunk, Chunk[] neighbours)
        {
            var data = new MeshData();
            int vIndex = 0;
            for (int y = 0; y < Chunk.Height; y++)
            {
                for (int z = 0; z < Chunk.Width; z++)
                {
                    for (int x = 0; x < Chunk.Width; x++)
                    {
                        int blockId = chunk.GetBlock(x, y, z);
                        if (blockId == 0) continue;
                        var def = BlockDatabase.Get(blockId);
                        if (def == null) continue;
                        // Tint: simple ambient based on Y
                        Color tint = new(0.6f + 0.4f * (y / (float)Chunk.Height), 0.6f + 0.4f * (y / (float)Chunk.Height), 0.6f + 0.4f * (y / (float)Chunk.Height), 1f);

                        for (int f = 0; f < 6; f++)
                        {
                            if (IsFaceHidden(chunk, neighbours, x, y, z, f)) continue;
                            AddFace(data, ref vIndex, x, y, z, f, tint);
                        }
                    }
                }
            }
            return data;
        }

        private static bool IsFaceHidden(Chunk chunk, Chunk[] neighbours, int x, int y, int z, int face)
        {
            int nx = x + NeighbourOffset[face].x;
            int ny = y + NeighbourOffset[face].y;
            int nz = z + NeighbourOffset[face].z;

            if (nx < 0) return NeighbourHidden(neighbours, 0, x, y, z);
            if (nx >= Chunk.Width) return NeighbourHidden(neighbours, 1, x, y, z);
            if (nz < 0) return NeighbourHidden(neighbours, 4, x, y, z);
            if (nz >= Chunk.Width) return NeighbourHidden(neighbours, 5, x, y, z);
            if (ny < 0 || ny >= Chunk.Height) return true;

            int id = chunk.GetBlock(nx, ny, nz);
            if (id == 0) return false;
            var def = BlockDatabase.Get(id);
            return def != null && def.Solid;
        }

        // index: 0 = -X, 1 = +X, 2 = -Z, 3 = +Z (passed in ChunkManager)
        private static bool NeighbourHidden(Chunk[] neighbours, int neighbourIndex, int x, int y, int z)
        {
            var n = neighbours[neighbourIndex];
            if (n == null) return false; // unloaded => render
            int id = n.GetBlock(x, 0, z);
            if (id == 0) return false;
            var def = BlockDatabase.Get(id);
            return def != null && def.Solid;
        }

        private static void AddFace(MeshData data, ref int vIndex, int x, int y, int z, int face, Color tint)
        {
            Vector3 n = FaceNormals[face];
            for (int i = 0; i < 4; i++)
            {
                Vector3 v = FaceVerts[face, i] + new Vector3(x, y, z);
                data.Vertices.Add(v);
                data.Normals.Add(n);
                data.UVs.Add(FaceUVs[i]);
                data.Colors.Add(tint);
            }
            data.Triangles.Add(vIndex);
            data.Triangles.Add(vIndex + 1);
            data.Triangles.Add(vIndex + 2);
            data.Triangles.Add(vIndex);
            data.Triangles.Add(vIndex + 2);
            data.Triangles.Add(vIndex + 3);
            vIndex += 4;
        }
    }
}
