using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DuneTerrainGenerator : MonoBehaviour
{
    [Header("Vertices")]
    public int countX = 20;
    public int countZ = 20;
    public float size = 1f;

    [Header("Paramètres des dunes")]
    [Range(0.01f, 10f)]
    public float amplitude = 3.5f;
    [Range(0.01f, 0.3f)]
    public float frequency = 0.1f;
    [Range(1, 8)]
    public int octaves = 3;
    [Range(0f, 1f)]
    public float persistence = 0.5f;
    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Header("Border size")]
    [Range(0f, 0.5f)]
    public float edgeSize = 0.1f;

    [Header("Forme des dunes")]
    [Range(0f, 4f)]
    public float ridgeStrength = 1f;
    public Vector2 windDirection = new Vector2(0f, 0f);


    [Header("Calculated min/max height and vertices count")]
    public float minY;
    public float maxY;
    public int verticesCount;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private MeshCollider meshCollider;

    void Awake()
    {
        InitializeComponents();
    }
    void InitializeComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
        }
        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();

            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }

        // Ensure we also have a MeshRenderer
        if (GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }
    }
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        GenerateTerrain();
    }

    [ContextMenu("Générer Terrain")]
    public void GenerateTerrain()
    {
        DateTime startGenerate = DateTime.Now;
        InitializeComponents();

        mesh = new Mesh();
        mesh.name = "Dune Terrain";

        Vector3[] vertices = GenerateVertices();
        int[] triangles = GenerateTriangles();
        Vector2[] uvs = GenerateUVs();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
        Debug.Log($"Generate terrain {verticesCount} vertices minY:{minY:F2} maxY:{maxY:F2} delta:{(maxY-minY):F2} {(DateTime.Now - startGenerate).TotalMilliseconds:F3} ms");
    }

    Vector3[] GenerateVertices()
    {
        Vector3[] vertices = new Vector3[(countX + 1) * (countZ + 1)];

        windDirection = windDirection.normalized;

        // Center the mesh in XZ around (0,0) in XZ; we'll lift it to start at y=0 later.
        float centerX = (size * countX) / 2f;
        float centerZ = (size * countZ) / 2f;

        minY = float.MaxValue;
        maxY = float.MinValue;
        verticesCount = 0;

        for (int z = 0; z <= countZ; z++)
        {
            for (int x = 0; x <= countX; x++)
            {
                float y = CalculateHeight(x, z);
                vertices[verticesCount] = new Vector3(centerX - x * size, y, centerZ - z * size);

                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                verticesCount++;
            }
        }

        // Normalize so the terrain starts exactly at y = 0.
        if (minY != 0f)
        {
            for (int k = 0; k < vertices.Length; k++)
            {
                Vector3 v = vertices[k];
                v.y -= minY;
                vertices[k] = v;
            }
        }

        return vertices;
    }

    float CalculateHeight(int x, int z)
    {
        // Normalize grid coordinates to [0,1]
        float xCoord = (float)x / countX;
        float zCoord = (float)z / countZ;

        // --- Base multi-octave Perlin noise (zero-mean) ---
        // Convert Perlin from [0,1] to [-1,1] so expected value is ~0.
        float height = 0f;
        float currentAmplitude = amplitude;
        float currentFrequency = frequency;

        /*
         fBM (fractal Brownian motion): a sum of Perlin octaves.
            amplitude is the starting vertical scale of that fBM stack. Practically: raising amplitude increases the dune relief 
            (peaks higher, valleys lower—if zero-mean) or pushes the whole surface upward.
            Each octave’s amplitude is multiplied by persistence, so the total possible relief from the fBM part is roughly:
            amplitude * (1 - persistence^octaves) / (1 - persistence) (geometric series).
            Perlin calls return values in [0, 1], not centered on zero. That makes the base height biased upward.
         */
        for (int i = 0; i < octaves; i++)
        {
            float p = Mathf.PerlinNoise(xCoord * currentFrequency * 10f,
                                        zCoord * currentFrequency * 10f);
            float centered = p * 2f - 1f; // [-1, 1]
            height += centered * currentAmplitude;

            currentAmplitude *= persistence;
            currentFrequency *= lacunarity;
        }

        // --- Ridge term (creates dune crests) ---
        // The mean of that squared triangular term is ≈ 1/3, so the ridge adds about ridgeStrength/3 of upward bias on average.*/
        // ridgeNoise in [0,1], peaked near 0.5; its squared mean is ~1/3.
        float ridgeNoise = Mathf.PerlinNoise(xCoord * 5f + windDirection.x,
                                             zCoord * 5f + windDirection.y);
        ridgeNoise = 1f - Mathf.Abs(ridgeNoise - 0.5f) * 2f; // triangular peak
        ridgeNoise = Mathf.Pow(ridgeNoise, 2f);               // sharper crests

        // De-bias ridge by subtracting its mean (~1/3) so it doesn’t push up overall
        const float RidgeMean = 1f / 3f;
        height += ridgeStrength * (ridgeNoise - RidgeMean);

        // --- Wind asymmetry ---
        // Adds a slight tilt; expected average is ~0 across a symmetric grid.
        float windEffect = Vector2.Dot(new Vector2(xCoord - 0.5f, zCoord - 0.5f), windDirection.normalized);
        height += windEffect * 0.3f;

        // --- Edge softening ---
        // Fade heights near borders to avoid cliffs.
        if (edgeSize > 0f)
        {
            // Minimal distance to a border (0 at the border, ~0.5 at the center)
            float distanceFromEdge = Mathf.Min(xCoord, 1f - xCoord, zCoord, 1f - zCoord);

            // edgeSize is the border band (0..0.5). Inside this band we fade to 0, outside we keep 1.
            // 0 at the very edge, 1 at the inner border limit (borderWidth)
            float t = Mathf.InverseLerp(0f, edgeSize, distanceFromEdge);

            // Smooth transition from 0 (edge) to 1 (interior)
            float edgeFactor = Mathf.SmoothStep(0f, 1f, t);

            height *= edgeFactor;
        }
        return height;
    }

    int[] GenerateTriangles()
    {
        int[] triangles = new int[countX * countZ * 6];

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < countZ; z++)
        {
            for (int x = 0; x < countX; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + countX + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + countX + 1;
                triangles[tris + 5] = vert + countX + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        Vector2[] uvs = new Vector2[(countX + 1) * (countZ + 1)];

        for (int i = 0, z = 0; z <= countZ; z++)
        {
            for (int x = 0; x <= countX; x++)
            {
                uvs[i] = new Vector2((float)x / countX, (float)z / countZ);
                i++;
            }
        }

        return uvs;
    }

    void OnValidate()
    {
        if (/*Application.isPlaying && */meshFilter != null)
        {
            GenerateTerrain();
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
}