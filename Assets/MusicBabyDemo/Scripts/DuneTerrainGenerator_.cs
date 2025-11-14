using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class xDuneTerrainGenerator : MonoBehaviour
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

    [Header("Border percentage")]
    [Range(0f, 1f)]
    public float edgeSize = 0.1f;

    [Header("Forme des dunes")]
    [Range(0f, 30f)]
    public float ridgeStrength = 4f;
    public Vector2 windDirection = new Vector2(0f, 0f);

    [Header("Calculated min/max height and vertices count")]
    public float minY, maxY;
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
            meshCollider= GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }
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
        Debug.Log($"Generate terrain {verticesCount} vertices minY:{minY:F2} maxY:{maxY:F2} {(DateTime.Now-startGenerate).TotalMilliseconds:F2} ms");
    }

    Vector3[] GenerateVertices()
    {
        Vector3[] vertices = new Vector3[(countX + 1) * (countZ + 1)];

        windDirection = windDirection.normalized;

        // Center the mesh
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

        return vertices;
    }

    float CalculateHeight(int x, int z)
    {
        // Normalize coordinate
        float xCoord = (float)x / countX;
        float zCoord = (float)z / countZ;

        // Bruit de Perlin multi-octaves pour la base
        float height = 0f;
        float currentAmplitude = amplitude;
        float currentFrequency = frequency;

        for (int i = 0; i < octaves; i++)
        {
            height += Mathf.PerlinNoise(xCoord * currentFrequency * 10f,
                                       zCoord * currentFrequency * 10f) * currentAmplitude;
            currentAmplitude *= persistence;
            currentFrequency *= lacunarity;
        }

        // Create dunes
        float ridgeNoise = Mathf.PerlinNoise(xCoord * 5f + windDirection.x,
                                           zCoord * 5f + windDirection.y);

        // Transformer le bruit en crêtes (valeurs proches de 0.5 deviennent des crêtes)
        ridgeNoise = 1f - Mathf.Abs(ridgeNoise - 0.5f) * 2f;
        ridgeNoise = Mathf.Pow(ridgeNoise, 2f);

        // Combiner le bruit de base avec les crêtes
        height += ridgeNoise * ridgeStrength;

        // Ajouter une asymétrie pour simuler l'effet du vent
        float windEffect = Vector2.Dot(new Vector2(xCoord - 0.5f, zCoord - 0.5f), windDirection);
        height += windEffect * 0.3f;

        // Adoucir les bords pour éviter les falaises
        if (edgeSize > 0f)
        {
            // Distance minimale aux bords (0 = bord, 0.5 = centre)
            float distanceFromEdge = Mathf.Min(xCoord, 1f - xCoord, zCoord, 1f - zCoord);

            // Transition douce vers 0 près des bords
            float edgeFactor = Mathf.SmoothStep(0f, edgeSize, distanceFromEdge);
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