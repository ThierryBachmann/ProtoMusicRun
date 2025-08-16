using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MusicRun
{
    public class TerrainGenerator : MonoBehaviour
    {
        public int chunkSize = 20;
        public int renderDistance = 2;
        public bool disableObstacles = false;

        [Header("For readonly")]
        public Vector2Int currentPlayerChunk;

        [Header("Defined Levels")]
        public Level[] levels;

        private Level currentLevel;
        private int currentIndexLevel;
        private GameObject currentStart;
        private Vector2Int startChunkCoord;
        private GameObject currentGoal;
        private Vector2Int goalChunkCoord;
        private Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
        private GameManager gameManager;
        private PlayerController player;

        public GameObject StartGO { get => currentStart; }
        public Level CurrentLevel { get => currentLevel; }
        public Vector2Int CurrentPlayerChunk { get => currentPlayerChunk; }

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
            bool oneLevelEnabledAtLeast = false;
            foreach (Level level in levels) if (level.enabled) { oneLevelEnabledAtLeast = true; break; }

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
                    Debug.LogWarning("Back to first level");
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

            // Better to clear chunks before creating new ones (in a prvious Unity frame) 
            // To be sure to remove all previous chunks when the level is changed (collider seems remained).
            //ClearChunks(1);

            // Create the start chunk
            // ----------------------
            if (currentLevel.startGO == null)
            {
                Debug.LogError("Start game object is not set in the level configuration.");
                return;
            }

            // Move the start chunk to the current player position (which is the current goal)
            currentStart = currentLevel.startGO;
            startChunkCoord = CurrentPlayerChunk;
            currentStart.name = $"start_{currentIndexLevel}_{startChunkCoord.x}_{startChunkCoord.y}";

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
            goalChunkCoord = CurrentPlayerChunk + currentLevel.deltaCurrentChunk;
            currentGoal.name = $"goal_{currentIndexLevel}_{goalChunkCoord.x}_{goalChunkCoord.y}";
            if (spawnedChunks.ContainsKey(goalChunkCoord))
            {
                Debug.Log($"Destroy existing goal chunk: {goalChunkCoord} --> playerChunk: {spawnedChunks[goalChunkCoord].name}");
                Destroy(spawnedChunks[goalChunkCoord]);
                spawnedChunks.Remove(goalChunkCoord);
            }
            currentGoal.transform.position = ChunkToPosition(goalChunkCoord);
            currentGoal.transform.rotation = Quaternion.identity;
            Debug.Log($"Add goal chunk coord: {goalChunkCoord} --> GO: {currentGoal.name} {currentGoal.transform.position}");
            // currentPlayerChunk=new Vector2Int(9999,9999);
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

            if (playerChunk != CurrentPlayerChunk)
            {
                //Debug.Log($"Player enters in a chunk: x={player.transform.position.x} z={player.transform.position.z} --> playerChunk: {playerChunk}");

                currentPlayerChunk = playerChunk;
                UpdateChunks();
            }
        }

        public void UpdateChunks()
        {
            // will contains chunks coord around the player at a distance of -renderDistance to renderDistance
            HashSet<Vector2Int> newChunks = new HashSet<Vector2Int>();

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector2Int chunkCoord = CurrentPlayerChunk + new Vector2Int(x, z);
                    newChunks.Add(chunkCoord);

                    // Does the chunk dictionary already contains this chunk? Don't instantiate for start and goal chunks.
                    if (!spawnedChunks.ContainsKey(chunkCoord) && chunkCoord != goalChunkCoord && chunkCoord != startChunkCoord)
                    {
                        // No, add it
                        Vector3 spawnPos = ChunkToPosition(chunkCoord);

                        // Instantiate a random prefab from the current level's runChunks
                        GameObject chunkPrefabRandom = currentLevel.runChunks[UnityEngine.Random.Range(0, currentLevel.runChunks.Length)];
                        GameObject chunk = Instantiate(chunkPrefabRandom, spawnPos, Quaternion.identity);
                        chunk.name = $"Chunk - Level: {currentIndexLevel} - coord: {chunkCoord.x} {chunkCoord.y}";
                        //Debug.Log($"Create chunk: {currentIndexLevel} {chunkCoord}  chunk: {chunk.name} prefab: {chunkPrefabRandom.name}");

                        foreach (Transform childTransform in chunk.transform)
                        {
                            if (childTransform.name.StartsWith("DatePalm") || childTransform.name.StartsWith("Sago") || childTransform.name.StartsWith("Grass") || childTransform.name.StartsWith("Fountain"))
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

                                //Debug.Log($"Chunk: {chunkCoord} Child: {childTransform.name} offset:{offsetX} {offsetZ} ");

                                // Position between -chunkSize and chunkSize
                                offsetX = offsetX * chunkSize - chunkSize / 2f;
                                offsetZ = offsetZ * chunkSize - chunkSize / 2f;

                                // Define position and place to the terrain
                                Vector3 newPosition = new Vector3(offsetX, 5f, offsetZ);
                                //Debug.Log($"Chunk: {chunkCoord} Child: {childTransform.name} local:{basePosition} --> new: {newPosition} ");
                                //childTransform.SetLocalPositionAndRotation(newPosition, Quaternion.identity);

                                // Random Y rotation (0–360 degrees)
                                float randomY = UnityEngine.Random.Range(0f, 360f);

                                currentLevel.maxScaleVegetable = Mathf.Clamp(currentLevel.maxScaleVegetable, 0.1f, 15f);
                                currentLevel.minScaleVegetable = Mathf.Clamp(currentLevel.minScaleVegetable, 0.1f, 15f);
                                if (currentLevel.minScaleVegetable >= currentLevel.maxScaleVegetable)
                                    currentLevel.minScaleVegetable = currentLevel.maxScaleVegetable - 0.1f;
                                // Random scale variation (e.g. between 0.8x and 1.2x original size)
                                float randomScale = UnityEngine.Random.Range(currentLevel.minScaleVegetable, currentLevel.maxScaleVegetable);

                                // Apply to transform
                                childTransform.localRotation = Quaternion.Euler(0f, randomY, 0f);
                                childTransform.localScale = childTransform.localScale * randomScale;

                                // Keep the local position already set
                                childTransform.localPosition = newPosition;

                                if (!PlaceOnHighestTerrain(childTransform, 100f))
                                    Debug.Log($"No hit, chunk: {chunkCoord} child: {childTransform.name} offsetX:{offsetX} offsetZ: {offsetZ} ");
                            }
                        }

                        if (disableObstacles) // useful for test mode
                        {
                            foreach (Collider col in chunk.GetComponentsInChildren<Collider>())
                            {
                                // keeps the GameObject visible, but disables any physical interaction.
                                // To disable collision detection, we can disable the collider component.
                                // But add tag "Obstacle" to the collision object associated to the gameobject.
                                if (col.CompareTag("Obstacle"))
                                    col.enabled = false;
                            }
                        }
                        spawnedChunks.Add(chunkCoord, chunk);

                        if (currentLevel.runBonus.Length > 0)
                        {
                            for (int i = 0; i < currentLevel.BonusCount; i++)
                            {
                                GameObject bonusPrefabRandom = currentLevel.runBonus[UnityEngine.Random.Range(0, currentLevel.runBonus.Length)];
                                GameObject bonus = Instantiate(bonusPrefabRandom);
                                bonus.transform.SetParent(chunk.transform, false);
                                float maxPos = chunkSize / 2f - 0.1f; // -0.1 to avoid border
                                Vector3 bonusPos = new Vector3(UnityEngine.Random.Range(-maxPos, maxPos), 5f, UnityEngine.Random.Range(-maxPos, maxPos));
                                bonus.transform.SetLocalPositionAndRotation(bonusPos, Quaternion.identity);
                                PlaceOnHighestTerrain(bonus.transform, 100f);
                                bonus.name = $"Bonus - level: {currentIndexLevel} - coord: {bonus.transform.localPosition}";
                            }
                        }
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
        /// Place un objet sur le terrain le plus haut sous sa position actuelle.
        /// </summary>
        /// <param name="obj">Objet à placer</param>
        /// <param name="maxRayHeight">Hauteur max du rayon pour détecter le sol</param>
        public static bool PlaceOnHighestTerrain(Transform obj, float maxRayHeight = 100f)
        {
            Vector3 startPos = obj.position + Vector3.up * maxRayHeight;
            Ray ray = new Ray(startPos, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray, maxRayHeight * 2f);

            if (hits.Length == 0)
            {
                //Debug.Log($"    --> no hit. From position: {startPos}");
                return false;
            }

            var terrainHits = hits
                .Where(h => h.collider.CompareTag("Terrain"))
                .OrderByDescending(h => h.point.y)
                .ToArray();

            if (terrainHits.Length == 0)
            {
                //Debug.Log($"    --> no terrain hit. All hits: {hits.Length} From position: {startPos}");
                return false;
            }

            RaycastHit topHit = terrainHits[0];

            // Convert contact wold position to local Chunk
            Transform chunk = obj;
            if (obj.parent != null)
                chunk = obj.parent;
            Vector3 localPos = chunk.InverseTransformPoint(topHit.point);

            obj.localPosition = new Vector3(localPos.x, localPos.y, localPos.z);

            // Debug purpose ....
            obj.name = obj.name.Substring(0, 6) + "_" + chunk.name + "_" + topHit.transform.name;

            //Debug.Log($"    --> {terrainHits.Length} hit {terrainHits[0].transform.name} world: {topHit.point} local:{obj.localPosition} --> parent: {chunk.name}");

            return true;
        }
        /// <summary>
        /// Clear chunks which are at a distance greater than the specified distance from the player.
        /// </summary>
        /// <param name="atDistance"></param>
        public void ClearChunks(int atDistance)
        {
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector2Int chunkCoord = CurrentPlayerChunk + new Vector2Int(x, z);
                    if ((chunkCoord - CurrentPlayerChunk).magnitude >= atDistance)
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
        public bool enabled;
        public string name;
        public string description;
        [Header("Defined MIDI associated to the level")]
        public int indexMIDI;

        [Range(0.1f, 5f)]
        public float RatioSpeedMusic = 0.3f;
        [Range(0.1f, 5f)]
        public float MinSpeedMusic = 0.1f;
        [Range(0.1f, 5f)]
        public float MaxSpeedMusic = 5f;

        [Range(0, 10)]
        public int BonusCount = 1;

        [Header("Delta chunk position with last goal")]
        public Vector2Int deltaCurrentChunk;

        [Header("Defined start and goal game object")]
        public GameObject startGO;
        public GameObject goalGO;

        [Header("How much vegetable must be spread on chunk")]
        [Range(0f, 10f)]
        public float perlinVegetable = 0.3f;

        [Header("How much vegetable must be spread by chunk")]
        [Range(0f, 10f)]
        public float perlinChunk = 100f;

        [Header("Min Max fpr random vegetable scale")]
        [Range(0.1f, 15f)]
        public float minScaleVegetable = 0.5f;

        [Range(0.1f, 15f)]
        public float maxScaleVegetable = 0.5f;

        [Header("Defined levels")]
        public GameObject[] runChunks;

        [Header("Defined bonus")]
        public GameObject[] runBonus;
    }
}