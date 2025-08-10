// not used
using UnityEngine;

public class ChunkHeightMap : MonoBehaviour
{
    public float[,] heightMap;
    public Vector2 worldOrigin;
    public float cellSize = 0.5f;
    public int width = 20;
    public int height = 20;
    public float rayHeight = 100f;

    private Collider[] colliders;

    public void Initialize()
    {
        heightMap = new float[width, height];
        colliders = GetComponentsInChildren<Collider>();

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 rayOrigin = new Vector3(
                    worldOrigin.x + x * cellSize,
                    rayHeight,
                    worldOrigin.y + z * cellSize
                );

                Ray ray = new Ray(rayOrigin, Vector3.down);
                float bestY = float.MinValue;

                foreach (var collider in colliders)
                {
                    if (collider.Raycast(ray, out RaycastHit hit, rayHeight * 2f))
                    {
                        if (hit.point.y > bestY)
                            bestY = hit.point.y;
                    }
                }

                heightMap[x, z] = bestY == float.MinValue ? -1f : bestY;
            }
        }
        Debug.Log($"Colliders:{colliders.Length} ");
    }


    public float GetHeightAtWorld(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - worldOrigin.x) / cellSize);
        int z = Mathf.FloorToInt((worldPosition.z - worldOrigin.y) / cellSize);

        if (x < 0 || x >= width || z < 0 || z >= height)
            return -1f;

        return heightMap[x, z];
    }
}
