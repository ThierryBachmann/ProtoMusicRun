using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

namespace MusicRun
{
    public class TerrainGenerator : MonoBehaviour
    {
        public int chunkSize = 20;
        public int renderDistance = 2;
        public bool disableObstacles = false;
        public Level[] levels;
        public Transform player;
        private Level currentLevel;

        public GameObject startGO;
        public Vector2Int startChunkCoord;

        public GameObject goalGO;
        public Vector2Int goalChunkCoord;

        //public Transform currentStartPosition;
        //public GameObject goalTransform;


        private GameObject[] forestChunks; // tableau de prefabs
        //private Transform goal;
        //private Transform start;
        private Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
        private Vector2Int currentPlayerChunk;

        void Start()
        {
            //CreateStartAndGoalChunk();
        }

        public void CreateLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levels.Length)
            {
                Debug.LogError("Invalid level index");
                return;
            }
            currentLevel = levels[levelIndex];
            forestChunks = currentLevel.runChunks;
            Debug.Log($"Start Level: {currentLevel.name} - {currentLevel.description}");
            CreateStartAndGoalChunk();
            // Force to update chunks with real position of the player
            currentPlayerChunk = new Vector2Int(-9999, -9999);
            // UpdateChunks();
        }

        private void CreateStartAndGoalChunk()
        {
            Debug.Log($"Create Start and Goal Chunk Delta={currentLevel.deltaCurrentChunk}");


            // Create the start chunk
            // ----------------------

            //if (startGO == null)
            //{
            //    Debug.LogError("Start game object from the last goal object.");
            //    startGO =Instantiate( goalGO);
            //}
            startGO = currentLevel.startGO;
            startChunkCoord = currentPlayerChunk;
            Vector3 position = ChunkToPosition(startChunkCoord);
            startGO.name = $"Chunk_start_{startChunkCoord.x}_{startChunkCoord.y}";

            //if (spawnedChunks.ContainsKey(startChunkCoord))
            //{
            //    Debug.Log($"Destroy existing start chunk: {startChunkCoord} --> playerChunk: {spawnedChunks[startChunkCoord].name}");
            //    Destroy(spawnedChunks[startChunkCoord]);
            //    spawnedChunks.Remove(startChunkCoord);
            //}
            // spawnedChunks.Add(chunkCoord, startGO);
            startGO.transform.position = position;
            startGO.transform.rotation = Quaternion.identity;
            Debug.Log($"Add start chunk coord: {startChunkCoord} --> GO: {startGO.name} {startGO.transform.position}");


            // Create the goal chunk
            // ----------------------
            goalGO = currentLevel.goalGO;
            if (goalGO == null)
            {
                Debug.LogError("Goal game object is not set in the level configuration.");
                return;
            }
            goalChunkCoord = currentPlayerChunk + currentLevel.deltaCurrentChunk; // PositionToChunk(goal.position);
            position = ChunkToPosition(goalChunkCoord);
            //GameObject chunk = Instantiate(goalChunk, position, Quaternion.identity);
            //GameObject chunk = goalChunk;
            //currentStartPosition = chunk.transform;
            goalGO.name = $"Chunk_goal_{goalChunkCoord.x}_{goalChunkCoord.y}";
            // goalGO.tag = "Goal";
            //if (spawnedChunks.ContainsKey(goalChunkCoord))
            //{
            //    Debug.Log($"Destroy existing goal chunk: {goalChunkCoord} --> playerChunk: {spawnedChunks[goalChunkCoord].name}");
            //    Destroy(spawnedChunks[goalChunkCoord]);
            //    spawnedChunks.Remove(goalChunkCoord);
            //}
            //spawnedChunks.Add(chunkCoord, goalGO);
            goalGO.transform.position = position;
            goalGO.transform.rotation = Quaternion.identity;
            Debug.Log($"Add goal chunk coord: {goalChunkCoord} --> GO: {goalGO.name} {goalGO.transform.position}");
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
                Debug.Log($"Player: x={player.position.x} z={player.position.z} --> playerChunk: {playerChunk}");

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
                    if (!spawnedChunks.ContainsKey(chunkCoord) && chunkCoord != goalChunkCoord && chunkCoord != startChunkCoord)
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
                    //if (spawnedChunks[coord].tag != "Goal")
                    {
                        Destroy(spawnedChunks[coord]);
                        chunksToRemove.Add(coord);
                    }
                    //else Debug.Log("Don't destroy chunk");
                }
            }
            foreach (var coord in chunksToRemove)
            {
                Debug.Log($"Remove {coord}");
                spawnedChunks.Remove(coord);
            }
        }
    }


    [Serializable]
    public class Level
    {
        public string name;
        public string description;
        [Header("Delta with current chunks")]
        public Vector2Int deltaCurrentChunk;
        [Header("Defined prefab game object")]
        public GameObject startGO;
        public GameObject goalGO;
        public GameObject[] runChunks;
    }
}