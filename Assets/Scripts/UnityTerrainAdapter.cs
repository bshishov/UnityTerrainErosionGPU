using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class UnityTerrainAdapter : MonoBehaviour
{
    public Simulation Simulation;
    
    private TerrainData _terrainData;
    private Terrain _terrain;

    private void Start()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
    }

    void Update()
    {
        if(Simulation != null && _terrainData != null)
            Simulation.UpdateSurfaceFromTerrainData(_terrainData);
    }
}