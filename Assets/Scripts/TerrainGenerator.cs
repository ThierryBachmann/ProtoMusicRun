using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    public Transform player;
    public GameObject[] forestChunks; // tableau de prefabs
    public int chunkSize = 20;
    public int renderDistance = 2;

    private Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int currentPlayerChunk;

    void Update()
    {
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(playerPos.x / chunkSize),
            Mathf.FloorToInt(playerPos.y / chunkSize)
        );
        //Debug.Log($"{playerPos.x} {playerPos.y} {playerChunk}");
        if (playerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = playerChunk;
            UpdateChunks();
        }
    }

    void UpdateChunks()
    {
        HashSet<Vector2Int> newChunks = new HashSet<Vector2Int>();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int chunkCoord = currentPlayerChunk + new Vector2Int(x, z);
                newChunks.Add(chunkCoord);

                if (!spawnedChunks.ContainsKey(chunkCoord))
                {
                    Vector3 spawnPos = new Vector3(
                        chunkCoord.x * chunkSize,
                        0,
                        chunkCoord.y * chunkSize
                    );

                    GameObject randomPrefab = forestChunks[Random.Range(0, forestChunks.Length)];
                    GameObject chunk = Instantiate(randomPrefab, spawnPos, Quaternion.identity);
                    chunk.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";
                    spawnedChunks.Add(chunkCoord, chunk);
                }
            }
        }

        // Supprimer les chunks hors de portée
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var coord in spawnedChunks.Keys)
        {
            if (!newChunks.Contains(coord))
            {
                Destroy(spawnedChunks[coord]);
                chunksToRemove.Add(coord);
            }
        }
        foreach (var coord in chunksToRemove)
        {
            spawnedChunks.Remove(coord);
        }
    }
}
