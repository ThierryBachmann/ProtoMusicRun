using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    public Transform player;
    public GameObject goalChunk;
    public GameObject[] forestChunks; // tableau de prefabs
    public Transform goal;
    public int chunkSize = 20;
    public int renderDistance = 2;
    public bool disableObstacles = false;
    private Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int currentPlayerChunk;

    void Start()
    {
        Vector2 playerPos = new Vector2(goal.position.x, goal.position.z);
        Vector2Int chunkCoord = PositionToChunkPosition(playerPos);
        GameObject chunk = Instantiate(goalChunk, goal.position, Quaternion.identity);
        chunk.name = $"Chunk_goal_{chunkCoord.x}_{chunkCoord.y}";
        chunk.tag = "goal";
        Debug.Log($"Add goal chunk: {playerPos.x} {playerPos.y} --> playerChunk: {chunk}");
        spawnedChunks.Add(chunkCoord, chunk);
    }

    Vector2Int PositionToChunkPosition(Vector2 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));
    }
    Vector3 ChunkPositionToPosition(Vector2Int chunkCoord)
    {
        return new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize);
    }
    void Update()
    {
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        Vector2Int playerChunk = PositionToChunkPosition(playerPos);

        if (playerChunk != currentPlayerChunk)
        {
            Debug.Log($"Player: {playerPos.x} {playerPos.y} --> playerChunk: {playerChunk}");

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
                    Vector3 spawnPos = ChunkPositionToPosition(chunkCoord);

                    GameObject randomPrefab = forestChunks[Random.Range(1, forestChunks.Length)];
                    GameObject chunk = Instantiate(randomPrefab, spawnPos, Quaternion.identity);
                    chunk.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";
                    if (disableObstacles)
                    {
                        foreach (Collider col in chunk.GetComponentsInChildren<Collider>())
                        {
                            // keeps the GameObject visible, but disables any physical interaction.
                            if (col.CompareTag("Obstacle"))
                                col.enabled = false;
                        }
                        //foreach (Transform child in chunk.transform)
                        //{
                        //    child.gameObject.SetActive(false);
                        //    // Do something with childGO
                        //}
                        //GameObject[] go = chunk.GetComponentsInChildren<GameObject>();
                        //foreach (GameObject t in go) t.SetActive(false);
                    }
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
                if (spawnedChunks[coord].tag != "goal")
                {
                    Destroy(spawnedChunks[coord]);
                    chunksToRemove.Add(coord);
                }
                else
                    Debug.Log("Don't destroy goal chunk");
            }
        }
        foreach (var coord in chunksToRemove)
        {
            //Debug.Log($"Remove {coord}");
            spawnedChunks.Remove(coord);
        }
    }
}
