using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
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

        /// <summary>
        /// Calculates the next level index to be selected.
        /// </summary>
        /// <param name="levelIndex"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates a level based on the specified index.
        /// </summary>
        /// <param name="levelIndex"></param>
        public void CreateLevel(int levelIndex)
        {
            currentLevel = levels[levelIndex];
            Debug.Log($"Start Level: {currentLevel.name} - {currentLevel.description}");
            CreateStartAndGoalChunk();
            // Force to update chunks with real position of the player
            //currentPlayerChunk = new Vector2Int(-9999, -9999);
            UpdateChunks();
        }

        /// <summary>
        /// Creates the start and goal chunks based on the current level configuration.
        /// </summary>
        private void CreateStartAndGoalChunk()
        {
            Debug.Log($"Create Start and Goal Chunk Delta={currentLevel.deltaCurrentChunk}");

            ClearChunks(1);

            // Create the start chunk
            // ----------------------
            if (currentLevel.startGO == null)
            {
                Debug.LogError("Start game object is not set in the level configuration.");
                return;
            }

            // Move the start chunk to the current player position (which is the current goal)
            currentStart = currentLevel.startGO;
            startChunkCoord = currentPlayerChunk;
            currentStart.name = $"start_{startChunkCoord.x}_{startChunkCoord.y}";

            // Remove existing chunk in the spawnedChunks dictionary if it exists
            if (spawnedChunks.ContainsKey(startChunkCoord))
            {
                Debug.Log($"Destroy existing start chunk: {startChunkCoord} --> playerChunk: {spawnedChunks[startChunkCoord].name}");
                Destroy(spawnedChunks[startChunkCoord]);
                spawnedChunks.Remove(startChunkCoord);
            }

            currentStart.transform.position = ChunkToPosition(startChunkCoord);
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
            goalChunkCoord = currentPlayerChunk + currentLevel.deltaCurrentChunk;
            currentGoal.name = $"goal_{goalChunkCoord.x}_{goalChunkCoord.y}";
            if (spawnedChunks.ContainsKey(goalChunkCoord))
            {
                Debug.Log($"Destroy existing goal chunk: {goalChunkCoord} --> playerChunk: {spawnedChunks[goalChunkCoord].name}");
                Destroy(spawnedChunks[goalChunkCoord]);
                spawnedChunks.Remove(goalChunkCoord);
            }
            currentGoal.transform.position = ChunkToPosition(goalChunkCoord);
            currentGoal.transform.rotation = Quaternion.identity;
            Debug.Log($"Add goal chunk coord: {goalChunkCoord} --> GO: {currentGoal.name} {currentGoal.transform.position}");
        }

        /// <summary>
        /// converts the specified world position to the corresponding chunk coordinates.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        Vector2Int PositionToChunk(Vector3 position)
        {
            return new Vector2Int((int)Mathf.Round(position.x / chunkSize), (int)Mathf.Round(position.z / chunkSize));
        }

        /// <summary>
        /// Converts the specified chunk coordinates to the corresponding world position.
        /// </summary>
        /// <param name="chunkCoord">The chunk coordinates to convert. The X and Y components represent the chunk's position in the grid.</param>
        /// <returns>A <see cref="Vector3"/> representing the world-space position of the origin of the specified chunk.</returns>
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

                    // Does the chunk dictionary already contains this chunk? Don't instantiate for start and goal chunks.
                    if (!spawnedChunks.ContainsKey(chunkCoord) && chunkCoord != goalChunkCoord && chunkCoord != startChunkCoord)
                    {
                        // No, add it
                        Vector3 spawnPos = ChunkToPosition(chunkCoord);

                        // Instantiate a random prefab from the current level's runChunks
                        GameObject randomPrefab = currentLevel.runChunks[UnityEngine.Random.Range(0, currentLevel.runChunks.Length)];
                        GameObject chunk = Instantiate(randomPrefab, spawnPos, Quaternion.identity);
                        chunk.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";

                        foreach (Transform child in chunk.transform)
                        {
                            if (child.name.StartsWith("DatePalm") || child.name.StartsWith("Sago") || child.name.StartsWith("Grass")|| child.name.StartsWith("Fountain"))
                            {
                                Debug.Log($"Chunk {chunkCoord} randomize child: {child.name} {child.tag}");
                                child.SetLocalPositionAndRotation(new Vector3(
                                    child.localPosition.x + UnityEngine.Random.Range(-2f, 2f),
                                    child.localPosition.y,
                                    child.localPosition.z + UnityEngine.Random.Range(-2f, 2f)), Quaternion.identity);
                            }
                        }
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

        /// <summary>
        /// Clear chunks which are at a distance greater than the specified distance from the player.
        /// </summary>
        /// <param name="atDistance"></param>
        void ClearChunks(int atDistance)
        {
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector2Int chunkCoord = currentPlayerChunk + new Vector2Int(x, z);
                    if ((chunkCoord - currentPlayerChunk).magnitude > atDistance)
                    {
                        // Does the chunk dictionary contains this chunk? Don't remove for start and goal chunks (in case of ...)).
                        if (spawnedChunks.ContainsKey(chunkCoord) && chunkCoord != goalChunkCoord && chunkCoord != startChunkCoord)
                        {
                            //Debug.Log($"ClearChunks {chunkCoord}");
                            Destroy(spawnedChunks[chunkCoord]);
                            spawnedChunks.Remove(chunkCoord);
                        }
                    }
                }
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