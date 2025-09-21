using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DuneTerrainGenerator : MonoBehaviour
{
    [Header("Dimensions du terrain")]
    public int sizeX = 20;
    public int sizeZ = 20;
    public float scale = 1f;

    [Header("Paramètres des dunes")]
    [Range(1f, 30f)]
    public float amplitude = 8f;
    [Range(0.01f, 0.3f)]
    public float frequency = 0.1f;
    [Range(1, 8)]
    public int octaves = 3;
    [Range(0f, 1f)]
    public float persistence = 0.5f;
    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Header("Forme des dunes")]
    [Range(0f, 30f)]
    public float ridgeStrength = 4f;
    public Vector2 windDirection = new Vector2(0f, 0f);


    private MeshFilter meshFilter;
    private Mesh mesh;

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

        // S'assurer qu'on a aussi un MeshRenderer
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
    }

    Vector3[] GenerateVertices()
    {
        Vector3[] vertices = new Vector3[(sizeX + 1) * (sizeZ + 1)];

        // Normaliser la direction du vent
        windDirection = windDirection.normalized;

        for (int i = 0, z = 0; z <= sizeZ; z++)
        {
            for (int x = 0; x <= sizeX; x++)
            {
                float y = CalculateHeight(x, z);
                vertices[i] = new Vector3(x * scale, y, z * scale);
                i++;
            }
        }

        return vertices;
    }

    float CalculateHeight(int x, int z)
    {
        // Convertir en coordonnées normalisées
        float xCoord = (float)x / sizeX;
        float zCoord = (float)z / sizeZ;

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

        // Créer des crêtes caractéristiques des dunes
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
        float edgeFactor = CalculateEdgeFactor(xCoord, zCoord);
        height *= edgeFactor;

        return height;
    }

    float CalculateEdgeFactor(float x, float z)
    {
        // Distance minimale aux bords (0 = bord, 0.5 = centre)
        float distanceFromEdge = Mathf.Min(x, 1f - x, z, 1f - z);

        // Transition douce vers 0 près des bords
        float edgeSize = 0.1f; // 10% de la taille pour la transition
        return Mathf.SmoothStep(0f, edgeSize, distanceFromEdge);
    }

    int[] GenerateTriangles()
    {
        int[] triangles = new int[sizeX * sizeZ * 6];

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + sizeX + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + sizeX + 1;
                triangles[tris + 5] = vert + sizeX + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        Vector2[] uvs = new Vector2[(sizeX + 1) * (sizeZ + 1)];

        for (int i = 0, z = 0; z <= sizeZ; z++)
        {
            for (int x = 0; x <= sizeX; x++)
            {
                uvs[i] = new Vector2((float)x / sizeX, (float)z / sizeZ);
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
}