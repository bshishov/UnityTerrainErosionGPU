using System;
using UnityEngine;
using Utils;

public class WaterBoundary : MonoBehaviour
{
    // Should match terrain size
    public Vector3 Size;
    
    // Material with a special WaterBoundary shader
    public Material Material;
    
    // Resolution should match state - texture resolution 
    public Vector2Int Resolution = new Vector2Int(512, 512);

    void Start()
    {
        CreatePlane("Back(Near)", new Vector3(0, 0,0), Vector3.right * Size.x, Resolution.x);
        CreatePlane("Front(Far)", new Vector3(Size.x, 0, Size.z), Vector3.left * Size.x, Resolution.x);
        CreatePlane("Left", new Vector3(0, 0, Size.z),  Vector3.back * Size.z, Resolution.x);
        CreatePlane("Right", new Vector3(Size.x, 0, 0), Vector3.forward * Size.z, Resolution.x);
        
        Material.SetVector("_WorldScale", Size);
    }

    void CreatePlane(string objectName, Vector3 origin, Vector3 horizontalAxis, int resolution)
    {
        var mesh = MeshUtils.GeneratePlane(Vector3.zero, horizontalAxis,
            new Vector3(0, Size.y, 0),
            resolution,
            2,
            Vector2.zero,
            Vector2.one);
        
        var go = new GameObject(objectName);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().material = Material;
        go.transform.SetParent(transform);
        go.transform.localPosition = origin;
    }
}