using System;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts
{
    public class TerrainGenerator : MonoBehaviour
    {
        [Serializable]
        public class ChunkLODSetting
        {
            [Range(4, 200)]
            public int Size = 200;

            [Range(0, 1)]
            public float ScreenHeight;
        }

        public bool GenerateOnStart;

        [Header("Terrain settings")]
        public Vector3 Size = new Vector3(256, 10, 256);
        public int ChunksX = 16;
        public int ChunksZ = 16;
        public ChunkLODSetting[] LODSettings;

        [Header("Materials")]
        public Material[] Materials;
        public bool InstantiateMaterials;

        void Start()
        {
            if(GenerateOnStart)
                Generate();
        }

        [ContextMenu("Generate Terrain")]
        public void Generate()
        {
            var chunkWorldSize = new Vector3(Size.x / ChunksX, Size.y, Size.z / ChunksZ);

            // Clear all children
            foreach (Transform child in transform)
                Destroy(child);

            for (var z = 0; z < ChunksZ; z++)
            {
                for (var x = 0; x < ChunksX; x++)
                {
                    // Chunk texture coordinates
                    var uvStart = new Vector2((float)x / ChunksX, (float)z / ChunksZ);
                    var uvEnd = new Vector2((float)(x + 1) / ChunksX, (float)(z + 1) / ChunksZ);

                    var group = new GameObject(string.Format("Chunk_{0}_{1}", x, z));
                    group.transform.position = new Vector3(x * chunkWorldSize.x, 0, z * chunkWorldSize.z);
                    group.transform.SetParent(transform, false);

                    var lodGroup = group.AddComponent<LODGroup>();
                    lodGroup.fadeMode = LODFadeMode.SpeedTree;
                    //lodGroup.animateCrossFading = true;

                    var lods = new LOD[LODSettings.Length];

                    for (var i = 0; i < LODSettings.Length; i++)
                    {
                        var chunk = GenerateChunk(chunkWorldSize, uvStart, uvEnd, i);
                        chunk.transform.SetParent(group.transform, false);

                        var chunkRenderer = chunk.GetComponent<Renderer>();
                        lods[i] = new LOD(LODSettings[i].ScreenHeight, new Renderer[] { chunkRenderer });
                    }
                    
                    lodGroup.SetLODs(lods);
                    lodGroup.RecalculateBounds();
                }
            }
        }

        GameObject GenerateChunk(Vector3 chunkSize, Vector2 uvStart, Vector2 uvEnd, int lod = 0)
        {
            var chunkObject = new GameObject(string.Format("Terrain_Chunk_LOD{0}", lod));
            var chunkMeshFilter = chunkObject.AddComponent<MeshFilter>();
            var chunkMeshRenderer = chunkObject.AddComponent<MeshRenderer>();

            var w = LODSettings[lod].Size;
            var h = LODSettings[lod].Size;

            var mesh = MeshUtils.GeneratePlane(Vector3.zero, Vector3.right * chunkSize.x, Vector3.forward * chunkSize.z, w, h, uvStart, uvEnd);
            
            mesh.bounds = new Bounds(0.5f * chunkSize, chunkSize);
            chunkMeshFilter.mesh = mesh;
            chunkMeshRenderer.materials = Materials;
            return chunkObject;
        }
    }
}
