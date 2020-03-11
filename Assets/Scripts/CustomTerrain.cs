using System;
using Assets.Scripts.Utils;
using UnityEngine;

public class CustomTerrain : MonoBehaviour
{
    [Header("Main settings")]
    public Material[] Materials;
    public ComputeShader ErosionComputeShader;
    public Texture2D InitialState;
    public Material InitHeightMap;
    public Texture2D RainMap;

    [Range(32, 1024)]
    public int Width = 256;
    [Range(32, 1024)]
    public int Height = 256;

    public float BrushAmount = 0f;
        
    public InputModes InputMode = InputModes.AddWater;

    public enum InputModes : int
    {
        AddWater = 0,
        RemoveWater = 1,
        AddTerrain = 2,
        RemoveTerrain = 3
    }

    [Serializable]
    public class SimulationSettings
    {
        [Range(0f, 10f)]
        public float TimeScale = 1f;

        public float PipeLength = 1f / 256;
        public Vector2 CellSize = new Vector2(1f / 256, 1f / 256);

        [Range(0, 0.5f)]
        public float RainRate = 0.012f;

        [Range(0, 1f)]
        public float Evaporation = 0.015f;

        [Range(0.001f, 1000)]
        public float PipeArea = 20;

        [Range(0.1f, 20f)]
        public float Gravity = 9.81f;

        [Header("Hydraulic erosion")]
        [Range(0.1f, 3f)]
        public float SedimentCapacity = 1f;

        [Range(0.1f, 2f)]
        public float SoilSuspensionRate = 0.5f;

        [Range(0.1f, 3f)]
        public float SedimentDepositionRate = 1f;

        [Range(0f, 10f)]
        public float SedimentSofteningRate = 5f;

        [Range(0f, 40f)]
        public float MaximalErosionDepth = 10f;

        [Header("Thermal erosion")]
        [Range(0, 1000f)]
        public float ThermalErosionTimeScale = 1f;

        [Range(0, 1f)]
        public float ThermalErosionRate = 0.15f;

        [Range(0f, 1f)]
        public float TalusAngleTangentCoeff = 0.8f;

        [Range(0f, 1f)]
        public float TalusAngleTangentBias = 0.1f;
            
    }
        
    public SimulationSettings Settings;

    // Computation stuff
    // State texture ARGBFloat
    // R - surface height  [0, +inf]
    // G - water over surface height [0, +inf]
    // B - Suspended sediment amount [0, +inf]
    // A - Hardness of the surface [0, 1]
    private RenderTexture _stateTexture;

    // Output water flux-field texture
    // represents how much water is OUTGOING in each direction
    // R - flux to the left cell [0, +inf]
    // G - flux to the right cell [0, +inf]
    // B - flux to the top cell [0, +inf]
    // A - flux to the bottom cell [0, +inf]
    private RenderTexture _waterFluxTexture;

    // Output terrain flux-field texture
    // represents how much landmass is OUTGOING in each direction
    // Used in thermal erosion process
    // R - flux to the left cell [0, +inf]
    // G - flux to the right cell [0, +inf]
    // B - flux to the top cell [0, +inf]
    // A - flux to the bottom cell [0, +inf]
    private RenderTexture _terrainFluxTexture;

    // Velocity texture
    // R - X-velocity [-inf, +inf]
    // G - Y-velocity [-inf, +inf]
    private RenderTexture _velocityTexture;

    // List of kernels in the compute shader to be dispatched
    // Sequentially in this order
    private readonly string[] _kernelNames = {
        "RainAndControl",
        "FluxComputation",
        "FluxApply",
        "HydraulicErosion",
        "SedimentAdvection",
        "ThermalErosion",
        "ApplyThermalErosion"
    };

    // Kernel-related data
    private int[] _kernels;
    private uint _threadsPerGroupX;
    private uint _threadsPerGroupY;
    private uint _threadsPerGroupZ;

    // Rendering stuff
    private const string StateTextureKey = "_StateTex";

    // Brush
    private Plane _floor = new Plane(Vector3.up, Vector3.zero);
    private float _brushRadius = 0.1f;
    private Vector4 _inputControls;

    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;

        // Set everything up
        Initialize();
    }
        
    void Update()
    {
        // Controls
        _brushRadius = Mathf.Clamp(_brushRadius + Input.mouseScrollDelta.y * Time.deltaTime * 0.2f, 0.01f, 1f);

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var amount = 0f;
        var brushX = 0f;
        var brushY = 0f;

        if (_floor.Raycast(ray, out var enter))
        {
            var hitPoint = ray.GetPoint(enter);
            brushX = hitPoint.x / Width;
            brushY = hitPoint.z / Height;

            if (Input.GetMouseButton(0))
                amount = BrushAmount;
        }
        else
        {
            amount = 0f;
        }

        _inputControls = new Vector4(brushX, brushY, _brushRadius, amount);
        Shader.SetGlobalVector("_InputControls", _inputControls);
    }

    void FixedUpdate()
    {
        // Compute dispatch
        if (ErosionComputeShader != null)
        {
            if (Settings != null)
            {
                // General parameters
                ErosionComputeShader.SetFloat("_TimeDelta", Time.fixedDeltaTime * Settings.TimeScale);
                ErosionComputeShader.SetFloat("_PipeArea", Settings.PipeArea);
                ErosionComputeShader.SetFloat("_Gravity", Settings.Gravity);
                ErosionComputeShader.SetFloat("_PipeLength", Settings.PipeLength);
                ErosionComputeShader.SetVector("_CellSize", Settings.CellSize);
                ErosionComputeShader.SetFloat("_Evaporation", Settings.Evaporation);
                ErosionComputeShader.SetFloat("_RainRate", Settings.RainRate);

                // Hydraulic erosion
                ErosionComputeShader.SetFloat("_SedimentCapacity", Settings.SedimentCapacity);
                ErosionComputeShader.SetFloat("_MaxErosionDepth", Settings.MaximalErosionDepth);
                ErosionComputeShader.SetFloat("_SuspensionRate", Settings.SoilSuspensionRate);
                ErosionComputeShader.SetFloat("_DepositionRate", Settings.SedimentDepositionRate);
                ErosionComputeShader.SetFloat("_SedimentSofteningRate", Settings.SedimentSofteningRate);

                // Thermal erosion
                ErosionComputeShader.SetFloat("_ThermalErosionRate", Settings.ThermalErosionRate);
                ErosionComputeShader.SetFloat("_TalusAngleTangentCoeff", Settings.TalusAngleTangentCoeff);
                ErosionComputeShader.SetFloat("_TalusAngleTangentBias", Settings.TalusAngleTangentBias);
                ErosionComputeShader.SetFloat("_ThermalErosionTimeScale", Settings.ThermalErosionTimeScale);

                // Inputs
                ErosionComputeShader.SetVector("_InputControls", _inputControls);
                ErosionComputeShader.SetInt("_InputMode", (int)InputMode);
            }

            // Dispatch all passes sequentially
            foreach (var kernel in _kernels)
            {
                ErosionComputeShader.Dispatch(kernel,
                    _stateTexture.width / (int)_threadsPerGroupX,
                    _stateTexture.height / (int)_threadsPerGroupY, 1);
            }
        }
    }

    [ContextMenu("Initialize")]
    public void Initialize()
    {
        /* ========= Setup computation =========== */
        // If there are already existing textures - release them
        if (_stateTexture != null)
            _stateTexture.Release();

        if (_waterFluxTexture != null)
            _waterFluxTexture.Release();

        if (_velocityTexture != null)
            _velocityTexture.Release();

        // Initialize texture for storing height map
        _stateTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        // Initialize texture for storing flow
        _waterFluxTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Initialize texture for storing flow for thermal erosion
        _terrainFluxTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        // Velocity texture
        _velocityTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        if (!_stateTexture.IsCreated())
            _stateTexture.Create();

        if (!_waterFluxTexture.IsCreated())
            _waterFluxTexture.Create();

        if (!_terrainFluxTexture.IsCreated())
            _terrainFluxTexture.Create();

        if (!_velocityTexture.IsCreated())
            _velocityTexture.Create();

        if (InitialState != null)
        {
            if (InitHeightMap != null)
                Graphics.Blit(InitialState, _stateTexture, InitHeightMap);
            else
                Graphics.Blit(InitialState, _stateTexture);
        }
            
        // Setup computation shader
        if (ErosionComputeShader != null)
        {
            _kernels = new int[_kernelNames.Length];
            var i = 0;
            foreach (var kernelName in _kernelNames)
            {
                var kernel = ErosionComputeShader.FindKernel(kernelName);;
                _kernels[i++] = kernel;

                // Set all textures
                ErosionComputeShader.SetTexture(kernel, "HeightMap", _stateTexture);
                ErosionComputeShader.SetTexture(kernel, "VelocityMap", _velocityTexture);
                ErosionComputeShader.SetTexture(kernel, "FluxMap", _waterFluxTexture);
                ErosionComputeShader.SetTexture(kernel, "TerrainFluxMap", _terrainFluxTexture);
            }
                
            ErosionComputeShader.SetInt("_Width", Width);
            ErosionComputeShader.SetInt("_Height", Height);
            ErosionComputeShader.GetKernelThreadGroupSizes(_kernels[0], out _threadsPerGroupX, out _threadsPerGroupY, out _threadsPerGroupZ);
                
        }

        // Debug information
        Debugger.Instance.Display("Width", Width);
        Debugger.Instance.Display("Height", Height);
        Debugger.Instance.Display("HeightMap", _stateTexture);
        Debugger.Instance.Display("FluxMap", _waterFluxTexture);
        Debugger.Instance.Display("TerrainFluxMap", _terrainFluxTexture);
        Debugger.Instance.Display("VelocityMap", _velocityTexture);


        /* ========= Setup Rendering =========== */
        // Assign state texture to materials
        foreach (var material in Materials)
        {
            material.SetTexture(StateTextureKey, _stateTexture);
        }
    }

    public void OnGUI()
    {
        var inputModes = new[] { "Add water", "Remove water", "Add Terrain", "Remove terrain" };
        GUILayout.BeginArea(new Rect(10, 10, 400, 400));
        InputMode = (InputModes)GUILayout.Toolbar((int)InputMode, inputModes);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Brush strength");
        BrushAmount = GUILayout.HorizontalSlider(BrushAmount, 1f, 100f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Brush size");
        _brushRadius = GUILayout.HorizontalSlider(_brushRadius, 0.01f, 0.2f);
        GUILayout.EndHorizontal();

        GUILayout.Label("[W][A][S][D] - Fly, hold [Shift] - fly faster");
        GUILayout.Label("hold [RMB] - rotate camera");
        GUILayout.Label("[LMB] - draw");
        GUILayout.EndArea();
    }
}