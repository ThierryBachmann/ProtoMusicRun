using MidiPlayerTK;
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
        private GameObject currentStart;
        private Vector2Int currentChunk;
        public Vector2Int startChunkCoord;
        public Vector2Int goalChunkCoord;
        private GameObject currentGoal;
        private Dictionary<Vector2Int, GameObject> spawnedChunks;
        //private Dictionary<Vector2Int, GameObject> freeChunks;
        private Dictionary<Vector2Int, int> spawnedBonus;
        private Dictionary<Vector2Int, int> spawnedInstrument;
        private GameManager gameManager;

        public GameObject StartGO { get => currentStart; }
        public TerrainLevel CurrentLevel { get => currentLevel; }

        public float timeCreateChunk;
        public float timeAverageCreate;
        public int chunkCreatedCount;

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
            ClearChunks(0);
            spawnedChunks = new Dictionary<Vector2Int, GameObject>();
            //freeChunks = new Dictionary<Vector2Int, GameObject>();
            spawnedBonus = new Dictionary<Vector2Int, int>();
            spawnedInstrument = new Dictionary<Vector2Int, int>();
        }

        /// <summary>
        /// Calculates the next level index to be selected.
        /// </summary>
        /// <param name="levelIndex"></param>
        /// <returns></returns>
        public int SelectNextLevel(int levelIndex)
        {
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
                    Debug.LogWarning("-terrain- Back to first level");
                }
                if (levels[levelIndex].enabled)
                    break;
            }
            currentIndexLevel = levelIndex;
            return currentIndexLevel;
        }

        /// <summary>
        /// Creates a level based on the specified index.
        /// </summary>
        /// <param name="levelIndex"></param>
        public void CreateLevel(int levelIndex)
        {
            currentLevel = levels[levelIndex];
            Debug.Log($"-terrain- Start Level: {currentLevel.name} - {currentLevel.description}");
            gameManager.LiteModeApply();
            CreateStartAndGoalChunk();
            if (currentLevel.LoopsToGoal <= 0) currentLevel.LoopsToGoal = 1;
            // Force to update chunks with real position of the player
            gameManager.playerController.currentPlayerChunk = new Vector2Int(-9999, -9999);
            //UpdateChunks(startChunkCoord);
        }

        /// <summary>
        /// Creates the start and goal chunks based on the current level configuration.
        /// </summary>
        private void CreateStartAndGoalChunk()
        {
            Debug.Log($"-terrain- Create Start and Goal Chunk Delta={currentLevel.deltaGoalChunk}");
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
                currentStart.SetActive(true);
                startChunkCoord = currentChunk;
                currentStart.name = $"start_{currentIndexLevel}_{startChunkCoord.x}_{startChunkCoord.y}";
                currentStart.transform.position = ChunkToPosition(startChunkCoord);
                currentStart.transform.rotation = Quaternion.identity;
                Debug.Log($"-terrain- Add start chunk coord: {startChunkCoord} --> GO: {currentStart.name} {currentStart.transform.position}");


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
                Debug.Log($"-terrain- Add goal chunk coord: {goalChunkCoord} --> GO: {currentGoal.name} {currentGoal.transform.position}");

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

        public void UpdateChunks(Vector2Int chunkUpdate)
        {
            if (!disableChunkUpdate)
            {
                currentChunk = chunkUpdate;

                // will contains new chunks coordinate around the player at a distance of -renderDistance to renderDistance
                HashSet<Vector2Int> newChunks = new HashSet<Vector2Int>();

                Debug.Log($"-terrain- create chunks around {currentChunk} start:{startChunkCoord} goal:{goalChunkCoord}  render: {renderDistance}");

                DateTime startCreate = DateTime.Now;
                chunkCreatedCount = 0;
                for (int x = -renderDistance; x <= renderDistance; x++)
                {
                    for (int z = -renderDistance; z <= renderDistance; z++)
                    {
                        Vector2Int chunkCoord = currentChunk + new Vector2Int(x, z);

                        if (chunkCoord.x == startChunkCoord.x && chunkCoord.y == startChunkCoord.y)
                        {
                            // Don't instantiate for start and goal chunks.
                            Debug.Log($"-terrain- Don't instantiate for start {startChunkCoord}");
                        }
                        else if (chunkCoord == goalChunkCoord)
                        {
                            // Don't instantiate for start and goal chunks.
                            Debug.Log($"-terrain- Don't instantiate for goal chunks {goalChunkCoord}");
                        }
                        else
                        {
                            // Does the chunk dictionary already contains this chunk? 
                            if (!spawnedChunks.ContainsKey(chunkCoord))
                            {
                                //DateTime startCreate= DateTime.Now;
                                chunkCreatedCount++;
                                CreateChunk(chunkCoord);
                                newChunks.Add(chunkCoord);
                            }
                            else
                                Debug.Log($"-terrain- Already exist: {currentIndexLevel} {chunkCoord}  chunk: {spawnedChunks[chunkCoord].name}");
                        }
                    }
                }

                timeCreateChunk = (float)(DateTime.Now - startCreate).TotalMilliseconds;
                if (chunkCreatedCount != 0)
                    timeAverageCreate = timeCreateChunk / chunkCreatedCount;
                Debug.Log($"{chunkCreatedCount} - {((chunkCreatedCount != 0) ? timeCreateChunk / chunkCreatedCount : "zero")} ms");

                // Remove from chunk dictionary, chunk out of view which are not in newChunks
                List<Vector2Int> chunksToRemove = new List<Vector2Int>();
                foreach (var coord in spawnedChunks.Keys)
                {
                    if (!newChunks.Contains(coord))
                    {
                        Debug.Log($"-terrain- Destroy {coord} {spawnedChunks[coord].name}");
                        Destroy(spawnedChunks[coord]);
                        chunksToRemove.Add(coord);
                    }
                }
                foreach (var coord in chunksToRemove)
                {
                    Debug.Log($"-terrain- Remove {coord} {spawnedChunks[coord].name}");
                    spawnedChunks.Remove(coord);
                }
            }
        }


        private void CreateChunk(Vector2Int chunkCoord)
        {
            // No, add it
            Vector3 spawnPos = ChunkToPosition(chunkCoord);

            // Instantiate a random prefab from the current level's runChunks
            GameObject chunkPrefabRandom = currentLevel.runChunks[UnityEngine.Random.Range(0, currentLevel.runChunks.Length)];
            GameObject chunkGO = Instantiate(chunkPrefabRandom, spawnPos, Quaternion.identity);
            chunkGO.name = $"Chunk - Level: {currentIndexLevel} - coord: {chunkCoord.x} {chunkCoord.y}";
            Debug.Log($"-terrain- Create chunk: {currentIndexLevel} {chunkCoord}  chunk: {chunkGO.name} prefab: {chunkPrefabRandom.name}");

            if (currentLevel.vegetables.Length == 0)
                PlaceAndScaleExistingVege(chunkCoord, chunkGO);
            else
                CreateAndScaleVege(chunkCoord, chunkGO);

            spawnedChunks.Add(chunkCoord, chunkGO);

            if (disableObstacles) // useful for test mode
            {
                foreach (Collider col in chunkGO.GetComponentsInChildren<Collider>())
                {
                    // keeps the GameObject visible, but disables any physical interaction.
                    // To disable collision detection, we can disable the collider component.
                    // But add tag "Obstacle" to the collision object associated to the gameobject.
                    if (col.CompareTag("Obstacle"))
                        col.enabled = false;
                }
            }

            // Generate and place bonus.
            // When a chunk is re-generated (player return), no bonus are generated.
            // ---------------------------------------------------------------------
            if (currentLevel.bonusScorePrefab.Length > 0 && currentLevel.bonusMalusDensity > 0f && !spawnedBonus.ContainsKey(chunkCoord))
            {
                AddBonusMalus(chunkCoord, chunkGO, currentLevel.bonusMalusDensity, currentLevel.bonusMalusRatio, currentLevel.bonusScorePrefab);
            }
            if (currentLevel.bonusInstrumentPrefab.Length > 0 && currentLevel.bonusInstrumentDensity > 0f &&
                gameManager.midiManager.InstrumentRestored < gameManager.midiManager.InstrumentFound)
            {
                AddInstrument(chunkCoord, chunkGO, currentLevel.bonusInstrumentDensity, currentLevel.bonusInstrumentPrefab);
            }
        }

        public void ModifyChunkMesh(GameObject chunkObject)
        {
            MeshFilter meshFilter = chunkObject.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // IMPORTANT : Créer une copie pour éviter de modifier l'asset original
                Mesh mesh = Instantiate(meshFilter.sharedMesh);

                // Modifier les vertices
                Vector3[] vertices = mesh.vertices;

                for (int i = 0; i < vertices.Length; i++)
                {
                    // Vos modifications ici
                    vertices[i].y += UnityEngine.Random.Range(-1f, 1f);
                }

                // Appliquer les changements
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                // Assigner le mesh modifié
                meshFilter.mesh = mesh;
            }
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
                    PositionOnHighestTerrain(bonus.transform, 100f);
                    bonus.name = $"Bonus - level: {currentIndexLevel} - chunk {chunkCoord} - localPosition: {bonus.transform.localPosition}";
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
                    Vector3 instrumentPos = new Vector3(UnityEngine.Random.Range(-chunkSize / 2f, chunkSize / 2f), 5f, UnityEngine.Random.Range(-chunkSize / 2f, chunkSize / 2f));
                    instrument.transform.SetLocalPositionAndRotation(instrumentPos, Quaternion.identity);
                    PositionOnHighestTerrain(instrument.transform, 100f, 4f);
                    instrument.name = $"AddInstrument - level: {currentIndexLevel} - chunk {chunkCoord} - localPosition: {instrument.transform.localPosition}";
                }

                // Just keep a trace of bonus count for this chunk to avoid re-generate if player return
                //spawnedInstrument.Add(chunkCoord, count);

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
                    offsetX = offsetX * 1.1f * chunkSize - chunkSize / 2f;
                    offsetZ = offsetZ * 1.1f * chunkSize - chunkSize / 2f;

                    //if (Mathf.Abs(offsetX) > 9f || Mathf.Abs(offsetZ) > 9f)
                    //    Debug.Log($"Chunk: {chunkCoord} Child: {childTransform.name} {childTransform.tag} offset:{offsetX} {offsetZ} ");

                    // Define position and place to the terrain
                    Vector3 localPosition = new Vector3(offsetX, 5f, offsetZ);
                    //Debug.Log($"Chunk: {chunkCoord} Vege: {childTransform.name} localPosition: {localPosition} ");
                    if (childTransform.CompareTag("TreeScalable"))
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
                        Debug.LogWarning($"No hit, chunk: {chunkCoord} child: {childTransform.name} offsetX:{offsetX} offsetZ: {offsetZ} ");
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

                    vege.name = $"Vege - {vege.name} - coord: {localPosition.x} {localPosition.z}";

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

        public static bool PositionOnHighestTerrain(Transform obj, float maxRayHeight = 100f, float yShift = 0f)
        {
            Vector3 startPos = obj.position + Vector3.up * maxRayHeight;
            Ray ray = new Ray(startPos, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray, maxRayHeight * 2f);

            if (hits.Length == 0)
            {
                Debug.LogWarning($"PositionOnHighestTerrain    --> no hit. From position: {startPos} for {obj.name}");
                return false;
            }

            var terrainHits = hits
                .Where(h => h.collider.CompareTag("Terrain"))
                .OrderByDescending(h => h.point.y)
                .ToArray();

            if (terrainHits.Length == 0)
            {
                Debug.LogWarning($"PositionOnHighestTerrain    --> no terrain hit. All hits: {hits.Length} From position: {startPos} for {obj.name}");
                return false;
            }

            RaycastHit topHit = terrainHits[0];

            // Convert contact world position to local Chunk
            Transform chunk = obj;
            if (obj.parent != null)
                chunk = obj.parent;
            Vector3 localPos = chunk.InverseTransformPoint(topHit.point);

            obj.localPosition = new Vector3(localPos.x, localPos.y + yShift, localPos.z);

            // Debug purpose ....
            //obj.name = obj.name.Substring(0, 6) + "_" + chunk.name + "_" + topHit.transform.name;

            //Debug.Log($"PositionOnHighestTerrain    --> {terrainHits.Length} hit {terrainHits[0].transform.name} world: {topHit.point} local:{obj.localPosition} --> parent: {chunk.name}");

            return true;
        }

        public static bool PositionWorldOnHighestTerrain(Transform obj, float maxRayHeight = 100f, float yShift = 0f)
        {
            Vector3 startPos = obj.position + Vector3.up * maxRayHeight;
            Ray ray = new Ray(startPos, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray, maxRayHeight * 2f);

            if (hits.Length == 0)
            {
                Debug.LogWarning($"PositionOnHighestTerrain    --> no hit. From position: {startPos} for {obj.name}");
                return false;
            }

            var terrainHits = hits
                .Where(h => h.collider.CompareTag("Terrain"))
                .OrderByDescending(h => h.point.y)
                .ToArray();

            if (terrainHits.Length == 0)
            {
                Debug.LogWarning($"PositionOnHighestTerrain    --> no terrain hit. All hits: {hits.Length} From position: {startPos} for {obj.name}");
                return false;
            }

            RaycastHit topHit = terrainHits[0];

            obj.position = new Vector3(topHit.point.x, topHit.point.y + yShift, topHit.point.z);

            // Debug purpose ....
            //obj.name = obj.name.Substring(0, 6) + "_" + chunk.name + "_" + topHit.transform.name;

            //Debug.Log($"PositionOnHighestTerrain    --> {terrainHits.Length} hit {terrainHits[0].transform.name} world: {topHit.point} local:{obj.localPosition} --> parent: {obj.name}");

            return true;
        }

        /// <summary>
        /// Clear chunks which are at a distance greater than the specified distance from the player.
        /// </summary>
        /// <param name="atDistance"></param>
        public void ClearChunks(int atDistance)
        {
            if (spawnedChunks != null)
                for (int x = -renderDistance; x <= renderDistance; x++)
                {
                    for (int z = -renderDistance; z <= renderDistance; z++)
                    {
                        Vector2Int chunkCoord = currentChunk + new Vector2Int(x, z);
                        if ((chunkCoord - currentChunk).magnitude >= atDistance)
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
    public class Vegetable
    {
        public GameObject vegetable;
        public int count;
    }
}