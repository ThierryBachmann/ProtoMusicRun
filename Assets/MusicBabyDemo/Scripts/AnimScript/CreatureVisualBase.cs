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
        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
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
