using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MusicRun
{
    [Serializable]
    /*
     * FILE ROLE
     * - Core orchestration part of TerrainGenerator (partial class).
     * - Contains terrain state, level selection/creation, start-goal setup,
     *   coordinate conversion, and visible chunk update loop.
     * - Chunk lifecycle and content placement are split into companion files.
     */
    public partial class TerrainGenerator : MonoBehaviour
    {
        public int chunkSize = 20;
        public int renderDistance = 2;
        [Header("Debug Options")]
        public bool disableObstacles = false;
        public bool disableChunkUpdate = false;
        public bool disableChunkPool = false;
        public bool enableLogAndRename = false;

        [Header("Defined Levels")]
        public TerrainLevel[] levels;

        private TerrainLevel currentLevel;
        private int currentIndexLevel;
        public GameObject currentStart;
        private Vector2Int currentChunk;
        public Vector2Int startChunkCoord;
        public Vector2Int goalChunkCoord;
        public GameObject currentGoal;
        private Dictionary<Vector2Int, GameObject> spawnedChunks;
        private Stack<GameObject> chunkPool = new Stack<GameObject>(20);
        private Dictionary<Vector2Int, int> spawnedBonus;
        private Dictionary<Vector2Int, int> spawnedInstrument;
        private GameManager gameManager;

        public TerrainLevel CurrentLevel { get => currentLevel; }

        public float timeCreateChunk;
        public float timeAverageCreate;
        public int chunkCreatedCount;
        public int chunkReusedCount;
        public int chunkDeletedCount;
        public int chunkPooledCount;

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
        }


        void Start()
        {
        }

        public void ResetTerrain()
        {
            Debug.Log("ResetTerrain");
            ClearChunks(0);
            ClearChunkPool();
            spawnedChunks = new Dictionary<Vector2Int, GameObject>();
            spawnedBonus = new Dictionary<Vector2Int, int>();
            spawnedInstrument = new Dictionary<Vector2Int, int>();
            // Updated in UpdateChunks() from PlayerController.Update() but we can wait to reset position.
            currentChunk = Vector2Int.zero;
        }

        /// <summary>
        /// Calculates the next level index to be selected.
        /// </summary>
        /// <param name="levelIndex"></param>
        /// <returns></returns>
        public int CalculateNextLevel(int levelIndex)
        {
            Debug.LogWarning($"terrain_SelectNextLevel from index {levelIndex}");

            bool oneLevelEnabledAtLeast = false;
            foreach (TerrainLevel level in levels) if (level.enabled) { oneLevelEnabledAtLeast = true; break; }

            if (!oneLevelEnabledAtLeast)
            {
                currentIndexLevel = 0;
                return currentIndexLevel;
            }

            while (true)
            {
                levelIndex++;
                if (levelIndex < 0 || levelIndex >= levels.Length)
                {
                    levelIndex = 0;
                    Debug.LogWarning("terrain_SelectNextLevel Back to first level");
                }
                if (levels[levelIndex].enabled)
                    break;
            }
            currentIndexLevel = levelIndex;
            return currentIndexLevel;
        }

        public void CreateBigTerrain()
        {
            ResetTerrain();
            CreateLevel(0);
            UpdateChunks(Vector2Int.zero, 20);
        }

        /// <summary>
        /// Creates a level based on the specified index.
        /// </summary>
        /// <param name="index"></param>
        public void CreateLevel(int index)
        {
            Debug.Log($"terrain_createLevel index:{index}");
            disableChunkUpdate = true;

            currentLevel = levels[index];
            gameManager.LiteModeApply();
            CreateStartAndGoalChunk();

            ClearChunkPool();

            if (currentLevel.LoopsToGoal <= 0) currentLevel.LoopsToGoal = 1;
            // Force to update chunks with real position of the player in the playerController update()
            gameManager.playerController.currentPlayerChunk = new Vector2Int(-9999, -9999);
            disableChunkUpdate = false;
        }


        /// <summary>
        /// Creates the start and goal chunks based on the current level configuration.
        /// </summary>
        private void CreateStartAndGoalChunk()
        {
            Debug.Log($"terrain_createStartAndGoalChunk Delta={currentLevel.deltaGoalChunk}");
            disableChunkUpdate = true;

            try
            {
                // Create the start chunk
                // ----------------------
                if (currentLevel.startGO == null)
                {
                    Debug.LogError("Start game object is not set in the level configuration.");
                    return;
                }

                // Move the start chunk to the current player position (which is the current goal)
                currentStart = currentLevel.startGO;
                //currentStart.SetActive(true);
                startChunkCoord = currentChunk;
                currentStart.name = $"start_{currentIndexLevel}_{startChunkCoord.x}_{startChunkCoord.y}";
                currentStart.transform.position = ChunkToPosition(startChunkCoord);
                currentStart.transform.rotation = Quaternion.identity;
                Debug.Log($"terrain_add_start chunk coord: {startChunkCoord} --> GO: {currentStart.name} {currentStart.transform.position}");


                // Create the goal chunk
                // ----------------------
                if (currentLevel.goalGO == null)
                {
                    Debug.LogError("Goal game object is not set in the level configuration.");
                    return;
                }
                currentGoal = currentLevel.goalGO;
                goalChunkCoord = currentChunk + currentLevel.deltaGoalChunk;
                currentGoal.name = $"goal_{currentIndexLevel}_{goalChunkCoord.x}_{goalChunkCoord.y}";
                currentGoal.transform.position = ChunkToPosition(goalChunkCoord);
                currentGoal.transform.rotation = Quaternion.identity;
                Debug.Log($"terrain_add_goal chunk coord: {goalChunkCoord} --> GO: {currentGoal.name} {currentGoal.transform.position}");

                spawnedBonus = new Dictionary<Vector2Int, int>();
                spawnedInstrument = new Dictionary<Vector2Int, int>();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                disableChunkUpdate = false;
            }
        }

        /// <summary>
        /// converts the specified world position to the corresponding chunk coordinates.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2Int PositionToChunk(Vector3 position)
        {
            return new Vector2Int((int)Mathf.Round(position.x / chunkSize), (int)Mathf.Round(position.z / chunkSize));
        }

        /// <summary>
        /// Converts the specified chunk coordinates to the corresponding world position.
        /// </summary>
        /// <param name="chunkCoord">The chunk coordinates to convert. The X and Y components represent the chunk's position in the grid.</param>
        /// <returns>A <see cref="Vector3"/> representing the world-space position of the origin of the specified chunk.</returns>
        public Vector3 ChunkToPosition(Vector2Int chunkCoord)
        {
            return new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize);
        }

        /// <summary>
        /// Updates the set of active terrain chunks based on the specified central chunk coordinate and render
        /// distance. Chunks outside the visible area are removed or pooled as appropriate.
        /// </summary>
        /// <remarks>This method manages the creation, reuse, and removal of terrain chunks to ensure that
        /// only chunks within the specified render distance of the central coordinate remain active. Chunks outside
        /// this area are either pooled for reuse or destroyed if the pool is full. This helps optimize performance by
        /// limiting the number of active chunks based on player position.</remarks>
        /// <param name="chunkUpdate">The central chunk coordinate around which to update visible terrain chunks.</param>
        /// <param name="render">The render distance, in chunks, determining how far from the central chunk to update. If less than or equal
        /// to 0, the default render distance is used.</param>
        public void UpdateChunks(Vector2Int chunkUpdate, int render = 0)
        {
            if (!disableChunkUpdate)
            {
                if (render <= 0) render = renderDistance;
                currentChunk = chunkUpdate;

                // will contains new chunks coordinate around the player at a distance of -renderDistance to renderDistance
                HashSet<Vector2Int> visibleChunks = new HashSet<Vector2Int>();

                //Debug.Log($"terrain_UpdateChunks currentChunk:{currentChunk} start:{startChunkCoord} goal:{goalChunkCoord}  render:{render} chunkPool:{chunkPool.Count} spawnedChunks:{spawnedChunks.Count}");

                DateTime startCreate = DateTime.Now;
                chunkCreatedCount = 0;
                chunkReusedCount = 0;
                chunkDeletedCount = 0;
                chunkPooledCount = 0;

                //Dictionary<Vector2Int, GameObject> createdChunks = new Dictionary<Vector2Int, GameObject>();

                for (int x = -render; x <= render; x++)
                {
                    for (int z = -render; z <= render; z++)
                    {
                        Vector2Int chunkCoord = currentChunk + new Vector2Int(x, z);

                        if (chunkCoord == startChunkCoord)
                        {
                            //Debug.Log($"-terrain- Don't instantiate for start {startChunkCoord}");
                        }
                        else if (chunkCoord == goalChunkCoord)
                        {
                            //Debug.Log($"-terrain- Don't instantiate for goal chunks {goalChunkCoord}");
                        }
                        else
                        {
                            // Does the chunk dictionary already contains this chunk? 
                            if (!spawnedChunks.ContainsKey(chunkCoord))
                            {
                                GameObject newChunk;

                                if (!disableChunkPool && chunkPool.Count > 0)
                                {
                                    // Reuse old one
                                    chunkReusedCount++;
                                    newChunk = ReuseChunk(chunkCoord);
                                }
                                else
                                {
                                    // No available chunk â€” create a new one
                                    chunkCreatedCount++;
                                    newChunk = CreateChunk(chunkCoord);
                                    //createdChunks.Add(chunkCoord, newChunk);
                                }
                                spawnedChunks.Add(chunkCoord, newChunk);
                            }
                            // Chunk in the visible range
                            visibleChunks.Add(chunkCoord);
                            //else
                            //    Debug.Log($"-terrain- Already exist: {currentIndexLevel} {chunkCoord}  chunk: {spawnedChunks[chunkCoord].name}");
                        }
                    }
                }

                //Debug.Log($"terrain_UpdateChunks visibleChunks:{visibleChunks.Count} createdChunks:{createdChunks.Count} spawnedChunks:{spawnedChunks.Count}");

                // Remove from chunk dictionary, chunk out of view which are not in visibleChunks
                var keys = spawnedChunks.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    var coord = keys[i];
                    if (!visibleChunks.Contains(coord))
                    {
                        var obj = spawnedChunks[coord];

                        // If pool not full and not a chunk from previous level then push to pool otherwise destroy
                        // With 5 render chunk distance we have a 21 x 21 chunks overhall (2 x 5 + 1)
                        // So 221 chunks visible, but at each update we can have only 11 new chunks (when player move by one chunk).
                        // Normally a pool of 15 chunks is enough.
                        if (!disableChunkPool && chunkPool.Count < 15 && obj.GetComponent<ChunkInfo>().Level == currentIndexLevel)
                        {
                            //Debug.Log($"terrain_pool_push pool:{chunkPool.Count} level:{obj.GetComponent<ChunkInfo>().Level} {coord} {obj.name}");
                            chunkPooledCount++;
                            // Disable instead of destroying
                            SetLTerrainLayerRecursively(obj, TerrainLayer.IgnoreRaycast);
                            //obj.gameObject.SetActive(false);
                            obj.SetActive(false);
                            chunkPool.Push(obj);
                        }
                        else
                        {
                            //Debug.Log($"terrain_pool_push destroy:{chunkPool.Count} level:{obj.GetComponent<ChunkInfo>().Level} {coord} {obj.name}");
                            chunkDeletedCount++;
                            DestroyChunk(obj);
                        }
                        spawnedChunks.Remove(coord);
                    }
                }

                if (enableLogAndRename)
                {
                    timeCreateChunk = (float)(DateTime.Now - startCreate).TotalMilliseconds;
                    if (chunkCreatedCount != 0)
                        timeAverageCreate = timeCreateChunk / chunkCreatedCount;
                    else
                        timeAverageCreate = 0;
                    Debug.Log($"terrain_created:{chunkCreatedCount,3} reused:{chunkReusedCount,3} pooled:{chunkPooledCount,3} del:{chunkDeletedCount,3} inPool:{chunkPool.Count,3} inSpawn:{spawnedChunks.Count,3} - overall time:{timeCreateChunk,6:F2} ms timeAverageCreate:{timeAverageCreate,6:F2}");
                }
            }
        }

    }

    [Serializable]
    public class Vegetable
    {
        public GameObject vegetable;
        public int count;
    }
}
