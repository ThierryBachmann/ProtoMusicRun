using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicRun
{
    public class TerrainGenerator : MonoBehaviour
    {
        public Transform player;
        public GameObject goalChunk;
        public GameObject[] forestChunks; // tableau de prefabs
        public Transform goal;
        public Transform start;
        public int chunkSize = 20;
        public int renderDistance = 2;
        public bool disableObstacles = false;
        private Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
        private Vector2Int currentPlayerChunk;

        void Start()
        {
            CreateStartAndGoalChunk();
        }

        private void CreateStartAndGoalChunk()
        {
            Debug.Log($"Create Start and Goal Chunk: {start} {goal}");

            if (goal != null)
            {
                Vector2Int chunkCoord = PositionToChunk(goal.position);
                GameObject chunk = Instantiate(goalChunk, goal.position, Quaternion.identity);
                chunk.name = $"Chunk_goal_{chunkCoord.x}_{chunkCoord.y}";
                chunk.tag = "Goal";
                Debug.Log($"Add goal chunk: {goal.position.x} {goal.position.y} --> playerChunk: {chunk}");
                spawnedChunks.Add(chunkCoord, chunk);
            }

            if (start != null)
            {
                Vector2Int chunkCoord = PositionToChunk(start.position);
                GameObject chunk = Instantiate(goalChunk, start.position, Quaternion.identity);
                chunk.name = $"Chunk_start_{chunkCoord.x}_{chunkCoord.y}";
                chunk.tag = "Goal";
                Debug.Log($"Add start chunk: {start.position.x} {start.position.y} --> playerChunk: {chunk}");
                spawnedChunks.Add(chunkCoord, chunk);
            }
        }

        Vector2Int PositionToChunk(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.z / chunkSize));
        }
        Vector3 ChunkToPosition(Vector2Int chunkCoord)
        {
            return new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize);
        }
        void Update()
        {
            Vector2Int playerChunk = PositionToChunk(player.position);

            if (playerChunk != currentPlayerChunk)
            {
                Debug.Log($"Player: {player.position.x} {player.position.y} --> playerChunk: {playerChunk}");

                currentPlayerChunk = playerChunk;
                UpdateChunks();
            }
        }

        void UpdateChunks()
        {
            // will contains chunks coord around the player at a distance of -renderDistance to renderDistance
            HashSet<Vector2Int> newChunks = new HashSet<Vector2Int>();

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector2Int chunkCoord = currentPlayerChunk + new Vector2Int(x, z);
                    newChunks.Add(chunkCoord);

                    // Does the chunk dictionary already contains this chunk?
                    if (!spawnedChunks.ContainsKey(chunkCoord))
                    {
                        // No, add it
                        Vector3 spawnPos = ChunkToPosition(chunkCoord);

                        GameObject randomPrefab = forestChunks[UnityEngine.Random.Range(0, forestChunks.Length)];
                        GameObject chunk = Instantiate(randomPrefab, spawnPos, Quaternion.identity);
                        chunk.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";
                        if (disableObstacles) // useful for test mode
                        {
                            foreach (Collider col in chunk.GetComponentsInChildren<Collider>())
                            {
                                // keeps the GameObject visible, but disables any physical interaction.
                                if (col.CompareTag("Obstacle"))
                                    col.enabled = false;
                            }
                        }
                        spawnedChunks.Add(chunkCoord, chunk);
                    }
                }
            }

            // Remove from chunk dictionary, chunk out of view which are not in newChunks but keep the goal chunk
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            foreach (var coord in spawnedChunks.Keys)
            {
                if (!newChunks.Contains(coord))
                {
                    if (spawnedChunks[coord].tag != "Goal")
                    {
                        Destroy(spawnedChunks[coord]);
                        chunksToRemove.Add(coord);
                    }
                    else
                        Debug.Log("Don't destroy chunk");
                }
            }
            foreach (var coord in chunksToRemove)
            {
                //Debug.Log($"Remove {coord}");
                spawnedChunks.Remove(coord);
            }
        }
    }


    [Serializable]
    public class Level
    {
        public string name;
        public GameObject goalChunk;
        public GameObject[] runChunks;
    }
}