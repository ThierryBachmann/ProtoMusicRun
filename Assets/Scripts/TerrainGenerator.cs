using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TerrainGenerator : MonoBehaviour
{
    public GameObject[] forestChunks;
    public Transform player;
    public float chunkLength = 20f;
    public int chunksAhead = 5;
    private List<GameObject> activeChunks = new();

    void Update()
    {
        int playerIndex = Mathf.FloorToInt(player.position.z / chunkLength);

        for (int i = -1; i <= chunksAhead; i++)
        {
            int index = playerIndex + i;
            if (!ChunkExists(index))
            {
                Vector3 pos = new Vector3(0, 0, index * chunkLength);
                GameObject chunk = Instantiate(forestChunks[Random.Range(0, forestChunks.Length)], pos, Quaternion.identity);
                chunk.name = "Chunk_" + index;
                activeChunks.Add(chunk);
            }
        }

        CleanupOldChunks(playerIndex);
    }

    bool ChunkExists(int index) => activeChunks.Any(c => c.name == "Chunk_" + index);

    void CleanupOldChunks(int currentIndex)
    {
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            GameObject chunk = activeChunks[i];
            int chunkIndex = int.Parse(chunk.name.Split('_')[1]);
            if (chunkIndex < currentIndex - 2)
            {
                Destroy(chunk);
                activeChunks.RemoveAt(i);
            }
        }
    }
}