using System;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.PDETest
{
    public class PDESolver : MonoBehaviour
    {
        public ComputeShader PDESystem;

        [Serializable]
        public class DisplaySettings
        {
            public Vector2Int MeshSize = new Vector2Int(256, 256);
            public Vector3 MeshScale = new Vector3(1, 1, 1);
            public float MinValue = 0f;
            public float MaxValue = 1f;

            [Header("Field visualization")]
            public int TextureIndex = 0;

            [Range(0, 3)]
            public int TextureChannel = 0;
        }

        [Serializable]
        public class TextureFieldSettings
        {
            public string Key = "_StateTex";
            public RenderTextureFormat Format = RenderTextureFormat.ARGBFloat;
            public FilterMode FilterMode = FilterMode.Point;
            public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
            public Material Initializer;
            public Texture2D InitialState;
        }

        [Serializable]
        public class Property
        {
            public string Name;
            public float Value;
            public float MinValue = -10;
            public float MaxValue = 10;
            public bool Editable = true;
        }
        

        [Serializable]
        public class Kernel
        {
            public string Name = "CSMain";

            [NonSerialized]
            public uint ThreadsPerGroupX;

            [NonSerialized]
            public uint ThreadsPerGroupY;

            [NonSerialized]
            public uint ThreadsPerGroupZ;

            [NonSerialized]
            public int Index;
        }

        [Header("Visuals")]
        public DisplaySettings VisualSettings;

        public Material GridMaterial;

        [Header("State")]
        public Vector2Int StateSize;
        public TextureFieldSettings[] TextureFields;
        public Kernel[] Kernels;
        public Property[] Properties;
        private RenderTexture[] _renderTextures;

        // Brush
        private float _brushSize = .075f;
        private float _brushAmount = 1f;
        private float _brushCurrentAmount = 0f;
        private float _brushX;
        private float _brushY;
        private int _brushMode;
        private Plane _floor = new Plane(Vector3.up, Vector3.zero);

        void Start()
        {
            Initialize();
        }
    
        void Update()
        {
            // Controls
            _brushSize = Mathf.Clamp(_brushSize + Input.mouseScrollDelta.y * Time.deltaTime * 0.2f, 0.01f, 1f);

            _brushCurrentAmount = 0;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (_floor.Raycast(ray, out var enter))
            {
                var hitPoint = ray.GetPoint(enter);
                _brushX = hitPoint.x / VisualSettings.MeshScale.x;
                _brushY = hitPoint.z / VisualSettings.MeshScale.z;

                if (Input.GetMouseButton(0))
                    _brushCurrentAmount = _brushAmount;
            }
        }

        void FixedUpdate()
        {
            

            if (PDESystem != null)
            {
                PDESystem.SetFloat("_BrushSize", _brushSize * StateSize.magnitude);
                PDESystem.SetFloat("_BrushAmount", _brushCurrentAmount);
                PDESystem.SetFloat("_BrushX", _brushX * StateSize.x);
                PDESystem.SetFloat("_BrushY", _brushY * StateSize.y);
                PDESystem.SetInt("_BrushChannel", VisualSettings.TextureChannel);

                foreach (var prop in Properties)
                {
                    PDESystem.SetFloat(prop.Name, prop.Value);
                }

                foreach (var kernel in Kernels)
                {
                    PDESystem.Dispatch(kernel.Index,
                        StateSize.x / (int)kernel.ThreadsPerGroupX,
                        StateSize.y / (int)kernel.ThreadsPerGroupY, 1);
                }
            }
        }

        [ContextMenu("Initialize")]
        public void Initialize()
        {
            // Cleanup
            if (_renderTextures != null && _renderTextures.Length > 0)
            {
                foreach (var renderTexture in _renderTextures)
                    renderTexture.Release();

                _renderTextures = null;
            }

            // Kernel setup
            foreach (var kernel in Kernels)
            {
                // Get kernel ID
                kernel.Index = PDESystem.FindKernel(kernel.Name);

                // Get kernel meta data
                PDESystem.GetKernelThreadGroupSizes(kernel.Index, out kernel.ThreadsPerGroupX, out kernel.ThreadsPerGroupY, out kernel.ThreadsPerGroupZ);
            }
            
            // Texture initialization
            _renderTextures = new RenderTexture[TextureFields.Length];
            var idx = 0;
            foreach (var textureFieldSettings in TextureFields)
            {
                var rt = new RenderTexture(width: StateSize.x, height: StateSize.y, 0, textureFieldSettings.Format)
                {
                    filterMode = textureFieldSettings.FilterMode,
                    wrapMode = textureFieldSettings.WrapMode,
                    enableRandomWrite = true
                };

                rt.Create();

                if (textureFieldSettings.InitialState != null)
                {
                    if(textureFieldSettings.Initializer == null)
                        Graphics.Blit(textureFieldSettings.InitialState, rt);
                    else
                        Graphics.Blit(textureFieldSettings.InitialState, rt, textureFieldSettings.Initializer);
                }

                // Set texture to all kernels in compute shader
                foreach (var kernel in Kernels)
                {
                    Debug.LogFormat("Setting texture {0} for kernel {1}", textureFieldSettings.Key, kernel.Index);
                    PDESystem.SetTexture(kernel.Index, textureFieldSettings.Key, rt);
                }

                _renderTextures[idx++] = rt;
                Debugger.Instance.Display("Textures/" + textureFieldSettings.Key, rt);

                for (uint channel = 0; channel < 4; channel++)
                {
                    SurfacePlot.Create(Vector3.zero + Vector3.right * channel * 1.2f, 
                        Vector3.one, 
                        rt, 
                        channel: channel, 
                        minValue: -20, 
                        maxValue: 20);
                }
            }

            // Mesh
            var mesh = MeshUtils.GeneratePlane(
                Vector3.zero,
                Vector3.right * VisualSettings.MeshScale.x,
                Vector3.forward * VisualSettings.MeshScale.z,
                VisualSettings.MeshSize.x,
                VisualSettings.MeshSize.y,
                Vector2.zero,
                Vector2.one);

            mesh.bounds = new Bounds(VisualSettings.MeshScale * 0.5f, VisualSettings.MeshScale * 0.5f);
        }

        public void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 400));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Brush amount", GUILayout.MaxWidth(100));
            GUILayout.Label(_brushAmount.ToString(), GUILayout.MaxWidth(100));
            _brushAmount = GUILayout.HorizontalSlider(_brushAmount, -100f, 100f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Brush size", GUILayout.MaxWidth(100));
            GUILayout.Label(_brushSize.ToString(), GUILayout.MaxWidth(100));
            _brushSize = GUILayout.HorizontalSlider(_brushSize, 0.01f, 0.2f);
            GUILayout.EndHorizontal();

            foreach (var property in Properties)
            {
                if(!property.Editable)
                    continue;
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(property.Name, GUILayout.MaxWidth(100));
                GUILayout.Label(property.Value.ToString(), GUILayout.MaxWidth(100));
                property.Value = GUILayout.HorizontalSlider(property.Value, property.MinValue, property.MaxValue);
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndArea();
        }
    }
}
