using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicRun
{
    public class TerrainGenerator : MonoBehaviour
    {
        public int chunkSize = 20;
        public int renderDistance = 2;
        public bool disableObstacles = false;
        public Level[] levels;

        private Level currentLevel;
        private GameObject currentStart;
        private Vector2Int startChunkCoord;
        private GameObject currentGoal;
        private Vector2Int goalChunkCoord;
        private GameObject[] forestChunks; // tableau de prefabs
        private Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
        private Vector2Int currentPlayerChunk;

        private GameManager gameManager;
        private PlayerController player;

        public GameObject StartGO { get => currentStart; }
        public Level CurrentLevel { get => currentLevel; }

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
        }


        void Start()
        {
        }

        public int SelectNextLevel(int levelIndex)
        {
            levelIndex++;
            if (levelIndex < 0 || levelIndex >= levels.Length)
            {
                levelIndex = 0;
                Debug.LogWarning("Back to first level");
            }
            return levelIndex;
        }
        public void CreateLevel(int levelIndex)
        {
            currentLevel = levels[levelIndex];
            forestChunks = currentLevel.runChunks;
            Debug.Log($"Start Level: {currentLevel.name} - {currentLevel.description}");
            CreateStartAndGoalChunk();
            // Force to update chunks with real position of the player
            //currentPlayerChunk = new Vector2Int(-9999, -9999);
            UpdateChunks();
        }

        private void CreateStartAndGoalChunk()
        {
            Debug.Log($"Create Start and Goal Chunk Delta={currentLevel.deltaCurrentChunk}");

            // Create the start chunk
            // ----------------------
            if (currentLevel.startGO == null)
            {
                Debug.LogError("Start game object is not set in the level configuration.");
                return;
            }
            currentStart = currentLevel.startGO;
            startChunkCoord = currentPlayerChunk;
            Vector3 position = ChunkToPosition(startChunkCoord);
            currentStart.name = $"start_{startChunkCoord.x}_{startChunkCoord.y}";

            if (spawnedChunks.ContainsKey(startChunkCoord))
            {
                Debug.Log($"Destroy existing start chunk: {startChunkCoord} --> playerChunk: {spawnedChunks[startChunkCoord].name}");
                Destroy(spawnedChunks[startChunkCoord]);
                spawnedChunks.Remove(startChunkCoord);
            }
            currentStart.transform.position = position;
            currentStart.transform.rotation = Quaternion.identity;
            Debug.Log($"Add start chunk coord: {startChunkCoord} --> GO: {currentStart.name} {currentStart.transform.position}");


            // Create the goal chunk
            // ----------------------
            if (currentLevel.goalGO == null)
            {
                Debug.LogError("Goal game object is not set in the level configuration.");
                return;
            }
            currentGoal = currentLevel.goalGO;
            goalChunkCoord = currentPlayerChunk + currentLevel.deltaCurrentChunk; // PositionToChunk(goal.position);
            position = ChunkToPosition(goalChunkCoord);
            currentGoal.name = $"goal_{goalChunkCoord.x}_{goalChunkCoord.y}";
            if (spawnedChunks.ContainsKey(goalChunkCoord))
            {
                Debug.Log($"Destroy existing goal chunk: {goalChunkCoord} --> playerChunk: {spawnedChunks[goalChunkCoord].name}");
                Destroy(spawnedChunks[goalChunkCoord]);
                spawnedChunks.Remove(goalChunkCoord);
            }
            currentGoal.transform.position = position;
            currentGoal.transform.rotation = Quaternion.identity;
            Debug.Log($"Add goal chunk coord: {goalChunkCoord} --> GO: {currentGoal.name} {currentGoal.transform.position}");
        }

        Vector2Int PositionToChunk(Vector3 position)
        {
            return new Vector2Int((int)Mathf.Round(position.x / chunkSize), (int)Mathf.Round(position.z / chunkSize));
        }

        Vector3 ChunkToPosition(Vector2Int chunkCoord)
        {
            return new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize);
        }

        void Update()
        {
            Vector2Int playerChunk = PositionToChunk(player.transform.position);

            if (playerChunk != currentPlayerChunk)
            {
                //Debug.Log($"Player enters in a chunk: x={player.transform.position.x} z={player.transform.position.z} --> playerChunk: {playerChunk}");

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
                //Debug.Log($"Remove {coord}");
                spawnedChunks.Remove(coord);
            }
        }
    }


    [Serializable]
    public class Level
    {
        public string name;
        public string description;
        [Header("Defined MIDI  associated to the level")]
        public int indexMIDI;
        [Range(0.1f, 5f)]
        public float RatioSpeedMusic = 0.3f;
        [Range(0.1f, 5f)]
        public float MinSpeedMusic = 0.1f;
        [Range(0.1f, 5f)]
        public float MaxSpeedMusic = 5f;
        [Header("Delta chunk position with last goal")]
        public Vector2Int deltaCurrentChunk;
        [Header("Defined game object sor start end goal level")]
        public GameObject startGO;
        public GameObject goalGO;
        [Header("Defined levels")]
        public GameObject[] runChunks;
    }
}