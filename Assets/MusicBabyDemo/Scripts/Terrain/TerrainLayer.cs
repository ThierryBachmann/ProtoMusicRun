using UnityEngine;

static class TerrainLayer
{
    public static readonly int TerrainCurrent;
    public static readonly int TerrainCurrentBit;
    public static readonly int IgnoreRaycast;

    static TerrainLayer()
    {
        TerrainCurrent = LayerMask.NameToLayer("TerrainCurrent");
        if (TerrainCurrent == -1)
            Debug.LogError("Layer 'TerrainCurrent' not found. Please add it to the project layers.");
        else
            TerrainCurrentBit = 1 << TerrainCurrent;

        IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
    }
}
