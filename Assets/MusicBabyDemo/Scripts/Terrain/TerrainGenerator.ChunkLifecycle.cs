using UnityEngine;

namespace MusicRun
{
    /*
     * FILE ROLE
     * - Chunk lifecycle part of TerrainGenerator (partial class).
     * - Handles chunk creation/reuse/destruction, pooling, and layer assignment.
     * - Keeps chunk housekeeping separated from core orchestration.
     */
    public partial class TerrainGenerator
    {
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
            if (enableLogAndRename)
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
}
