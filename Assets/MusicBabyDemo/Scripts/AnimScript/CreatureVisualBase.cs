using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CreatureVisualState
{
    Idle = 0,
    Follow = 1,
    Overtake = 2,
    Hunt = 3,
    Recenter = 4,
    WaitPlayer = 5,
    EatAttack = 6,
    EatRecovery = 7,
    LeashReturn = 8,
    Stunned = 9,
}

public enum CreatureSphereDetailLevel
{
    Blocky = -1,
    VeryLow = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4,
}

public abstract class CreatureVisualBase : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Current visual state used by animation. Runtime controller updates it in Play mode; in Edit mode, you can set it manually.")]
    public CreatureVisualState state = CreatureVisualState.Idle;
    [Tooltip("Play animation continuously in Edit Mode without entering Play mode (uses current State).")]
    public bool previewInEditMode = false;

    [HideInInspector] [SerializeField] protected float runtimeSpeed;
    [HideInInspector] [SerializeField] protected bool runtimeGrounded;
    [HideInInspector] [SerializeField] protected bool runtimeHasTarget;
    [HideInInspector] [SerializeField] protected Vector3 runtimeGroundNormal = Vector3.up;

    protected CreatureVisualState previousState;
    protected float stateTimer;
    protected bool materialsDirty = true;
    protected float editPreviewDeltaTime = 0.016f;
#if UNITY_EDITOR
    private double editorLastUpdateTime;
#endif

    protected virtual void Start()
    {
        materialsDirty = true;
        BuildIfNeeded(false);
        SyncPreviousState();
    }

    protected virtual void OnEnable()
    {
        materialsDirty = true;
        BuildIfNeeded(false);
        SyncPreviousState();
#if UNITY_EDITOR
        editorLastUpdateTime = EditorApplication.timeSinceStartup;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
#endif
    }

    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= OnEditorUpdate;
#endif
    }

    protected virtual void OnValidate()
    {
        materialsDirty = true;
        BuildIfNeeded(false);
    }

    protected virtual void Update()
    {
        if (!Application.isPlaying && IsEditModePreviewActive())
            return;

        BuildIfNeeded(false);
        HandleAnimationStateChanges();
        if (Application.isPlaying)
            stateTimer += Time.deltaTime;

        UpdateAnimation();
    }

    public virtual void SetState(CreatureVisualState newState)
    {
        if (state == newState)
            return;

        CreatureVisualState old = state;
        state = newState;

        if (Application.isPlaying)
        {
            OnStateChanged(old, newState);
            previousState = newState;
        }
        else
        {
            previousState = GetAnimationState();
        }
    }

    public virtual void SetControllerState(MusicRun.CreatureState controllerState)
    {
        SetState(MapControllerState(controllerState));
    }

    public virtual void SetMotionContext(float speed, bool grounded, Vector3 groundNormal)
    {
        runtimeSpeed = Mathf.Max(0f, speed);
        runtimeGrounded = grounded;
        runtimeGroundNormal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
    }

    public virtual void SetTargetContext(bool hasTarget)
    {
        runtimeHasTarget = hasTarget;
    }

    public virtual void RebuildVisual()
    {
        BuildIfNeeded(true);
        SyncPreviousState();
    }

    protected virtual CreatureVisualState MapControllerState(MusicRun.CreatureState controllerState)
    {
        switch (controllerState)
        {
            case MusicRun.CreatureState.FOLLOW:
                return CreatureVisualState.Follow;
            case MusicRun.CreatureState.OVERTAKE:
                return CreatureVisualState.Overtake;
            case MusicRun.CreatureState.HUNT:
                return CreatureVisualState.Hunt;
            case MusicRun.CreatureState.RECENTER:
                return CreatureVisualState.Recenter;
            case MusicRun.CreatureState.WAIT_PLAYER:
                return CreatureVisualState.WaitPlayer;
            case MusicRun.CreatureState.EAT_ATTACK:
                return CreatureVisualState.EatAttack;
            case MusicRun.CreatureState.EAT_RECOVERY:
                return CreatureVisualState.EatRecovery;
            case MusicRun.CreatureState.LEASH_RETURN:
                return CreatureVisualState.LeashReturn;
            default:
                return CreatureVisualState.Idle;
        }
    }

    protected void SyncPreviousState()
    {
        previousState = GetAnimationState();
        stateTimer = 0f;
    }

    protected virtual void OnStateChanged(CreatureVisualState oldState, CreatureVisualState newState)
    {
        stateTimer = 0f;
    }

    protected float DeltaTimeSafe()
    {
        if (Application.isPlaying)
            return Time.deltaTime;

        if (IsEditModePreviewActive())
            return editPreviewDeltaTime;

        return 0.016f;
    }

    protected CreatureVisualState GetAnimationState()
    {
        return state;
    }

    protected bool IsEditModePreviewActive()
    {
#if UNITY_EDITOR
        return !Application.isPlaying && previewInEditMode;
#else
        return false;
#endif
    }

    protected void HandleAnimationStateChanges()
    {
        CreatureVisualState currentAnimationState = GetAnimationState();
        if (previousState == currentAnimationState)
            return;

        OnStateChanged(previousState, currentAnimationState);
        previousState = currentAnimationState;
    }

#if UNITY_EDITOR
    private void OnEditorUpdate()
    {
        if (Application.isPlaying)
            return;
        if (!isActiveAndEnabled)
            return;

        if (!IsEditModePreviewActive())
        {
            editorLastUpdateTime = EditorApplication.timeSinceStartup;
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - editorLastUpdateTime);
        editorLastUpdateTime = now;

        if (dt <= 0f || dt > 0.25f)
            dt = 0.016f;

        editPreviewDeltaTime = dt;

        BuildIfNeeded(false);
        HandleAnimationStateChanges();
        stateTimer += editPreviewDeltaTime;
        UpdateAnimation();

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
#endif

    protected abstract void BuildIfNeeded(bool force);
    protected abstract void UpdateAnimation();
}

/// <summary>
/// Intermediate base for procedural creatures generated from code.
/// Step 1 introduces this layer without behavioral changes.
/// Next steps can move shared construction/material helpers here.
/// </summary>
public abstract class ProceduralCreatureVisualBase : CreatureVisualBase
{
    [Header("STRUCTURE / Geometry")]
    [Tooltip("Global mesh detail for generated sphere parts. Blocky uses cubes, Very Low gives a low-poly sphere look.")]
    public CreatureSphereDetailLevel sphereDetailLevel = CreatureSphereDetailLevel.High;

    // Shared material semantics for procedural creatures.
    // Keeping this in the intermediate base will make future species (ostrich, elephant, etc.)
    // reuse the same slot vocabulary.
    protected enum CreatureMaterialSlot
    {
        Body,
        Tail,
        Ears,
        EyeSclera,
        EyePupil,
        Mouth,
    }

    private static readonly Dictionary<CreatureSphereDetailLevel, Mesh> SphereMeshCache = new Dictionary<CreatureSphereDetailLevel, Mesh>();
    private static Mesh blockyCubeMeshCache;

    // Species-specific material mapping (for example: body/sclera/mouth assignments).
    protected abstract Material ResolveMaterialForSlot(CreatureMaterialSlot slot);

    // Allows derived classes to customize fallback warning prefixes.
    protected virtual string FallbackMaterialContextName => GetType().Name;

    // Creates an empty transform node in local space (used as hierarchy pivot/group).
    protected Transform CreateNode(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    // Creates a primitive mesh part, parents it, sets local TRS, then assigns material.
    protected Transform CreatePart(
        string name,
        PrimitiveType primitiveType,
        Transform parent,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale,
        CreatureMaterialSlot slot)
    {
        GameObject go;
        if (primitiveType == PrimitiveType.Sphere)
        {
            go = new GameObject(name);
            MeshFilter meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetOrCreateSphereMesh(sphereDetailLevel);
            go.AddComponent<MeshRenderer>();
        }
        else
        {
            go = GameObject.CreatePrimitive(primitiveType);
            go.name = name;
        }

        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(localEulerAngles);
        go.transform.localScale = localScale;

        // Primitives come with a default collider; procedural visuals should stay collider-free
        // unless an explicit gameplay collider is added separately (for example trigger on root).
        RemoveCollider(go.GetComponent<Collider>());

        ApplyMaterial(go, slot);
        return go.transform;
    }

    private static Mesh GetOrCreateSphereMesh(CreatureSphereDetailLevel detailLevel)
    {
        if (detailLevel == CreatureSphereDetailLevel.Blocky)
            return GetOrCreateBlockyCubeMesh();

        if (SphereMeshCache.TryGetValue(detailLevel, out Mesh cached) && cached != null)
            return cached;

        GetSphereSegments(detailLevel, out int longitudeSegments, out int latitudeSegments);
        Mesh generated = BuildUvSphereMesh(longitudeSegments, latitudeSegments);
        generated.name = "CreatureSphere_" + detailLevel;
        generated.hideFlags = HideFlags.HideAndDontSave;
        SphereMeshCache[detailLevel] = generated;
        return generated;
    }

    private static Mesh GetOrCreateBlockyCubeMesh()
    {
        if (blockyCubeMeshCache != null)
            return blockyCubeMeshCache;

        // Centered cube with size 1 so procedural scale logic remains unchanged.
        Vector3[] vertices =
        {
            // Front
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            // Back
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            // Left
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            // Right
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            // Top
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            // Bottom
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
        };

        Vector3[] normals =
        {
            // Front
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            // Back
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            // Left
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            // Right
            Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            // Top
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            // Bottom
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
        };

        Vector2[] uv =
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
        };

        int[] triangles =
        {
            0, 1, 2, 0, 2, 3,       // Front
            4, 5, 6, 4, 6, 7,       // Back
            8, 9, 10, 8, 10, 11,    // Left
            12, 13, 14, 12, 14, 15, // Right
            16, 17, 18, 16, 18, 19, // Top
            20, 21, 22, 20, 22, 23, // Bottom
        };

        Mesh mesh = new Mesh();
        mesh.name = "CreatureCube_Blocky";
        mesh.hideFlags = HideFlags.HideAndDontSave;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        blockyCubeMeshCache = mesh;
        return blockyCubeMeshCache;
    }

    private static void GetSphereSegments(CreatureSphereDetailLevel detailLevel, out int longitudeSegments, out int latitudeSegments)
    {
        switch (detailLevel)
        {
            case CreatureSphereDetailLevel.VeryLow:
                longitudeSegments = 8;
                latitudeSegments = 4;
                break;
            case CreatureSphereDetailLevel.Low:
                longitudeSegments = 12;
                latitudeSegments = 6;
                break;
            case CreatureSphereDetailLevel.Medium:
                longitudeSegments = 20;
                latitudeSegments = 10;
                break;
            case CreatureSphereDetailLevel.High:
                longitudeSegments = 28;
                latitudeSegments = 14;
                break;
            default:
                longitudeSegments = 36;
                latitudeSegments = 18;
                break;
        }
    }

    private static Mesh BuildUvSphereMesh(int longitudeSegments, int latitudeSegments)
    {
        int vertexCount = (latitudeSegments + 1) * (longitudeSegments + 1);
        List<Vector3> vertices = new List<Vector3>(vertexCount);
        List<Vector3> normals = new List<Vector3>(vertexCount);
        List<Vector2> uv = new List<Vector2>(vertexCount);
        List<int> triangles = new List<int>(longitudeSegments * (latitudeSegments - 1) * 6);

        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float v = lat / (float)latitudeSegments;
            float theta = Mathf.PI * v;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float u = lon / (float)longitudeSegments;
                float phi = u * Mathf.PI * 2f;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                Vector3 normal = new Vector3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
                vertices.Add(normal * 0.5f);
                normals.Add(normal);
                uv.Add(new Vector2(u, 1f - v));
            }
        }

        int stride = longitudeSegments + 1;
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int current = lat * stride + lon;
                int next = current + stride;

                if (lat > 0)
                {
                    // Unity expects clockwise winding for front faces.
                    triangles.Add(current);
                    triangles.Add(current + 1);
                    triangles.Add(next);
                }

                if (lat < latitudeSegments - 1)
                {
                    triangles.Add(current + 1);
                    triangles.Add(next + 1);
                    triangles.Add(next);
                }
            }
        }

        Mesh mesh = new Mesh();
        if (vertexCount > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    protected void ApplyMaterial(GameObject go, CreatureMaterialSlot slot)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material assigned = ResolveMaterialForSlot(slot);
        if (assigned != null)
            renderer.sharedMaterial = assigned;
    }

    protected Material CreateFallbackMaterial(string matName, Color color)
    {
        // Force URP-compatible fallbacks to avoid pink/magenta materials in URP projects.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (shader == null)
        {
            Debug.LogWarning(
                $"[{FallbackMaterialContextName}] URP fallback shaders not found. " +
                "Falling back to a generic unlit shader.");
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Hidden/InternalErrorShader");

        Material mat = new Material(shader);
        mat.name = matName;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        return mat;
    }

    protected void RemoveCollider(GameObject go)
    {
        if (go == null)
            return;

        RemoveCollider(go.GetComponent<Collider>());
    }

    protected void RemoveCollider(Collider collider)
    {
        if (collider == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(collider);
        else
            Destroy(collider);
#else
        Destroy(collider);
#endif
    }

    protected void RemoveCollidersInHierarchy(Transform hierarchyRoot, bool includeRoot = true)
    {
        if (hierarchyRoot == null)
            return;

        Collider[] colliders = hierarchyRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
                continue;
            if (!includeRoot && collider.transform == hierarchyRoot)
                continue;
            if (collider is CharacterController)
                continue;

            RemoveCollider(collider);
        }
    }
}
