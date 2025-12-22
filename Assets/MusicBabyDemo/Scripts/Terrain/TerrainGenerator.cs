using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MusicRun
{
    [Serializable]
    public class TerrainGenerator : MonoBehaviour
    {
        public int chunkSize = 20;
        public int renderDistance = 2;
        public bool disableObstacles = false;
        public bool disableChunkUpdate = false;

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

                                //if (chunkPool.Count > 0)
                                //{
                                //    // Reuse old one
                                //    chunkReusedCount++;
                                //    newChunk = ReuseChunk(chunkCoord);
                                //}
                                //else
                                {
                                    // No available chunk — create a new one
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
                        if (chunkPool.Count < 15 && obj.GetComponent<ChunkInfo>().Level == currentIndexLevel)
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

                timeCreateChunk = (float)(DateTime.Now - startCreate).TotalMilliseconds;
                if (chunkCreatedCount != 0)
                    timeAverageCreate = timeCreateChunk / chunkCreatedCount;
                else
                    timeAverageCreate = 0;
                Debug.Log($"terrain_created:{chunkCreatedCount,3} reused:{chunkReusedCount,3} pooled:{chunkPooledCount,3} del:{chunkDeletedCount,3} inPool:{chunkPool.Count,3} inSpawn:{spawnedChunks.Count,3} - overall time:{timeCreateChunk,6:F2} ms timeAverageCreate:{timeAverageCreate,6:F2}");
            }
        }

        private GameObject ReuseChunk(Vector2Int chunkCoord)
        {
            GameObject usedChunk = chunkPool.Pop();
            SetLTerrainLayerRecursively(usedChunk, TerrainLayer.TerrainCurrent);
            usedChunk.SetActive(true);
            Vector3 spawnPos = ChunkToPosition(chunkCoord);
            usedChunk.transform.position = spawnPos;
            //usedChunk.name = $"Chunk-L:{currentIndexLevel}-at:{chunkCoord.x}/{chunkCoord.y} (from pool)";
            if (gameManager.midiManager.InstrumentRestored >= gameManager.midiManager.InstrumentFound)
                RemoveInstrument(usedChunk);
            //Debug.Log($"terrain_reused pooled chunk {usedChunk.name}");
            return usedChunk;
        }

        private GameObject CreateChunk(Vector2Int chunkCoord)
        {
            // No, add it
            Vector3 spawnPos = ChunkToPosition(chunkCoord);

            // Instantiate a random prefab from the current level's runChunks
            GameObject chunkPrefabRandom = currentLevel.runChunks[UnityEngine.Random.Range(0, currentLevel.runChunks.Length)];
            GameObject createdChunk = Instantiate(chunkPrefabRandom, spawnPos, Quaternion.identity);
            createdChunk.name = $"Chunk-L:{currentIndexLevel}-at:{chunkCoord.x}/{chunkCoord.y}-{chunkPrefabRandom.name}";
            createdChunk.GetComponent<ChunkInfo>().Level = currentIndexLevel;
            SetLTerrainLayerRecursively(createdChunk, TerrainLayer.TerrainCurrent);
            Debug.Log($"terrain_create IndexLevel: {currentIndexLevel} chunkCoord: {chunkCoord} name: '{createdChunk.name}' prefab: '{chunkPrefabRandom.name}'");

            if (disableObstacles) // useful for test mode
            {
                foreach (Collider col in createdChunk.GetComponentsInChildren<Collider>())
                {
                    // keeps the GameObject visible, but disables any physical interaction.
                    // To disable collision detection, we can disable the collider component.
                    // But add tag "Obstacle" to the collision object associated to the gameobject.
                    if (col.CompareTag("Obstacle"))
                        col.enabled = false;
                }
            }

            PlaceAndScaleExistingVege(chunkCoord, createdChunk);

            // Experimentale, to be done: create specific scale and random position in Vegetable class
            // CreateAndScaleVege(chunkCoord, createdChunk);


            // Generate and place bonus.
            // When a chunk is re-generated (player return), no bonus are generated.
            // ---------------------------------------------------------------------
            if (currentLevel.bonusScorePrefab.Length > 0 && currentLevel.bonusMalusDensity > 0f && !spawnedBonus.ContainsKey(chunkCoord))
            {
                AddBonusMalus(chunkCoord, createdChunk, currentLevel.bonusMalusDensity, currentLevel.bonusMalusRatio, currentLevel.bonusScorePrefab);
            }
            if (currentLevel.bonusInstrumentPrefab.Length > 0 && currentLevel.bonusInstrumentDensity > 0f &&
                gameManager.midiManager.InstrumentRestored < gameManager.midiManager.InstrumentFound)
            {
                AddInstrument(chunkCoord, createdChunk, currentLevel.bonusInstrumentDensity, currentLevel.bonusInstrumentPrefab);
            }

            return createdChunk;
        }

        public void DestroyChunk(GameObject toDestroy)
        {
            //Debug.Log($"   terrain_ClearChunks {toDestroy.name}");
            toDestroy.name = "DESTROYED_" + toDestroy.name;
            SetLTerrainLayerRecursively(toDestroy, TerrainLayer.IgnoreRaycast);
            Destroy(toDestroy);
        }
        static void SetLTerrainLayerRecursively(GameObject obj, int layer)
        {

            if (obj.CompareTag("Terrain"))
                obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLTerrainLayerRecursively(child.gameObject, layer);
        }
        // 
        private void AddBonusMalus(Vector2Int chunkCoord, GameObject chunk, float density, float ratio, GameObject[] prefab)
        {
            //Debug.Log($"AddBonusMalus density:{density:F1} ratio:{ratio:F1}");
            int count = 1;
            if (density < 1)
            {
                // Special case when count is inferior to 1.
                // Create 0 or 1 bonus with probability from BonusCount
                if (UnityEngine.Random.Range(0f, 1f) < density)
                    count = 1;
                else
                    count = 0;
            }
            else
                count = (int)density;
            // At least one bonus for this chunk
            if (count > 0)
            {
                for (int i = 0; i < density; i++)
                {
                    int indexPrefab;
                    if (prefab.Length == 1)
                        indexPrefab = 0;
                    else
                    {
                        // Ratio between Bonus and Malus: 0 only bonus, 1 only malus
                        if (UnityEngine.Random.Range(0f, 1f) > ratio)
                            indexPrefab = 1;
                        else
                            indexPrefab = 0;
                    }
                    GameObject bonusPrefabRandom = prefab[indexPrefab];
                    GameObject bonus = Instantiate(bonusPrefabRandom);
                    bonus.transform.SetParent(chunk.transform, false);
                    float maxPos = chunkSize / 2f - 0.1f; // -0.1 to avoid border
                    Vector3 bonusPos = new Vector3(UnityEngine.Random.Range(-maxPos, maxPos), 5f, UnityEngine.Random.Range(-maxPos, maxPos));
                    bonus.transform.SetLocalPositionAndRotation(bonusPos, Quaternion.identity);
                    if (!PositionOnHighestTerrain(bonus.transform, 100f))
                        Debug.LogWarning($"No hit bonus, chunk: {chunkCoord} '{chunk.name}' child: {bonus.name} at {bonusPos}");
                    //bonus.name = $"Bonus-L:{currentIndexLevel}-chunk:{chunkCoord}-localPosition: {bonus.transform.localPosition}";
                }

                // Just keep a trace of bonus count for this chunk to avoid re-generate if player return
                spawnedBonus.Add(chunkCoord, count);

            }
        }
        private void AddInstrument(Vector2Int chunkCoord, GameObject chunk, float density, GameObject[] prefab)
        {
            //Debug.Log($"AddInstrument density:{density:F1}");
            int count = 1;
            if (density < 1)
            {
                // Special case when count is inferior to 1.
                // Create 0 or 1 bonus with probability from BonusCount
                if (UnityEngine.Random.Range(0f, 1f) < density)
                    count = 1;
                else
                    count = 0;
            }
            else
                count = (int)density;
            // At least one bonus for this chunk
            if (count > 0)
            {
                for (int i = 0; i < density; i++)
                {
                    int indexPrefab = UnityEngine.Random.Range(0, prefab.Length);
                    GameObject instrumentPrefabRandom = prefab[indexPrefab];
                    GameObject instrument = Instantiate(instrumentPrefabRandom);
                    instrument.transform.SetParent(chunk.transform, false);
                    Vector3 instrumentPos = new Vector3(UnityEngine.Random.Range(-chunkSize / 2f, chunkSize / 2f), 3f, UnityEngine.Random.Range(-chunkSize / 2f, chunkSize / 2f));
                    instrument.transform.SetLocalPositionAndRotation(instrumentPos, Quaternion.identity);
                    if (!PositionOnHighestTerrain(instrument.transform, 100f, 3f))
                        Debug.LogWarning($"No hit instrument, chunk: {chunkCoord} '{chunk.name}' child: {instrument.name} at {instrumentPos}");
                    //instrument.name = $"Instr-L:{currentIndexLevel}-chunk:{chunkCoord}-localPosition:{instrument.transform.localPosition}";
                }

                // Just keep a trace of bonus count for this chunk to avoid re-generate if player return
                //spawnedInstrument.Add(chunkCoord, count);

            }
        }
        private void RemoveInstrument(GameObject chunk)
        {
            foreach (Transform childTransform in chunk.transform)
            {
                if (childTransform.CompareTag("Instrument"))
                {
                    Destroy(childTransform.gameObject);
                }
            }
        }

        private void PlaceAndScaleExistingVege(Vector2Int chunkCoord, GameObject chunk)
        {

            // Build vegetable. Get list of gameobject exiting in the prefab chunk and apply random or perlin change
            foreach (Transform childTransform in chunk.transform)
            {
                if (childTransform.CompareTag("TreeScalable") || childTransform.CompareTag("Grass"))
                {
                    Vector3 childPosition = childTransform.localPosition;

                    /*
                        Perlin noise (invented by Ken Perlin) is a type of gradient noise — meaning it’s generated by smoothly interpolating 
                        between pseudo-random gradient values at grid points.
                        It’s not random white noise (which changes abruptly each sample), but a continuous, smooth function that produces “organic” patterns.
                    */

                    // Perlin generator for vegetable -  return a value between 0 and 1
                    //  perlinVegetable: how much position are spread on the chunk. 0: all vegetables are at the same place on the current chunk.
                    //  perlinChunk:  how much position are modified between chunk. 0: all vegetables are at the same place for each chunk.
                    float offsetX = Mathf.PerlinNoise(
                        childPosition.x * currentLevel.perlinVegetable + chunkCoord.x * currentLevel.perlinChunk,
                        childPosition.z * currentLevel.perlinVegetable + chunkCoord.y * currentLevel.perlinChunk);
                    float offsetZ = Mathf.PerlinNoise(
                        childPosition.z * currentLevel.perlinVegetable + chunkCoord.x * currentLevel.perlinChunk,
                        childPosition.x * currentLevel.perlinVegetable + chunkCoord.y * currentLevel.perlinChunk);


                    // Position between -chunkSize/2 and chunkSize/2
                    offsetX = offsetX * chunkSize - chunkSize / 2f;
                    offsetZ = offsetZ * chunkSize - chunkSize / 2f;

                    //if (Mathf.Abs(offsetX) > 9f || Mathf.Abs(offsetZ) > 9f)
                    //    Debug.Log($"Chunk: {chunkCoord} Child: {childTransform.name} {childTransform.tag} offset:{offsetX} {offsetZ} ");

                    // Define position and place to the terrain
                    Vector3 localPosition = new Vector3(offsetX, 5f, offsetZ);
                    //Debug.Log($"Chunk: {chunkCoord} Vege: {childTransform.name} localPosition: {localPosition} ");
                    if (childTransform.CompareTag("TreeScalable") || childTransform.CompareTag("Grass"))
                    {
                        currentLevel.maxScaleVegetable = Mathf.Clamp(currentLevel.maxScaleVegetable, 0.1f, 15f);
                        currentLevel.minScaleVegetable = Mathf.Clamp(currentLevel.minScaleVegetable, 0.1f, 15f);
                        if (currentLevel.minScaleVegetable >= currentLevel.maxScaleVegetable)
                            currentLevel.minScaleVegetable = currentLevel.maxScaleVegetable - 0.1f;
                        // Random scale variation (e.g. between 0.8x and 1.2x original size)
                        float randomScale = UnityEngine.Random.Range(currentLevel.minScaleVegetable, currentLevel.maxScaleVegetable);
                        childTransform.localScale = childTransform.localScale * randomScale;
                    }

                    // Random Y rotation (0–360 degrees)
                    float randomY = UnityEngine.Random.Range(0f, 360f);
                    childTransform.localRotation = Quaternion.Euler(0f, randomY, 0f);

                    // Set the local position already set
                    childTransform.localPosition = localPosition;

                    // Search and apply the Y 
                    if (!PositionOnHighestTerrain(childTransform, 100f))
                        Debug.LogWarning($"No hit, chunk: {chunkCoord} '{chunk.name}' child: {childTransform.name} offsetX:{offsetX} offsetZ: {offsetZ} ");
                }
            }
        }

        private void CreateAndScaleVege(Vector2Int chunkCoord, GameObject chunk)
        {

            // Build vegetable. Get list of gameobject exiting in the prefab chunk and apply random or perlin change
            for (int indexVege = 0; indexVege < currentLevel.vegetables.Length; indexVege++)
            {
                for (int indexCount = 0; indexCount < currentLevel.vegetables[indexVege].count; indexCount++)
                {
                    GameObject vege = Instantiate(currentLevel.vegetables[indexVege].vegetable);
                    vege.transform.SetParent(chunk.transform);

                    /*
                        Perlin noise (invented by Ken Perlin) is a type of gradient noise — meaning it’s generated by smoothly interpolating 
                        between pseudo-random gradient values at grid points.
                        It’s not random white noise (which changes abruptly each sample), but a continuous, smooth function that produces “organic” patterns.

                        Perlin generator for vegetable -  return a value between 0 and 1
                            perlinVegetable: how much position are spread on the chunk. 0: all vegetables are at the same place on the current chunk.
                            perlinChunk:  how much position are modified between chunk. 0: all vegetables are at the same place for each chunk.
                    */
                    float localX = Mathf.PerlinNoise(
                        (indexCount + 1) * currentLevel.perlinVegetable + chunkCoord.x * currentLevel.perlinChunk,
                        (indexCount + 1) * currentLevel.perlinVegetable + chunkCoord.y * currentLevel.perlinChunk);
                    float localZ = Mathf.PerlinNoise(
                        (indexCount + 1) * currentLevel.perlinVegetable + chunkCoord.y * currentLevel.perlinChunk,
                        (indexCount + 1) * currentLevel.perlinVegetable + chunkCoord.x * currentLevel.perlinChunk);

                    // Position between -chunkSize/2 and chunkSize/2
                    localX = localX * 1.1f * chunkSize - chunkSize / 2f;
                    localZ = localZ * 1.1f * chunkSize - chunkSize / 2f;

                    Vector3 localPosition = new Vector3(localX, 5f, localZ);
                    vege.transform.localPosition = localPosition;

                    vege.name = $"Vege-{vege.name}-coord:{localPosition.x}-{localPosition.z}";

                    if (!PositionOnHighestTerrain(vege.transform, 100f))
                        Debug.LogWarning($"No hit, chunk: {chunkCoord} child: {vege.name} position:{vege.transform.localPosition} ");


                    //Debug.Log($"Chunk: {chunkCoord} Vege: {vege.name} localPosition: {localPosition} ");
                    if (vege.CompareTag("TreeScalable"))
                    {
                        currentLevel.maxScaleVegetable = Mathf.Clamp(currentLevel.maxScaleVegetable, 0.1f, 15f);
                        currentLevel.minScaleVegetable = Mathf.Clamp(currentLevel.minScaleVegetable, 0.1f, 15f);
                        if (currentLevel.minScaleVegetable >= currentLevel.maxScaleVegetable)
                            currentLevel.minScaleVegetable = currentLevel.maxScaleVegetable - 0.1f;
                        // Random scale variation (e.g. between 0.8x and 1.2x original size)
                        float randomScale = UnityEngine.Random.Range(currentLevel.minScaleVegetable, currentLevel.maxScaleVegetable);
                        vege.transform.localScale = vege.transform.localScale * randomScale;
                    }

                    // Random Y rotation (0–360 degrees)
                    float randomY = UnityEngine.Random.Range(0f, 360f);
                    vege.transform.localRotation = Quaternion.Euler(0f, randomY, 0f);
                }
            }
        }
        private static bool PositionWorldOnHighestTerrain(Transform obj, float maxRayHeight = 100f, float yShift = 0f)
        {
            Vector3 startPos = obj.position + Vector3.up * maxRayHeight;

            RaycastHit topHit;
            if (!Physics.Raycast(startPos, Vector3.down, out topHit, maxRayHeight, TerrainLayer.TerrainCurrentBit))
                Debug.LogWarning($"PositionOnHighestTerrain    --> no hit. From position: {startPos} for '{obj.name}'");
            else
            {
                obj.position = new Vector3(topHit.point.x, topHit.point.y + yShift, topHit.point.z);

                // Debug purpose ....
                //obj.name = obj.name.Substring(0, 6) + "_" + chunk.name + "_" + topHit.transform.name;
                //Debug.Log($"PositionOnHighestTerrain    --> {terrainHits.Length} hit {terrainHits[0].transform.name} world: {topHit.point} local:{obj.localPosition} --> parent: {obj.name}");
            }
            return true;
        }
        public static bool PositionOnHighestTerrain(Transform objTransform, float maxRayHeight = 100f, float yShift = 0f)
        {
            Vector3 startPos = objTransform.position + Vector3.up * maxRayHeight;
            Ray ray = new Ray(startPos, Vector3.down);

            RaycastHit topHit;
            if (!Physics.Raycast(startPos, Vector3.down, out topHit, maxRayHeight * 2f, TerrainLayer.TerrainCurrentBit))
            {
                //Debug.LogWarning($"PositionOnHighestTerrain    --> no hit. From position: {startPos} for '{objTransform.name}'");
                return false;
            }
            else
            {
                // Convert contact world position to local Chunk
                Transform chunk = objTransform;
                if (objTransform.parent != null)
                    chunk = objTransform.parent;
                Vector3 localPos = chunk.InverseTransformPoint(topHit.point);

                objTransform.localPosition = new Vector3(localPos.x, localPos.y + yShift, localPos.z);

                // Debug purpose ....
                //objTransform.name = $"{objTransform.name.Substring(0, 6)}_{chunk.name}_{localPos.y:F1}_{topHit.transform.name}";
                //Debug.Log($"terrain_hits hit:'{topHit.transform.name}' world:{topHit.point} for:'{objTransform.name}' posW:{objTransform.position} --> parent: '{chunk.name}' {chunk.gameObject.activeInHierarchy} {chunk.gameObject.activeSelf}");
            }
            return true;
        }

        /// <summary>
        /// Clear chunks which are at a distance greater than the specified distance from the player.
        /// </summary>
        /// <param name="atDistance"></param>
        public void ClearChunks(float atDistance)
        {
            if (spawnedChunks != null)
            {
                //Debug.Log($"terrain_ClearChunks spawnedChunks:{spawnedChunks.Count}");
                int chunksDestroyed = 0;
                int chunksTotal = spawnedChunks.Count;
                for (int x = -renderDistance; x <= renderDistance; x++)
                {
                    for (int z = -renderDistance; z <= renderDistance; z++)
                    {
                        Vector2Int chunkCoord = currentChunk + new Vector2Int(x, z);
                        if ((chunkCoord - currentChunk).magnitude >= atDistance)
                        {
                            // Does the chunk dictionary contains this chunk? Don't remove for start and goal chunks (in case of ...)).
                            if (spawnedChunks.ContainsKey(chunkCoord))
                            {
                                DestroyChunk(spawnedChunks[chunkCoord]);
                                spawnedChunks.Remove(chunkCoord);
                                chunksDestroyed++;
                            }
                        }
                    }
                }
                //Debug.Log($"terrain_ClearChunks chunksDestroyed:{chunksDestroyed} chunksTotal:{chunksTotal}");
            }
        }
        public void ClearChunkPool()
        {
            if (chunkPool != null)
            {
                //Debug.Log($"terrain_ClearChunkPool {chunkPool.Count} ");
                foreach (var chunk in chunkPool)
                {
                    DestroyChunk(chunk);
                }
                chunkPool.Clear();
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