using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.PDETest
{
    public class SurfacePlot : MonoBehaviour
    {
        public Vector3 Size;
        public float MinValue;
        public float MaxValue;
        public Texture ActiveTexture;
        public uint ActiveChannel;
        public Vector2Int MeshResolution = new Vector2Int(128, 128);

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private GameObject[] _grids;
        private Material _surfaceMaterial;
        private Material _gridMaterial;

        void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            
            _gridMaterial = new Material(Shader.Find("Plot/Grid"));
            _surfaceMaterial = new Material(Shader.Find("Plot/Surface"));

            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();

            if (_meshRenderer == null)
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            _meshRenderer.material = _surfaceMaterial;

            Initialize();
        }
        
        void FixedUpdate()
        {
            if (_surfaceMaterial != null)
            {
                _surfaceMaterial.mainTexture = ActiveTexture;
                _surfaceMaterial.SetInt("_Channel", (int) ActiveChannel);
                _surfaceMaterial.SetFloat("_HeightScale", Size.y);
                _surfaceMaterial.SetFloat("_MinValue", MinValue);
                _surfaceMaterial.SetFloat("_MaxValue", MaxValue);
            }
        }

        [ContextMenu("Initialize")]
        void Initialize()
        {
            // Surface mesh
            var mesh = MeshUtils.GeneratePlane(
                Vector3.zero,
                Vector3.right * Size.x,
                Vector3.forward * Size.z,
                MeshResolution.x,
                MeshResolution.y,
                Vector2.zero,
                Vector2.one);

            mesh.bounds = new Bounds(Size * 0.5f, Size * 0.5f);
            _meshFilter.sharedMesh = mesh;


            if (_grids != null)
            {
                foreach (var grid in _grids)
                {
                    if(grid != null)
                        Destroy(grid);
                }
            }

            var right = Vector3.right * Size.x;
            var up = Vector3.up * Size.y;
            var forward = Vector3.forward * Size.z;

            _grids = new []
            {
                CreateGridPlane(Vector3.zero, right, forward),
                CreateGridPlane(Vector3.zero, up, right),
                CreateGridPlane(Vector3.zero, forward, up),
                CreateGridPlane(forward, right, up),
                CreateGridPlane(right, up, forward)
            };
        }

        GameObject CreateGridPlane(Vector3 origin, Vector3 axis0, Vector3 axis1)
        {
            var mesh = MeshUtils.GeneratePlane(
                origin,
                axis0, axis1,
                2, 2,
                Vector2.zero,
                Vector2.one);
            mesh.RecalculateBounds();

            var go = new GameObject("Grid");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _gridMaterial;
            go.transform.SetParent(transform, false);
            return go;
        }

        public static SurfacePlot Create(
            Vector3 position, 
            Vector3 size,
            Texture texture,
            uint channel=0,
            float minValue = 0f,
            float maxValue = 1f)
        {
            var go = new GameObject(string.Format("SurfacePlot {0}:{1}", texture.name, channel));
            go.transform.position = position;
            var sp = go.AddComponent<SurfacePlot>();
            sp.Size = size;
            sp.ActiveTexture = texture;
            sp.ActiveChannel = channel;
            sp.MinValue = minValue;
            sp.MaxValue = maxValue;
            sp.MeshResolution = new Vector2Int(
                Mathf.Max(128, texture.width), 
                Mathf.Max(128, texture.height)
                );

            return sp;
        }

        public void SetGridColor(Color color)
        {
            if (_gridMaterial != null)
            {
                _gridMaterial.color = color;
            }
        }

        public void SetGridBackgroundColor(Color color)
        {
            if (_gridMaterial != null)
            {
                _gridMaterial.SetColor("_BackgroundColor", color);
            }
        }

        public void SetGridTicks(int lines)
        {
            if (_gridMaterial != null && lines > 0)
            {
                _gridMaterial.SetFloat("_LineDistance", 1f / lines);
            }
        }
    }
}
