using UnityEngine;

[ExecuteAlways]
public class HippoVisual : MonoBehaviour
{
    public enum HippoAnimState
    {
        Idle,
        Chase,
        Eat,
        Stunned
    }

    [Header("External State")]
    public HippoAnimState state = HippoAnimState.Idle;

    [Header("Build")]
    [Min(0.01f)] public float overallScale = 1f;

    [Header("Materials")]
    public Material bodyMaterial;
    public Material eyeMaterial;
    public Material mouthMaterial;

    public Color fallbackBodyColor = new Color(0.56f, 0.58f, 0.63f);
    public Color fallbackEyeColor = new Color(0.08f, 0.08f, 0.08f);
    public Color fallbackMouthColor = new Color(0.55f, 0.22f, 0.24f);

    [Header("Idle")]
    public float idleBreathSpeed = 1.6f;
    public float idleBreathAmount = 0.02f;
    public float idleHeadNodAngle = 1.5f;
    public float idleHeadNodSpeed = 1.3f;
    public float idleTailSwingFactor = 0.25f;

    [Header("Chase")]
    public bool animateWalk = true;
    public float walkCycleSpeed = 4.2f;
    public float legAngle = 16f;
    public float bodyBobAmount = 0.05f;
    public float headSwingAngle = 3f;
    public float tailSwingAngle = 14f;

    [Header("Eyes")]
    public float eyeLookSideAngle = 18f;
    public float eyeLookSpeed = 2.2f;
    [Tooltip("Horizontal offset of eye pivots from head center.")]
    public float eyePivotSideOffset = 0.44f;
    [Tooltip("Vertical offset of eye pivots from head center.")]
    public float eyePivotHeightOffset = 0.30f;
    [Tooltip("Forward offset of eye pivots from head center. Increase to pull eyes out of the head.")]
    public float eyePivotForwardOffset = 0.82f;
    [Tooltip("Uniform eye sphere scale.")]
    public float eyeScale = 0.22f;
    [Tooltip("Forward offset of each eyeball from its pivot (socket center) to make left/right look movement visible.")]
    public float eyeBallForwardOffset = 0.11f;

    [Header("Eat")]
    public float eatAnimDuration = 0.75f;
    public float eatJumpHeight = 0.45f;
    public float eatForwardStretch = 0.18f;
    public float eatLegFoldAngle = 50f;
    public float mouthOpenAngle = 30f;

    [Header("Eat Body Squash & Stretch")]
    public float eatStretchY = 1.12f;
    public float eatSquashXZ = 0.92f;
    public float landSquashY = 0.90f;
    public float landStretchXZ = 1.06f;

    [Header("Stunned")]
    public float stunnedShakeAngle = 10f;
    public float stunnedShakeSpeed = 20f;

    [Header("Debug")]
    public bool drawDebug = false;

    private const string RootName = "__HippoVisualRoot";

    private Transform root;
    private Transform visualRoot;
    private Transform body;
    private Transform headPivot;
    private Transform head;
    private Transform upperJaw;
    private Transform jawPivot;
    private Transform lowerJaw;
    private Transform tail;
    private Transform legFL;
    private Transform legFR;
    private Transform legBL;
    private Transform legBR;
    private Transform eyePivotL;
    private Transform eyePivotR;
    private Transform eyeL;
    private Transform eyeR;

    private Vector3 bodyBaseLocalPos;
    private Vector3 bodyBaseLocalScale;
    private Quaternion headBaseLocalRot;
    private Quaternion tailBaseLocalRot;
    private Quaternion jawBaseLocalRot;
    private Quaternion visualBaseLocalRot;
    private Quaternion eyePivotLBaseLocalRot;
    private Quaternion eyePivotRBaseLocalRot;
    private Vector3 eyeBaseScale;
    private Vector3 eyeBaseLocalPos;

    private HippoAnimState previousState;
    private float stateTimer;

    private enum MaterialSlot
    {
        Body,
        Eye,
        Mouth
    }

    private void Start()
    {
        BuildIfNeeded(false);
        SyncPreviousState();
    }

    private void OnEnable()
    {
        BuildIfNeeded(false);
        SyncPreviousState();
    }

    private void OnValidate()
    {
        BuildIfNeeded(false);
    }

    private void Update()
    {
        BuildIfNeeded(false);

        if (Application.isPlaying)
        {
            if (previousState != state)
            {
                OnStateChanged(previousState, state);
                previousState = state;
            }

            stateTimer += Time.deltaTime;
        }

        UpdateAnimation();
    }

    public void SetState(HippoAnimState newState)
    {
        if (state == newState)
            return;

        HippoAnimState old = state;
        state = newState;

        if (Application.isPlaying)
        {
            OnStateChanged(old, newState);
            previousState = newState;
        }
        else
        {
            previousState = newState;
        }
    }

    public void Rebuild()
    {
        BuildIfNeeded(true);
        SyncPreviousState();
    }

    private void SyncPreviousState()
    {
        previousState = state;
        stateTimer = 0f;
    }

    private void OnStateChanged(HippoAnimState oldState, HippoAnimState newState)
    {
        stateTimer = 0f;
    }

    private void BuildIfNeeded(bool force)
    {
        Transform existing = transform.Find(RootName);

        if (!force)
        {
            if (existing != null)
            {
                if (root == null)
                {
                    root = existing;
                    RecoverReferences();
                    CacheBases();
                }

                ApplyEyePlacement();
                return;
            }

            if (root != null)
                return;
        }

        ClearExisting();
        BuildVisual();
        RecoverReferences();
        CacheBases();
        ApplyEyePlacement();
    }

    private void RecoverReferences()
    {
        root = transform.Find(RootName);
        if (root == null)
            return;

        visualRoot = root.Find("VisualRoot");
        if (visualRoot == null)
            return;

        body = visualRoot.Find("Body");
        headPivot = visualRoot.Find("HeadPivot");
        tail = visualRoot.Find("Tail");
        legFL = visualRoot.Find("Leg_FL");
        legFR = visualRoot.Find("Leg_FR");
        legBL = visualRoot.Find("Leg_BL");
        legBR = visualRoot.Find("Leg_BR");

        if (headPivot != null)
        {
            head = headPivot.Find("Head");
            upperJaw = headPivot.Find("UpperJaw");
            jawPivot = headPivot.Find("JawPivot");
            eyePivotL = headPivot.Find("EyePivot_L");
            eyePivotR = headPivot.Find("EyePivot_R");

            if (jawPivot != null)
                lowerJaw = jawPivot.Find("LowerJaw");

            if (eyePivotL != null)
                eyeL = eyePivotL.Find("Eye_L");

            if (eyePivotR != null)
                eyeR = eyePivotR.Find("Eye_R");
        }
    }

    private void CacheBases()
    {
        if (body != null)
        {
            bodyBaseLocalPos = body.localPosition;
            bodyBaseLocalScale = body.localScale;
        }

        if (headPivot != null)
            headBaseLocalRot = headPivot.localRotation;

        if (tail != null)
            tailBaseLocalRot = tail.localRotation;

        if (jawPivot != null)
            jawBaseLocalRot = jawPivot.localRotation;

        if (visualRoot != null)
            visualBaseLocalRot = visualRoot.localRotation;

        if (eyePivotL != null)
            eyePivotLBaseLocalRot = eyePivotL.localRotation;

        if (eyePivotR != null)
            eyePivotRBaseLocalRot = eyePivotR.localRotation;

        if (eyeL != null)
        {
            eyeBaseScale = eyeL.localScale;
            eyeBaseLocalPos = eyeL.localPosition;
        }

        if (eyeR != null && eyeL == null)
            eyeBaseLocalPos = eyeR.localPosition;

        SetEyeScaleImmediate(eyeBaseScale);
        SetEyeLocalPositionImmediate(eyeBaseLocalPos);
    }

    private void ClearExisting()
    {
        Transform old = transform.Find(RootName);
        if (old == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(old.gameObject);
        else
            Destroy(old.gameObject);
#else
        Destroy(old.gameObject);
#endif

        root = null;
        visualRoot = null;
        body = null;
        headPivot = null;
        head = null;
        upperJaw = null;
        jawPivot = null;
        lowerJaw = null;
        tail = null;
        legFL = null;
        legFR = null;
        legBL = null;
        legBR = null;
        eyePivotL = null;
        eyePivotR = null;
        eyeL = null;
        eyeR = null;
    }

    private void BuildVisual()
    {
        root = CreateNode(RootName, transform);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one * overallScale;

        visualRoot = CreateNode("VisualRoot", root);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        body = CreatePart(
            "Body",
            PrimitiveType.Sphere,
            visualRoot,
            new Vector3(0f, 0.95f, 0f),
            Vector3.zero,
            new Vector3(2.8f, 1.5f, 3.5f),
            MaterialSlot.Body);

        headPivot = CreateNode("HeadPivot", visualRoot);
        headPivot.localPosition = new Vector3(0f, 1.02f, 2.05f);
        headPivot.localRotation = Quaternion.identity;

        head = CreatePart(
            "Head",
            PrimitiveType.Sphere,
            headPivot,
            Vector3.zero,
            Vector3.zero,
            new Vector3(2.0f, 1.15f, 1.7f),
            MaterialSlot.Body);

        upperJaw = CreatePart(
            "UpperJaw",
            PrimitiveType.Sphere,
            headPivot,
            new Vector3(0f, -0.18f, 0.82f),
            Vector3.zero,
            new Vector3(1.7f, 0.48f, 1.0f),
            MaterialSlot.Mouth);

        jawPivot = CreateNode("JawPivot", headPivot);
        jawPivot.localPosition = new Vector3(0f, -0.1f, 0.38f);
        jawPivot.localRotation = Quaternion.identity;

        lowerJaw = CreatePart(
            "LowerJaw",
            PrimitiveType.Sphere,
            jawPivot,
            new Vector3(0f, -0.16f, 0.46f),
            Vector3.zero,
            new Vector3(1.65f, 0.32f, 0.92f),
            MaterialSlot.Mouth);

        eyePivotL = CreateNode("EyePivot_L", headPivot);
        eyePivotL.localPosition = new Vector3(-eyePivotSideOffset, eyePivotHeightOffset, eyePivotForwardOffset);
        eyePivotL.localRotation = Quaternion.identity;

        eyePivotR = CreateNode("EyePivot_R", headPivot);
        eyePivotR.localPosition = new Vector3(eyePivotSideOffset, eyePivotHeightOffset, eyePivotForwardOffset);
        eyePivotR.localRotation = Quaternion.identity;

        eyeL = CreatePart(
            "Eye_L",
            PrimitiveType.Sphere,
            eyePivotL,
            new Vector3(0f, 0f, eyeBallForwardOffset),
            Vector3.zero,
            new Vector3(eyeScale, eyeScale, eyeScale),
            MaterialSlot.Eye);

        eyeR = CreatePart(
            "Eye_R",
            PrimitiveType.Sphere,
            eyePivotR,
            new Vector3(0f, 0f, eyeBallForwardOffset),
            Vector3.zero,
            new Vector3(eyeScale, eyeScale, eyeScale),
            MaterialSlot.Eye);

        CreatePart(
            "Ear_L",
            PrimitiveType.Sphere,
            headPivot,
            new Vector3(-0.56f, 0.3f, -0.08f),
            new Vector3(0f, 0f, 18f),
            new Vector3(0.20f, 0.28f, 0.12f),
            MaterialSlot.Body);

        CreatePart(
            "Ear_R",
            PrimitiveType.Sphere,
            headPivot,
            new Vector3(0.56f, 0.3f, -0.08f),
            new Vector3(0f, 0f, -18f),
            new Vector3(0.20f, 0.28f, 0.12f),
            MaterialSlot.Body);

        tail = CreatePart(
            "Tail",
            PrimitiveType.Cylinder,
            visualRoot,
            new Vector3(0f, 1.0f, -1.9f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.06f, 0.22f, 0.06f),
            MaterialSlot.Body);

        legFL = CreateLeg("Leg_FL", new Vector3(-0.78f, 0.52f, 1.0f));
        legFR = CreateLeg("Leg_FR", new Vector3(0.78f, 0.52f, 1.0f));
        legBL = CreateLeg("Leg_BL", new Vector3(-0.78f, 0.52f, -1.0f));
        legBR = CreateLeg("Leg_BR", new Vector3(0.78f, 0.52f, -1.0f));
    }

    private void UpdateAnimation()
    {
        if (!IsRigReady())
            return;

        float t = Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds;
        float dt = DeltaTimeSafe();

        ResetTowardBase(dt);

        switch (state)
        {
            case HippoAnimState.Idle:
                AnimateIdle(t);
                break;

            case HippoAnimState.Chase:
                AnimateChase(t);
                break;

            case HippoAnimState.Eat:
                AnimateEat();
                break;

            case HippoAnimState.Stunned:
                AnimateStunned(t);
                break;
        }

        UpdateEyes(t);
    }

    private bool IsRigReady()
    {
        return body != null &&
               headPivot != null &&
               jawPivot != null &&
               tail != null &&
               legFL != null &&
               legFR != null &&
               legBL != null &&
               legBR != null &&
               eyePivotL != null &&
               eyePivotR != null;
    }

    private void ResetTowardBase(float dt)
    {
        legFL.localRotation = Quaternion.Slerp(legFL.localRotation, Quaternion.identity, 8f * dt);
        legFR.localRotation = Quaternion.Slerp(legFR.localRotation, Quaternion.identity, 8f * dt);
        legBL.localRotation = Quaternion.Slerp(legBL.localRotation, Quaternion.identity, 8f * dt);
        legBR.localRotation = Quaternion.Slerp(legBR.localRotation, Quaternion.identity, 8f * dt);

        body.localPosition = Vector3.Lerp(body.localPosition, bodyBaseLocalPos, 8f * dt);
        body.localScale = Vector3.Lerp(body.localScale, bodyBaseLocalScale, 8f * dt);

        headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, headBaseLocalRot, 8f * dt);
        tail.localRotation = Quaternion.Slerp(tail.localRotation, tailBaseLocalRot, 8f * dt);
        jawPivot.localRotation = Quaternion.Slerp(jawPivot.localRotation, jawBaseLocalRot, 10f * dt);
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, visualBaseLocalRot, 8f * dt);

        eyePivotL.localRotation = Quaternion.Slerp(eyePivotL.localRotation, eyePivotLBaseLocalRot, 8f * dt);
        eyePivotR.localRotation = Quaternion.Slerp(eyePivotR.localRotation, eyePivotRBaseLocalRot, 8f * dt);

        SetEyeScaleImmediate(eyeBaseScale);
    }

    private void AnimateIdle(float t)
    {
        float breath = Mathf.Sin(t * idleBreathSpeed) * idleBreathAmount;
        body.localPosition = bodyBaseLocalPos + new Vector3(0f, breath, 0f);

        float headNod = Mathf.Sin(t * idleHeadNodSpeed) * idleHeadNodAngle;
        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(headNod, 0f, 0f);

        float tailSwing = Mathf.Sin(t * 1.2f) * (tailSwingAngle * idleTailSwingFactor);
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwing, 0f);

        jawPivot.localRotation = jawBaseLocalRot;
    }

    private void AnimateChase(float t)
    {
        if (animateWalk)
        {
            float phase = t * walkCycleSpeed;

            float fl = Mathf.Sin(phase) * legAngle;
            float fr = Mathf.Sin(phase + Mathf.PI) * legAngle;
            float bl = Mathf.Sin(phase + Mathf.PI) * legAngle;
            float br = Mathf.Sin(phase) * legAngle;

            legFL.localRotation = Quaternion.Euler(fl, 0f, 0f);
            legFR.localRotation = Quaternion.Euler(fr, 0f, 0f);
            legBL.localRotation = Quaternion.Euler(bl, 0f, 0f);
            legBR.localRotation = Quaternion.Euler(br, 0f, 0f);

            float bob = Mathf.Abs(Mathf.Sin(phase * 1.2f)) * bodyBobAmount;
            body.localPosition = bodyBaseLocalPos + new Vector3(0f, bob, 0f);

            float headSwing = Mathf.Sin(phase) * headSwingAngle;
            headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(headSwing, 0f, 0f);

            float tailSwing = Mathf.Sin(phase + 0.8f) * tailSwingAngle;
            tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwing, 0f);
        }

        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpenAngle * 0.12f, 0f, 0f);
    }

    private void AnimateEat()
    {
        float p = Mathf.Clamp01(stateTimer / Mathf.Max(0.0001f, eatAnimDuration));

        float jumpArc = Mathf.Sin(p * Mathf.PI);
        float forward = jumpArc * eatForwardStretch;

        body.localPosition = bodyBaseLocalPos + new Vector3(0f, jumpArc * eatJumpHeight, forward);

        Quaternion foldedLeg = Quaternion.Euler(eatLegFoldAngle, 0f, 0f);
        legFL.localRotation = foldedLeg;
        legFR.localRotation = foldedLeg;
        legBL.localRotation = foldedLeg;
        legBR.localRotation = foldedLeg;

        float headPitch = Mathf.Lerp(6f, -10f, p);
        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(headPitch, 0f, 0f);

        float mouthPulse = 0.65f + 0.35f * Mathf.Sin(stateTimer * 14f);
        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpenAngle * mouthPulse, 0f, 0f);

        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, -tailSwingAngle * 0.4f, 0f);

        Vector3 bodyScale = bodyBaseLocalScale;

        if (p < 0.5f)
        {
            float up = p / 0.5f;
            float stretch = Mathf.Sin(up * Mathf.PI * 0.5f);

            bodyScale.x *= Mathf.Lerp(1f, eatSquashXZ, stretch);
            bodyScale.z *= Mathf.Lerp(1f, eatSquashXZ, stretch);
            bodyScale.y *= Mathf.Lerp(1f, eatStretchY, stretch);
        }
        else
        {
            float down = (p - 0.5f) / 0.5f;
            float squash = Mathf.Sin(down * Mathf.PI);

            bodyScale.x *= Mathf.Lerp(1f, landStretchXZ, squash);
            bodyScale.z *= Mathf.Lerp(1f, landStretchXZ, squash);
            bodyScale.y *= Mathf.Lerp(1f, landSquashY, squash);
        }

        body.localScale = bodyScale;
    }

    private void AnimateStunned(float t)
    {
        float shake = Mathf.Sin(t * stunnedShakeSpeed) * stunnedShakeAngle;
        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(0f, 0f, shake);

        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpenAngle * 0.08f, 0f, 0f);
    }

    private void UpdateEyes(float t)
    {
        if (state == HippoAnimState.Chase)
        {
            float look = Mathf.Sin(t * eyeLookSpeed) * eyeLookSideAngle;

            eyePivotL.localRotation = eyePivotLBaseLocalRot * Quaternion.Euler(0f, look, 0f);
            eyePivotR.localRotation = eyePivotRBaseLocalRot * Quaternion.Euler(0f, look, 0f);
        }
        else
        {
            eyePivotL.localRotation = eyePivotLBaseLocalRot;
            eyePivotR.localRotation = eyePivotRBaseLocalRot;
        }
    }

    private void SetEyeScaleImmediate(Vector3 scale)
    {
        if (eyeL != null)
            eyeL.localScale = scale;

        if (eyeR != null)
            eyeR.localScale = scale;
    }

    private void SetEyeLocalPositionImmediate(Vector3 localPos)
    {
        if (eyeL != null)
            eyeL.localPosition = localPos;

        if (eyeR != null)
            eyeR.localPosition = localPos;
    }

    private void ApplyEyePlacement()
    {
        float side = Mathf.Abs(eyePivotSideOffset);
        float height = eyePivotHeightOffset;
        float forward = eyePivotForwardOffset;

        if (eyePivotL != null)
            eyePivotL.localPosition = new Vector3(-side, height, forward);

        if (eyePivotR != null)
            eyePivotR.localPosition = new Vector3(side, height, forward);

        float uniformEyeScale = Mathf.Max(0.01f, eyeScale);
        eyeBaseScale = new Vector3(uniformEyeScale, uniformEyeScale, uniformEyeScale);
        SetEyeScaleImmediate(eyeBaseScale);

        float eyeballForward = Mathf.Max(0f, eyeBallForwardOffset);
        eyeBaseLocalPos = new Vector3(0f, 0f, eyeballForward);
        SetEyeLocalPositionImmediate(eyeBaseLocalPos);
    }

    private float DeltaTimeSafe()
    {
        return Application.isPlaying ? Time.deltaTime : 0.016f;
    }

    private Transform CreateLeg(string name, Vector3 localPos)
    {
        Transform legRoot = CreateNode(name, visualRoot);
        legRoot.localPosition = localPos;
        legRoot.localRotation = Quaternion.identity;
        legRoot.localScale = Vector3.one;

        Transform upper = CreatePart(
            "Upper",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, -0.28f, 0f),
            Vector3.zero,
            new Vector3(0.46f, 0.88f, 0.46f),
            MaterialSlot.Body);

        Transform foot = CreatePart(
            "Foot",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, -0.68f, 0.08f),
            Vector3.zero,
            new Vector3(0.58f, 0.20f, 0.70f),
            MaterialSlot.Body);

        RemoveCollider(upper.gameObject);
        RemoveCollider(foot.gameObject);

        return legRoot;
    }

    private Transform CreateNode(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private Transform CreatePart(
        string name,
        PrimitiveType primitiveType,
        Transform parent,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale,
        MaterialSlot slot)
    {
        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(localEulerAngles);
        go.transform.localScale = localScale;

        ApplyMaterial(go, slot);
        return go.transform;
    }

    private void ApplyMaterial(GameObject go, MaterialSlot slot)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material assigned = null;

        switch (slot)
        {
            case MaterialSlot.Body:
                assigned = bodyMaterial != null ? bodyMaterial : CreateFallbackMaterial("HippoBody_Fallback", fallbackBodyColor);
                break;

            case MaterialSlot.Eye:
                assigned = eyeMaterial != null ? eyeMaterial : CreateFallbackMaterial("HippoEye_Fallback", fallbackEyeColor);
                break;

            case MaterialSlot.Mouth:
                assigned = mouthMaterial != null ? mouthMaterial : CreateFallbackMaterial("HippoMouth_Fallback", fallbackMouthColor);
                break;
        }

        renderer.sharedMaterial = assigned;
    }

    private Material CreateFallbackMaterial(string matName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;
        mat.color = color;
        return mat;
    }

    private void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(c);
        else
            Destroy(c);
#else
        Destroy(c);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, 0.25f);
    }
}
