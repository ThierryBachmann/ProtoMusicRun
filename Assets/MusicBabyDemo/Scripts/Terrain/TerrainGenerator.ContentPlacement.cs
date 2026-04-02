using UnityEngine;
using Random = UnityEngine.Random;

namespace MusicRun
{
    /*
     * FILE ROLE
     * - Content placement part of TerrainGenerator (partial class).
     * - Handles bonus/instrument spawn logic, vegetation placement, and terrain raycast fitting.
     * - Keeps gameplay decoration logic separated from chunk lifecycle.
     */
    public partial class TerrainGenerator
    {
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
                    float maxPos = chunkSize / 2f;// - 0.1f; // -0.1 to avoid border
                    Vector3 bonusPos = new Vector3(Random.Range(-maxPos, maxPos), 5f, Random.Range(-maxPos, maxPos));
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
                    float maxPos = chunkSize / 2f;// - 0.1f; // -0.1 to avoid border
                    Vector3 instrumentPos = new Vector3(Random.Range(-maxPos, maxPos), 5f, Random.Range(-maxPos, maxPos));
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
            // Perlin noise test - return not strictly between 0 and 1
            //int count=0;
            //const float EPS = 1e-6f;
            //for (int i = 0; i < 100000; i++)
            //{
            //    float x = UnityEngine.Random.value * 100f;
            //    float y = UnityEngine.Random.value * 100f;

            //    float n = UnityEngine.Mathf.PerlinNoise(x, y);

            //    if (n < -EPS || n > 1f + EPS)
            //    {
            //        count++;
            //        Debug.LogError(
            //            $"[PerlinTest] " +
            //            $"count={count} " +
            //            $"x={x:F6} " +
            //            $"y={y:F6} " +
            //            $"n={n:F9}"
            //        );
            //    }
            //}


            // Build vegetable. Get list of gameobject exiting in the prefab chunk and apply random or perlin change
            foreach (Transform childTransform in chunk.transform)
            {
                if (childTransform.CompareTag("TreeScalable") || childTransform.CompareTag("Grass") || childTransform.CompareTag("Cliff"))
                {
                    Vector3 childPosition = childTransform.localPosition;

                    /*
                        Perlin noise (invented by Ken Perlin) is a type of gradient noise � meaning it�s generated by smoothly interpolating 
                        between pseudo-random gradient values at grid points.

                        It�s not random white noise (which changes abruptly each sample), but a continuous, smooth function that produces �organic� patterns.
                    */

                    // Perlin generator for vegetable -  return a value between 0 and 1 (at 0.05% near 0 or 1, so 1.06 is possible)
                    //  perlinVegetable: how much position are spread on the chunk. 0: all vegetables are at the same place on the current chunk.
                    //  perlinChunk:  how much position are modified between chunk. 0: all vegetables are at the same place for each chunk.
                    //float offsetX = Mathf.PerlinNoise(
                    //    childPosition.x * currentLevel.perlinVegetable + chunkCoord.x * currentLevel.perlinChunk,
                    //    childPosition.z * currentLevel.perlinVegetable+ chunkCoord.y * currentLevel.perlinChunk);
                    //float offsetZ = Mathf.PerlinNoise(
                    //    childPosition.z * currentLevel.perlinVegetable+ chunkCoord.x * currentLevel.perlinChunk,
                    //    childPosition.x * currentLevel.perlinVegetable+ chunkCoord.y * currentLevel.perlinChunk);

                    // For this version, we prefer pure random placement. It's better to set child position at 0,0 (center of the terrain)
                    // perlinVegetable will controls how much children are spread on the terrain.
                    // Original position + random value between 0 and perlinVegetable
                    float offsetX = childPosition.x + currentLevel.perlinVegetable * Random.value;
                    float offsetZ = childPosition.z + currentLevel.perlinVegetable * Random.value;

                    offsetX = Mathf.Clamp(offsetX, 0f, 1f);
                    offsetZ = Mathf.Clamp(offsetZ, 0f, 1f);

                    // Position between -chunkSize/2 and chunkSize/2
                    float scaledOffsetX = offsetX * chunkSize - chunkSize / 2f;
                    float scaledOffsetZ = offsetZ * chunkSize - chunkSize / 2f;

                    if (Mathf.Abs(scaledOffsetX) > 10f || Mathf.Abs(scaledOffsetZ) > 10f)
                        Debug.LogWarning($"No hit, out of the chunk: {chunkCoord} Child: {childTransform.name} {childTransform.tag} offset:{offsetX} {offsetZ} scaled:{scaledOffsetX} {scaledOffsetZ}");

                    // Define position and place to the terrain
                    Vector3 localPosition = new Vector3(scaledOffsetX, 5f, scaledOffsetZ);
                    //Debug.Log($"Chunk: {chunkCoord} Vege: {childTransform.name} localPosition: {localPosition} ");

                    float randomScale = 0;
                    if (childTransform.CompareTag("TreeScalable") || childTransform.CompareTag("Grass"))
                    {
                        //currentLevel.maxScaleVegetable = Mathf.Clamp(currentLevel.maxScaleVegetable, 0.01f, 5f);
                        //currentLevel.minScaleVegetable = Mathf.Clamp(currentLevel.minScaleVegetable, 0.01f, 5f);
                        if (currentLevel.minScaleVegetable > currentLevel.maxScaleVegetable)
                            currentLevel.minScaleVegetable = currentLevel.maxScaleVegetable;
                        // Random scale variation
                        randomScale = Random.Range(currentLevel.minScaleVegetable, currentLevel.maxScaleVegetable);
                        childTransform.localScale = childTransform.localScale * randomScale;
                    }

                    // Random Y rotation (0�360 degrees)
                    float randomRotY = Random.Range(0f, 360f);
                    childTransform.localRotation = Quaternion.Euler(childTransform.localRotation.eulerAngles.x, randomRotY, childTransform.localRotation.eulerAngles.z);

                    // Set the local position 
                    childTransform.localPosition = localPosition;

                    // Search and apply the Y 
                    if (!PositionOnHighestTerrain(childTransform, 100f))
                        Debug.LogWarning($"No hit, chunk: {chunkCoord} '{chunk.name}' child: {childTransform.name} offsetX:{scaledOffsetX} offsetZ: {scaledOffsetZ} ");

                    if (enableLogAndRename)
                        Debug.Log($"Add Vege at {chunkCoord} '{childTransform.name}' '{childTransform.tag}' offset:{childTransform.localPosition} {childTransform.position} randomScale:{randomScale:F2} rotation:{randomRotY:F2}");

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
                        Perlin noise (invented by Ken Perlin) is a type of gradient noise � meaning it�s generated by smoothly interpolating 
                        between pseudo-random gradient values at grid points.
                        It�s not random white noise (which changes abruptly each sample), but a continuous, smooth function that produces �organic� patterns.

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

                    // Random Y rotation (0�360 degrees)
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
        public bool PositionOnHighestTerrain(Transform objTransform, float maxRayHeight = 100f, float yShift = 0f)
        {
            Vector3 startPos = objTransform.position + Vector3.up * maxRayHeight;
            Ray ray = new Ray(startPos, Vector3.down);

            RaycastHit topHit;
            // **** Warning ****
            // Don't forget to set the tag 'Terrain' to the objects which holds a mesh (or other) collider.
            // Object with tag 'Terrain' will be set to the layer 'TerrainCurrent' when the chunk is created or reused.
            // If not, no hit will be detected.
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

                if (enableLogAndRename)
                    // Debug purpose ....
                    objTransform.name = $"{objTransform.name}_{chunk.name}_{localPos.y:F1}_{topHit.transform.name}";
                //Debug.Log($"terrain_hits hit:'{topHit.transform.name}' world:{topHit.point} for:'{objTransform.name}' posW:{objTransform.position} --> parent: '{chunk.name}' {chunk.gameObject.activeInHierarchy} {chunk.gameObject.activeSelf}");
            }
            return true;
        }
    }
}
