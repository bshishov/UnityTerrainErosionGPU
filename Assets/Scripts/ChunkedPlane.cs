using System;
using System.Collections.Generic;
using Assets.Scripts.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using Utils;

public class ChunkedPlane : MonoBehaviour
{
    [Serializable]
    public class RenderSettings
    {
        public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
        public bool ReceiveShadows = true;
        public bool DynamicOccluded = true;
    }

    [Serializable]
    public class ChunkLODSetting
    {
        [Range(4, 200)]
        public int Size = 200;

        [Range(0, 1)]
        [Tooltip("The screen relative height to use for the transition [0-1].")]
        public float ScreenHeight;

        [Range(0, 1)]
        [Tooltip("Width of the cross-fade transition zone (proportion to the current LOD's whole length) [0-1]. Only used if it's not animated.")]
        public float FadeTransitionWidth = 0.5f;

        [Tooltip("If true, mesh for the current chunk will ignore LOD group of the chunk. May be useful for low-detailed shadow-only meshes")]
        public bool IgnoreLodGroup = false;

        public RenderSettings RenderSettings;
    }

    public bool GenerateOnStart;

    [Header("Terrain settings")]
    public Vector3 Size = new Vector3(256, 10, 256);
    public int ChunksX = 16;
    public int ChunksZ = 16;
    public ChunkLODSetting[] LODSettings;
    public LODFadeMode LodFadeMode = LODFadeMode.CrossFade;
    public bool AnimateLodCrossFading = true;

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
                lodGroup.fadeMode = LodFadeMode;
                lodGroup.animateCrossFading = AnimateLodCrossFading;

                var lods = new List<LOD>();
                for (var i = 0; i < LODSettings.Length; i++)
                {
                    var settings = LODSettings[i];
                    var chunk = GenerateChunk(chunkWorldSize, uvStart, uvEnd, settings, i);
                    chunk.transform.SetParent(group.transform, false);

                    if (!settings.IgnoreLodGroup)
                    {
                        // Add chunk renderer to the LOD group
                        var chunkRenderer = chunk.GetComponent<Renderer>();
                        lods.Add(new LOD(settings.ScreenHeight, new Renderer[] {chunkRenderer})
                        {
                            fadeTransitionWidth = settings.FadeTransitionWidth
                        });
                    }
                }
                    
                lodGroup.SetLODs(lods.ToArray());
                lodGroup.RecalculateBounds();
            }
        }
    }

    private GameObject GenerateChunk(Vector3 chunkSize, Vector2 uvStart, Vector2 uvEnd, ChunkLODSetting settings, int lodLevel)
    {
        var chunkObject = new GameObject($"Terrain_Chunk_LOD{lodLevel}");
        var chunkMeshFilter = chunkObject.AddComponent<MeshFilter>();
        var chunkMeshRenderer = chunkObject.AddComponent<MeshRenderer>();

        // Renderer settings
        chunkMeshRenderer.shadowCastingMode = settings.RenderSettings.ShadowCastingMode;
        chunkMeshRenderer.allowOcclusionWhenDynamic = settings.RenderSettings.DynamicOccluded;
        chunkMeshRenderer.receiveShadows = settings.RenderSettings.ReceiveShadows;

        var mesh = MeshUtils.GeneratePlane(
            Vector3.zero, 
            Vector3.right * chunkSize.x, 
            Vector3.forward * chunkSize.z,
            settings.Size,
            settings.Size, 
            uvStart, uvEnd);
            
        mesh.bounds = new Bounds(0.5f * chunkSize, chunkSize);
        mesh.name = chunkObject.name;

        chunkMeshFilter.mesh = mesh;
        chunkMeshRenderer.materials = Materials;
        return chunkObject;
    }
}