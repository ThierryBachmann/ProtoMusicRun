using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class HippoVisual : ProceduralCreatureVisualBase
{
    [Header("STRUCTURE / Global")]
    [Range(0.25f, 5f)] public float overallScale = 1f;

    [Header("STRUCTURE / Body & Head")]
    [Range(0.1f, 3f)] public float bodyHeightFactor = 1.3f;
    [Range(0.1f, 3f)] public float headVolumeFactor = 1.2f;

    [Header("STRUCTURE / Ears")]
    [Range(0.1f, 3f)] public float earWidthFactor = 1.3f;
    [Tooltip("Extra ear placement to keep ears visible on top/sides of the head.")]
    public Vector3 earPlacementOffset = new Vector3(0.10f, 0.26f, -0.04f);

    [Header("STRUCTURE / Tail")]
    [Range(-2f, 2f)] public float tailSideOffset = 0f;
    [Range(-1f, 3f)] public float tailHeightOffset = 1.0f;
    [Range(-4f, 2f)] public float tailForwardOffset = -1.9f;
    [Range(0f, 180f)] public float tailPitchDegrees = 90f;
    [Tooltip("Tail half-width on local X (ellipsoid).")]
    [FormerlySerializedAs("tailRadius")]
    [Range(0.01f, 1f)] public float tailRadiusX = 0.06f;
    [Tooltip("Tail half-width on local Z (ellipsoid).")]
    [Range(0.01f, 1f)] public float tailRadiusZ = 0.06f;
    [Tooltip("Tail half-length along local Y before pitch rotation (ellipsoid).")]
    [Range(0.05f, 3f)] public float tailLength = 0.22f;

    [Header("STRUCTURE / Mouth")]
    [FormerlySerializedAs("jawHeight")]
    [Range(0.1f, 4f)] public float heightJaw = 0.8f;
    [FormerlySerializedAs("jawHeightUpperToLowerRatio")]
    [Range(0.2f, 5f)] public float ratioHeightUpperToLowerJaw = 1.5f;
    [FormerlySerializedAs("jawLength")]
    [Range(0.1f, 6f)] public float lengthJaw = 1.92f;
    [FormerlySerializedAs("jawLengthUpperToLowerRatio")]
    [Range(0.2f, 5f)] public float ratioLengthUpperToLowerJaw = 1.087f;
    [FormerlySerializedAs("jawWidth")]
    [Range(0.1f, 8f)] public float widthJaw = 3.35f;
    [FormerlySerializedAs("jawWidthUpperToLowerRatio")]
    [Range(0.2f, 5f)] public float ratioWidthUpperToLowerJaw = 1.03f;
    [FormerlySerializedAs("jawHeightOffset")]
    [Range(-4f, 4f)] public float offsetHeightJaw = -0.34f;
    [FormerlySerializedAs("jawHeightOffsetUpperToLowerRatio")]
    [Range(0.2f, 5f)] public float ratioOffsetHeightUpperToLowerJaw = 1.125f;
    [FormerlySerializedAs("jawForwardOffset")]
    [Range(-4f, 4f)] public float offsetForwardJaw = 1.28f;
    [FormerlySerializedAs("jawForwardOffsetUpperToLowerRatio")]
    [Range(0.2f, 5f)] public float ratioOffsetForwardUpperToLowerJaw = 1.783f;
    [FormerlySerializedAs("jawPivotHeightOffset")]
    [Range(-2f, 2f)] public float offsetPivotHeightJaw = -0.1f;
    [FormerlySerializedAs("jawPivotForwardOffset")]
    [Range(-2f, 2f)] public float offsetPivotForwardJaw = 0.38f;
    [FormerlySerializedAs("jawPivotBasePitch")]
    [Range(-45f, 45f)] public float basePitchJaw = 0f;

    [Header("STRUCTURE / Legs")]
    [Range(0.1f, 2f)] public float upperLegThickness = 0.60f;
    [Range(0.2f, 3f)] public float upperLegLength = 1.30f;
    [Range(-1f, 2f)] public float legAttachHeight = 0.78f;
    [Range(0.1f, 2f)] public float ankleDiameter = 0.50f;
    [Range(0.1f, 2f)] public float footHeight = 0.38f;
    [Range(0.1f, 3f)] public float footWidth = 0.84f;

    [Header("STRUCTURE / Eyes Geometry")]
    [Tooltip("Horizontal offset of eyes from head center.")]
    [Range(0f, 2f)] public float eyePivotSideOffset = 0.58f;
    [Tooltip("Vertical offset of eyes from head center.")]
    [Range(-1f, 2f)] public float eyePivotHeightOffset = 0.24f;
    [Tooltip("Forward offset of eyes from head center. Increase to pull eyes out of the head.")]
    [Range(-1f, 2f)] public float eyePivotForwardOffset = 0.92f;
    [Tooltip("Uniform eye sphere scale.")]
    [Range(0.01f, 2f)] public float eyeScale = 0.82f;
    [Tooltip("Pupil size ratio relative to eye size (0.5 = half-eye diameter).")]
    [Range(0.05f, 1.0f)] public float pupilScale = 0.48f;
    [Tooltip("Pupil center forward offset in eye local space (0=center, 0.5=eye surface).")]
    [Range(0f, 0.9f)] public float pupilForwardOffset = 0.27f;

    [Header("STRUCTURE / Collision")]
    [Tooltip("Remove physical colliders from generated visual parts. CharacterController remains the unique movement collider.")]
    public bool removeGeneratedPartColliders = true;
    [Tooltip("Ensure one trigger SphereCollider on Hippo root, auto-sized from current structure.")]
    public bool autoStructureTriggerCollider = true;
    [Tooltip("Optional local offset applied to the auto trigger center.")]
    public Vector3 triggerCenterOffset = Vector3.zero;
    [Tooltip("Multiplier applied to computed trigger radius.")]
    [Range(0.2f, 3f)] public float triggerRadiusScale = 1f;
    [Tooltip("Extra radius padding added after scaling.")]
    [Range(0f, 2f)] public float triggerRadiusPadding = 0.05f;

    [Header("STRUCTURE / Attachment")]
    [Tooltip("Keep visual anchored on parent local X/Z. Prevents apparent floating/sinking when controller tilts on slopes.")]
    public bool lockLocalXZToParent = true;

    [Header("Materials")]
    public Material bodyMaterial;
    public Material earMaterial;
    public Material scleraMaterial;
    public Material pupilMaterial;
    public Material mouthMaterial;
    public Material tailMaterial;

    [Header("Fallback Colors")]
    public Color fallbackBodyColor = new Color(0.56f, 0.58f, 0.63f);
    public Color fallbackEarColor = new Color(0.24f, 0.26f, 0.30f);
    public Color fallbackTailColor = new Color(0.24f, 0.26f, 0.30f);
    public Color fallbackScleraColor = new Color(0.92f, 0.94f, 0.97f);
    [FormerlySerializedAs("fallbackEyeColor")]
    public Color fallbackPupilColor = new Color(0.08f, 0.08f, 0.08f);
    public Color fallbackMouthColor = new Color(0.55f, 0.22f, 0.24f);

    [Header("ANIMATION / Shared")]
    [Range(0f, 90f)] public float mouthOpenAngle = 35f;

    [Header("ANIMATION / Idle")]
    public float idleBreathSpeed = 1.6f;
    public float idleBreathAmount = 0.03f;
    public float idleHeadNodAngle = 1.5f;
    public float idleHeadNodSpeed = 1.3f;
    public float idleTailSwingFactor = 0.25f;

    [Header("ANIMATION / Chase")]
    public bool animateWalk = true;
    [Range(0f, 20f)] public float walkCycleSpeed = 4.2f;
    [Range(0.01f, 85f)] public float legAngle = 16f;
    [Range(0f, 1f)] public float bodyBobAmount = 0.05f;
    [Range(0f, 45f)] public float headSwingAngle = 3f;
    [Range(0f, 60f)] public float tailSwingAngle = 14f;

    [Header("ANIMATION / Chase Phase Coupling")]
    [Tooltip("When enabled, chase gait phase advances from real horizontal displacement instead of Time.time.")]
    public bool driveChasePhaseFromDisplacement = true;
    [Tooltip("Ignore tiny displacements to avoid micro-jitter in gait progression.")]
    [Range(0f, 1f)] public float chaseMinDisplacement = 0.0005f;
    [Tooltip("Ignore a single-frame displacement above this threshold (teleport/recovery safety).")]
    [Range(0.001f, 5f)] public float chaseMaxDisplacementPerFrame = 1.25f;

    [Header("ANIMATION / Wait Player")]
    [Range(0f, 1f)] public float waitBodyBobAmount = 0.035f;
    [Range(0f, 20f)] public float waitBodyBobSpeed = 2.4f;
    [Range(0f, 45f)] public float waitHeadYawAngle = 7f;
    [Range(0f, 20f)] public float waitHeadYawSpeed = 1.8f;
    [Range(0f, 60f)] public float waitLegStompAngle = 6f;
    [Range(0f, 20f)] public float waitLegStompSpeed = 5f;
    [Range(0f, 60f)] public float waitTailSwingAngle = 11f;
    [Range(0f, 60f)] public float waitMouthOpenBase = 8f;

    [Header("ANIMATION / Eyes")]
    [Range(0f, 75f)] public float eyeLookSideAngle = 35f;
    [Range(0f, 20f)] public float eyeLookSpeed = 2.2f;
    [Tooltip("Horizontal pupil travel in eye local space when looking left/right.")]
    [Range(0f, 0.23f)] public float pupilLookSideOffset = 0.08f;

    [Header("ANIMATION / Eat")]
    [Range(0.01f, 5f)] public float eatAnimDuration = 0.75f;
    [Range(0f, 5f)] public float eatJumpHeight = 0.45f;
    [Range(0f, 2f)] public float eatForwardStretch = 0.18f;
    [Range(0f, 90f)] public float eatLegFoldAngle = 50f;
    [Tooltip("Global hippo pitch during EAT_ATTACK (positive tilts up).")]
    [Range(-90f, 90f)] public float eatAttackGlobalPitch = 20f;
    [Tooltip("Global hippo pitch during EAT_RECOVERY (negative tilts down).")]
    [Range(-90f, 90f)] public float eatRecoveryGlobalPitch = -20f;
    [Tooltip("Blend duration to transition global pitch from EAT_ATTACK to EAT_RECOVERY.")]
    [Range(0f, 2f)] public float eatRecoveryPitchBlendDuration = 0.25f;
    [Tooltip("Leg pitch used in landing/recovery pose (0 means vertical legs).")]
    [Range(0f, 90f)] public float eatRecoveryLegForwardAngle = 45f;

    [Header("ANIMATION / Eat Squash & Stretch")]
    [Range(0.5f, 2f)] public float eatStretchY = 1.12f;
    [Range(0.5f, 1.5f)] public float eatSquashXZ = 0.92f;
    [Range(0.5f, 1.5f)] public float landSquashY = 0.90f;
    [Range(0.5f, 2f)] public float landStretchXZ = 1.06f;

    [Header("ANIMATION / Stunned")]
    [Range(0f, 90f)] public float stunnedShakeAngle = 10f;
    [Range(0f, 60f)] public float stunnedShakeSpeed = 20f;

    [Header("Debug")]
    public bool drawDebug = false;

    private const string RootName = "__HippoVisualRoot";
    private const float LegSideOffset = 0.76f;
    private const float LegFrontOffset = 1.0f;
    private const float UpperLegCenterYRatio = -0.24f / 1.30f;
    private const float AnkleCenterYRatio = -0.88f / 1.30f;
    private const float FootCenterYRatio = -1.16f / 1.30f;
    private const float AnkleForwardFromFootWidthRatio = 0.08f / 0.84f;
    private const float FootForwardFromFootWidthRatio = 0.18f / 0.84f;
    private const float FootDepthFromFootWidthRatio = 0.98f / 0.84f;

    private Transform root;
    private Transform visualRoot;
    private Transform body;
    private Transform headPivot;
    private Transform head;
    private Transform upperJaw;
    private Transform jawPivot;
    private Transform lowerJaw;
    private Transform earL;
    private Transform earR;
    private Transform tail;
    private Transform legFL;
    private Transform legFR;
    private Transform legBL;
    private Transform legBR;
    private Transform eyeL;
    private Transform eyeR;
    private Transform pupilL;
    private Transform pupilR;

    private Vector3 bodyBaseLocalPos;
    private Vector3 bodyBaseLocalScale;
    private Quaternion headBaseLocalRot;
    private Quaternion tailBaseLocalRot;
    private Quaternion jawBaseLocalRot;
    private Quaternion visualBaseLocalRot;
    private Vector3 eyeBaseScale;
    private Vector3 eyeLBaseLocalPos;
    private Vector3 eyeRBaseLocalPos;
    private Vector3 pupilBaseScale;
    private Vector3 pupilBaseLocalPos;

    private Material fallbackBodyMat;
    private Material fallbackEarMat;
    private Material fallbackTailMat;
    private Material fallbackScleraMat;
    private Material fallbackPupilMat;
    private Material fallbackMouthMat;
    private SphereCollider structureTriggerCollider;
    private bool generatedPartCollidersPurged;
    private bool rootColliderCleanupDone;
    private float chaseGaitPhase;
    private Vector3 chaseLastWorldPosition;
    private bool chaseHasLastWorldPosition;
    private float chaseLegLengthEstimate = 0.8f;
    private float chaseCycleDistance = 1f;
    private float chaseCachedLegAngle = float.NaN;

    [ContextMenu("Rebuild Visual")]
    public void Rebuild()
    {
        RebuildVisual();
    }

    protected override void OnStateChanged(CreatureVisualState oldState, CreatureVisualState newState)
    {
        base.OnStateChanged(oldState, newState);

        if (IsChaseLikeState(newState) || IsChaseLikeState(oldState))
            ResetChaseDisplacementSampling();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (chaseMaxDisplacementPerFrame <= chaseMinDisplacement)
            chaseMaxDisplacementPerFrame = chaseMinDisplacement + 0.001f;
    }

    // Central lifecycle method:
    // - fast path: reuse existing rig and apply live structure/eye updates
    // - forced path: rebuild the full procedural rig
    protected override void BuildIfNeeded(bool force)
    {
        ApplyAttachmentConstraint();
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

                if (eyeL == null || eyeR == null || pupilL == null || pupilR == null)
                {
                    BuildIfNeeded(true);
                    return;
                }

                if (!HasUpdatedLegGeometry())
                {
                    BuildIfNeeded(true);
                    return;
                }

                if (materialsDirty)
                {
                    ApplyCurrentMaterialsToRig();
                    materialsDirty = false;
                }

                ApplyStructurePlacement();
                ApplyEyePlacement();
                ConfigureCollisionSetup();
                return;
            }

            if (root != null)
            {
                if (materialsDirty)
                {
                    ApplyCurrentMaterialsToRig();
                    materialsDirty = false;
                }

                ApplyStructurePlacement();
                ApplyEyePlacement();
                ConfigureCollisionSetup();
                return;
            }
        }

        ClearExisting();
        BuildVisual();
        RecoverReferences();
        ApplyStructurePlacement();
        ApplyEyePlacement();
        ConfigureCollisionSetup();
        CacheBases();
        ApplyCurrentMaterialsToRig();
        materialsDirty = false;
    }

    private void ApplyAttachmentConstraint()
    {
        if (!lockLocalXZToParent || transform.parent == null)
            return;

        Vector3 localPos = transform.localPosition;
        if (Mathf.Abs(localPos.x) <= 0.0001f && Mathf.Abs(localPos.z) <= 0.0001f)
            return;

        transform.localPosition = new Vector3(0f, localPos.y, 0f);
    }

    private bool HasUpdatedLegGeometry()
    {
        return HasLegParts(legFL) &&
               HasLegParts(legFR) &&
               HasLegParts(legBL) &&
               HasLegParts(legBR);
    }

    private static bool HasLegParts(Transform legRoot)
    {
        return legRoot != null &&
               legRoot.Find("Upper") != null &&
               legRoot.Find("Ankle") != null &&
               legRoot.Find("Foot") != null;
    }

    private void ApplyCurrentMaterialsToRig()
    {
        if (body != null)
            ApplyMaterial(body.gameObject, CreatureMaterialSlot.Body);

        if (head != null)
            ApplyMaterial(head.gameObject, CreatureMaterialSlot.Body);

        if (tail != null)
            ApplyMaterial(tail.gameObject, CreatureMaterialSlot.Tail);

        if (upperJaw != null)
            ApplyMaterial(upperJaw.gameObject, CreatureMaterialSlot.Mouth);

        if (lowerJaw != null)
            ApplyMaterial(lowerJaw.gameObject, CreatureMaterialSlot.Mouth);

        if (eyeL != null)
            ApplyMaterial(eyeL.gameObject, CreatureMaterialSlot.EyeSclera);

        if (eyeR != null)
            ApplyMaterial(eyeR.gameObject, CreatureMaterialSlot.EyeSclera);

        if (pupilL != null)
            ApplyMaterial(pupilL.gameObject, CreatureMaterialSlot.EyePupil);

        if (pupilR != null)
            ApplyMaterial(pupilR.gameObject, CreatureMaterialSlot.EyePupil);

        if (earL != null)
            ApplyMaterial(earL.gameObject, CreatureMaterialSlot.Ears);

        if (earR != null)
            ApplyMaterial(earR.gameObject, CreatureMaterialSlot.Ears);

        ApplyLegMaterials(legFL);
        ApplyLegMaterials(legFR);
        ApplyLegMaterials(legBL);
        ApplyLegMaterials(legBR);
    }

    private void ApplyLegMaterials(Transform legRoot)
    {
        if (legRoot == null)
            return;

        Transform upper = legRoot.Find("Upper");
        if (upper != null)
            ApplyMaterial(upper.gameObject, CreatureMaterialSlot.Body);

        Transform ankle = legRoot.Find("Ankle");
        if (ankle != null)
            ApplyMaterial(ankle.gameObject, CreatureMaterialSlot.Body);

        Transform foot = legRoot.Find("Foot");
        if (foot != null)
            ApplyMaterial(foot.gameObject, CreatureMaterialSlot.Body);
    }

    protected override Material ResolveMaterialForSlot(CreatureMaterialSlot slot)
    {
        switch (slot)
        {
            case CreatureMaterialSlot.Body:
                return bodyMaterial != null ? bodyMaterial : GetOrCreateBodyFallbackMaterial();

            case CreatureMaterialSlot.Tail:
                return tailMaterial != null ? tailMaterial : GetOrCreateTailFallbackMaterial();

            case CreatureMaterialSlot.Ears:
                return earMaterial != null ? earMaterial : GetOrCreateEarFallbackMaterial();

            case CreatureMaterialSlot.EyeSclera:
                return scleraMaterial != null ? scleraMaterial : GetOrCreateScleraFallbackMaterial();

            case CreatureMaterialSlot.EyePupil:
                return pupilMaterial != null ? pupilMaterial : GetOrCreatePupilFallbackMaterial();

            case CreatureMaterialSlot.Mouth:
                return mouthMaterial != null ? mouthMaterial : GetOrCreateMouthFallbackMaterial();

            default:
                return bodyMaterial != null ? bodyMaterial : GetOrCreateBodyFallbackMaterial();
        }
    }

    protected override string FallbackMaterialContextName => nameof(HippoVisual);

    private Material GetOrCreateBodyFallbackMaterial()
    {
        if (fallbackBodyMat == null)
            fallbackBodyMat = CreateFallbackMaterial("CreatureBody_Fallback", fallbackBodyColor);
        return fallbackBodyMat;
    }

    private Material GetOrCreateScleraFallbackMaterial()
    {
        if (fallbackScleraMat == null)
            fallbackScleraMat = CreateFallbackMaterial("CreatureSclera_Fallback", fallbackScleraColor);
        return fallbackScleraMat;
    }

    private Material GetOrCreateTailFallbackMaterial()
    {
        if (fallbackTailMat == null)
            fallbackTailMat = CreateFallbackMaterial("CreatureTail_Fallback", fallbackTailColor);
        return fallbackTailMat;
    }

    private Material GetOrCreateEarFallbackMaterial()
    {
        if (fallbackEarMat == null)
            fallbackEarMat = CreateFallbackMaterial("CreatureEars_Fallback", fallbackEarColor);
        return fallbackEarMat;
    }

    private Material GetOrCreatePupilFallbackMaterial()
    {
        if (fallbackPupilMat == null)
            fallbackPupilMat = CreateFallbackMaterial("CreaturePupil_Fallback", fallbackPupilColor);
        return fallbackPupilMat;
    }

    private Material GetOrCreateMouthFallbackMaterial()
    {
        if (fallbackMouthMat == null)
            fallbackMouthMat = CreateFallbackMaterial("CreatureMouth_Fallback", fallbackMouthColor);
        return fallbackMouthMat;
    }

    // Rebind cached Transform references from an already-built visual hierarchy.
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
            earL = headPivot.Find("Ear_L");
            earR = headPivot.Find("Ear_R");
            eyeL = headPivot.Find("Eye_L");
            eyeR = headPivot.Find("Eye_R");

            if (jawPivot != null)
                lowerJaw = jawPivot.Find("LowerJaw");

            if (eyeL != null)
                pupilL = eyeL.Find("Pupil_L");

            if (eyeR != null)
                pupilR = eyeR.Find("Pupil_R");
        }
    }

    // Cache neutral poses/scales used by animation resets and additive animation.
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

        if (eyeL != null)
        {
            eyeBaseScale = eyeL.localScale;
            eyeLBaseLocalPos = eyeL.localPosition;
        }

        if (eyeR != null)
        {
            if (eyeL == null)
                eyeBaseScale = eyeR.localScale;
            eyeRBaseLocalPos = eyeR.localPosition;
        }

        if (pupilL != null)
        {
            pupilBaseScale = pupilL.localScale;
            pupilBaseLocalPos = pupilL.localPosition;
        }
        else if (pupilR != null)
        {
            pupilBaseScale = pupilR.localScale;
            pupilBaseLocalPos = pupilR.localPosition;
        }

        SetEyeScaleImmediate(eyeBaseScale);
        SetEyeLocalPositionsImmediate(eyeLBaseLocalPos, eyeRBaseLocalPos);
        SetPupilScaleImmediate(pupilBaseScale);
        SetPupilLocalPositionImmediate(pupilBaseLocalPos);
        RecomputeChaseCalibration();
        ResetChaseDisplacementSampling();
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
        earL = null;
        earR = null;
        tail = null;
        legFL = null;
        legFR = null;
        legBL = null;
        legBR = null;
        eyeL = null;
        eyeR = null;
        pupilL = null;
        pupilR = null;
        structureTriggerCollider = null;
        generatedPartCollidersPurged = false;
        rootColliderCleanupDone = false;
        chaseHasLastWorldPosition = false;
        chaseGaitPhase = 0f;
        chaseLegLengthEstimate = 0.8f;
        chaseCycleDistance = 1f;
        chaseCachedLegAngle = float.NaN;
    }

    // One-shot procedural construction of the full hippo hierarchy.
    // Geometry creation happens here; per-frame/live parameter application is separated.
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

        float headLinearScale = Mathf.Pow(headVolumeFactor, 1f / 3f);

        body = CreatePart(
            "Body",
            PrimitiveType.Sphere,
            visualRoot,
            new Vector3(0f, 0.95f, 0f),
            Vector3.zero,
            new Vector3(2.8f, 1.5f * bodyHeightFactor, 3.5f),
            CreatureMaterialSlot.Body);

        headPivot = CreateNode("HeadPivot", visualRoot);
        headPivot.localPosition = new Vector3(0f, 1.02f, 2.05f);
        headPivot.localRotation = Quaternion.identity;

        head = CreatePart(
            "Head",
            PrimitiveType.Sphere,
            headPivot,
            Vector3.zero,
            Vector3.zero,
            new Vector3(2.0f, 1.15f, 1.7f) * headLinearScale,
            CreatureMaterialSlot.Body);

        ResolveJawDimensions(
            out float upperJawResolvedWidth,
            out float lowerJawResolvedWidth,
            out float upperJawResolvedHeight,
            out float lowerJawResolvedHeight,
            out float upperJawResolvedLength,
            out float lowerJawResolvedLength);
        ResolveJawOffsets(
            out float upperJawResolvedHeightOffset,
            out float lowerJawResolvedHeightOffset,
            out float upperJawResolvedForwardOffset,
            out float lowerJawResolvedForwardOffset);

        upperJaw = CreatePart(
            "UpperJaw",
            PrimitiveType.Sphere,
            headPivot,
            new Vector3(0f, upperJawResolvedHeightOffset, upperJawResolvedForwardOffset),
            Vector3.zero,
            new Vector3(upperJawResolvedWidth, upperJawResolvedHeight, upperJawResolvedLength),
            CreatureMaterialSlot.Mouth);

        jawPivot = CreateNode("JawPivot", headPivot);
        jawPivot.localPosition = new Vector3(0f, offsetPivotHeightJaw, offsetPivotForwardJaw);
        jawPivot.localRotation = Quaternion.Euler(basePitchJaw, 0f, 0f);

        lowerJaw = CreatePart(
            "LowerJaw",
            PrimitiveType.Sphere,
            jawPivot,
            new Vector3(0f, lowerJawResolvedHeightOffset, lowerJawResolvedForwardOffset),
            Vector3.zero,
            new Vector3(lowerJawResolvedWidth, lowerJawResolvedHeight, lowerJawResolvedLength),
            CreatureMaterialSlot.Mouth);

        float eyeSide = Mathf.Abs(eyePivotSideOffset);
        float eyeHeight = eyePivotHeightOffset;
        float eyeForward = eyePivotForwardOffset;

        eyeL = CreatePart(
            "Eye_L",
            PrimitiveType.Sphere,
            headPivot,
            new Vector3(-eyeSide, eyeHeight, eyeForward),
            Vector3.zero,
            new Vector3(eyeScale, eyeScale, eyeScale),
            CreatureMaterialSlot.EyeSclera);

        eyeR = CreatePart(
            "Eye_R",
            PrimitiveType.Sphere,
            headPivot,
            new Vector3(eyeSide, eyeHeight, eyeForward),
            Vector3.zero,
            new Vector3(eyeScale, eyeScale, eyeScale),
            CreatureMaterialSlot.EyeSclera);

        pupilL = CreatePart(
            "Pupil_L",
            PrimitiveType.Sphere,
            eyeL,
            new Vector3(0f, 0f, pupilForwardOffset),
            Vector3.zero,
            new Vector3(pupilScale, pupilScale, pupilScale),
            CreatureMaterialSlot.EyePupil);

        pupilR = CreatePart(
            "Pupil_R",
            PrimitiveType.Sphere,
            eyeR,
            new Vector3(0f, 0f, pupilForwardOffset),
            Vector3.zero,
            new Vector3(pupilScale, pupilScale, pupilScale),
            CreatureMaterialSlot.EyePupil);

        RemoveCollider(pupilL.gameObject);
        RemoveCollider(pupilR.gameObject);

        Vector3 baseEarPosL = new Vector3(-0.56f, 0.30f, -0.08f);
        Vector3 baseEarPosR = new Vector3(0.56f, 0.30f, -0.08f);
        Vector3 earPosL = new Vector3(baseEarPosL.x - earPlacementOffset.x, baseEarPosL.y + earPlacementOffset.y, baseEarPosL.z + earPlacementOffset.z);
        Vector3 earPosR = new Vector3(baseEarPosR.x + earPlacementOffset.x, baseEarPosR.y + earPlacementOffset.y, baseEarPosR.z + earPlacementOffset.z);
        Vector3 earScale = new Vector3(0.20f * earWidthFactor, 0.28f, 0.12f);

        earL = CreatePart(
            "Ear_L",
            PrimitiveType.Sphere,
            headPivot,
            earPosL,
            new Vector3(0f, 0f, 18f),
            earScale,
            CreatureMaterialSlot.Ears);

        earR = CreatePart(
            "Ear_R",
            PrimitiveType.Sphere,
            headPivot,
            earPosR,
            new Vector3(0f, 0f, -18f),
            earScale,
            CreatureMaterialSlot.Ears);

        tail = CreatePart(
            "Tail",
            PrimitiveType.Sphere,
            visualRoot,
            new Vector3(tailSideOffset, tailHeightOffset, tailForwardOffset),
            new Vector3(tailPitchDegrees, 0f, 0f),
            GetTailEllipsoidScale(),
            CreatureMaterialSlot.Tail);

        legFL = CreateLeg("Leg_FL", new Vector3(-LegSideOffset, legAttachHeight, LegFrontOffset));
        legFR = CreateLeg("Leg_FR", new Vector3(LegSideOffset, legAttachHeight, LegFrontOffset));
        legBL = CreateLeg("Leg_BL", new Vector3(-LegSideOffset, legAttachHeight, -LegFrontOffset));
        legBR = CreateLeg("Leg_BR", new Vector3(LegSideOffset, legAttachHeight, -LegFrontOffset));
    }

    // Main animation dispatch called every frame.
    // Each state applies its pose on top of the cached neutral bases.
    protected override void UpdateAnimation()
    {
        if (!IsRigReady())
            return;

        float t = Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds;
        float dt = DeltaTimeSafe();

        ResetTowardBase(dt);

        switch (state)
        {
            case CreatureVisualState.Idle:
                AnimateIdle(t);
                break;

            case CreatureVisualState.Follow:
            case CreatureVisualState.Overtake:
            case CreatureVisualState.Hunt:
            case CreatureVisualState.Recenter:
            case CreatureVisualState.LeashReturn:
                AnimateChase(t);
                break;

            case CreatureVisualState.WaitPlayer:
                AnimateWaitPlayer(t);
                break;

            case CreatureVisualState.EatAttack:
                AnimateEatAttackPose();
                break;

            case CreatureVisualState.EatRecovery:
                AnimateEatRecoveryPose();
                break;

            case CreatureVisualState.Stunned:
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
               eyeL != null &&
               eyeR != null;
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
            float phase = ResolveChasePhase(t);
            float appliedLegAngle = legAngle;
            float appliedBodyBob = bodyBobAmount;
            float appliedHeadSwing = headSwingAngle;
            float appliedTailSwing = tailSwingAngle;

            float fl = Mathf.Sin(phase) * appliedLegAngle;
            float fr = Mathf.Sin(phase + Mathf.PI) * appliedLegAngle;
            float bl = Mathf.Sin(phase + Mathf.PI) * appliedLegAngle;
            float br = Mathf.Sin(phase) * appliedLegAngle;

            legFL.localRotation = Quaternion.Euler(fl, 0f, 0f);
            legFR.localRotation = Quaternion.Euler(fr, 0f, 0f);
            legBL.localRotation = Quaternion.Euler(bl, 0f, 0f);
            legBR.localRotation = Quaternion.Euler(br, 0f, 0f);

            float bob = Mathf.Abs(Mathf.Sin(phase * 1.2f)) * appliedBodyBob;
            body.localPosition = bodyBaseLocalPos + new Vector3(0f, bob, 0f);

            float headSwing = Mathf.Sin(phase) * appliedHeadSwing;
            headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(headSwing, 0f, 0f);

            float tailSwing = Mathf.Sin(phase + 0.8f) * appliedTailSwing;
            tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwing, 0f);
        }

        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpenAngle * 0.12f, 0f, 0f);
    }

    // Computes chase gait phase.
    // Can be time-driven or displacement-driven (distance traveled in world space).
    private float ResolveChasePhase(float t)
    {
        if (!driveChasePhaseFromDisplacement || !Application.isPlaying)
            return t * walkCycleSpeed;

        EnsureChaseCalibration();

        Vector3 currentPosition = transform.position;
        if (!chaseHasLastWorldPosition)
        {
            chaseLastWorldPosition = currentPosition;
            chaseHasLastWorldPosition = true;
            return chaseGaitPhase;
        }

        Vector3 delta = currentPosition - chaseLastWorldPosition;
        chaseLastWorldPosition = currentPosition;
        delta.y = 0f;
        float distance = delta.magnitude;

        float minDistance = chaseMinDisplacement;
        if (distance <= minDistance)
            return chaseGaitPhase;

        float maxDistance = chaseMaxDisplacementPerFrame;
        if (distance > maxDistance)
            return chaseGaitPhase;

        float phaseAdvance = (distance / chaseCycleDistance) * Mathf.PI * 2f;
        chaseGaitPhase = Mathf.Repeat(chaseGaitPhase + phaseAdvance, Mathf.PI * 2f);
        return chaseGaitPhase;
    }

    private void EnsureChaseCalibration()
    {
        if (float.IsNaN(chaseCachedLegAngle) || float.IsInfinity(chaseCachedLegAngle) ||
            Mathf.Abs(chaseCachedLegAngle - legAngle) > 0.001f ||
            chaseCycleDistance <= 0.001f)
        {
            RecomputeChaseCalibration();
        }
    }

    private void RecomputeChaseCalibration()
    {
        chaseLegLengthEstimate = EstimateAverageLegLength();
        float stepDistance = 2f * chaseLegLengthEstimate * Mathf.Sin(legAngle * Mathf.Deg2Rad);
        chaseCycleDistance = stepDistance * 2f;
        chaseCachedLegAngle = legAngle;
    }

    private float EstimateAverageLegLength()
    {
        float total = 0f;
        int count = 0;

        TryAccumulateLegLength(legFL, ref total, ref count);
        TryAccumulateLegLength(legFR, ref total, ref count);
        TryAccumulateLegLength(legBL, ref total, ref count);
        TryAccumulateLegLength(legBR, ref total, ref count);

        if (count <= 0)
            return 0.2f;

        return total / count;
    }

    private static void TryAccumulateLegLength(Transform legRoot, ref float totalLength, ref int count)
    {
        if (legRoot == null)
            return;

        Transform upper = legRoot.Find("Upper");
        Transform ankle = legRoot.Find("Ankle");
        Transform foot = legRoot.Find("Foot");
        if (upper == null || ankle == null || foot == null)
            return;

        float segmentA = Vector3.Distance(upper.position, ankle.position);
        float segmentB = Vector3.Distance(ankle.position, foot.position);
        float candidate = segmentA + segmentB;
        if (candidate <= 0.001f)
            return;

        totalLength += candidate;
        count++;
    }

    private void ResetChaseDisplacementSampling()
    {
        chaseHasLastWorldPosition = false;
        chaseLastWorldPosition = transform.position;
    }

    private void AnimateEatAttackPose()
    {
        // Keep body anchored: jump motion is handled by controller movement.
        body.localPosition = bodyBaseLocalPos;
        body.localScale = bodyBaseLocalScale;

        // Unity local X rotation is positive when pitching down.
        // Negate here so inspector convention stays intuitive: + = up, - = down.
        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(-eatAttackGlobalPitch, 0f, 0f);

        Quaternion foldedLeg = Quaternion.Euler(eatLegFoldAngle, 0f, 0f);
        legFL.localRotation = foldedLeg;
        legFR.localRotation = foldedLeg;
        legBL.localRotation = foldedLeg;
        legBR.localRotation = foldedLeg;

        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(6f, 0f, 0f);
        float mouthPulse = 0.65f + 0.35f * Mathf.Sin(stateTimer * 14f);
        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpenAngle * mouthPulse, 0f, 0f);
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, -tailSwingAngle * 0.4f, 0f);
    }

    private void AnimateEatRecoveryPose()
    {
        body.localPosition = bodyBaseLocalPos;
        body.localScale = bodyBaseLocalScale;

        // Same convention as EAT_ATTACK: + = up, - = down from inspector values.
        float blendDuration = eatRecoveryPitchBlendDuration;
        float blendT;
        if (blendDuration <= 0f)
        {
            blendT = 1f;
        }
        else
        {
            blendT = stateTimer / blendDuration;
            if (blendT < 0f)
                blendT = 0f;
            else if (blendT > 1f)
                blendT = 1f;
        }
        float blendedPitch = Mathf.Lerp(eatAttackGlobalPitch, eatRecoveryGlobalPitch, blendT);
        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(-blendedPitch, 0f, 0f);

        Quaternion landingLegPose = Quaternion.Euler(-eatRecoveryLegForwardAngle, 0f, 0f);
        legFL.localRotation = landingLegPose;
        legFR.localRotation = landingLegPose;
        legBL.localRotation = landingLegPose;
        legBR.localRotation = landingLegPose;

        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(-6f, 0f, 0f);
        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpenAngle * 0.3f, 0f, 0f);
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwingAngle * 0.2f, 0f);
    }

    private void AnimateWaitPlayer(float t)
    {
        float bob = Mathf.Abs(Mathf.Sin(t * waitBodyBobSpeed)) * waitBodyBobAmount;
        body.localPosition = bodyBaseLocalPos + new Vector3(0f, bob, 0f);

        float yaw = Mathf.Sin(t * waitHeadYawSpeed) * waitHeadYawAngle;
        float nod = Mathf.Sin(t * (waitHeadYawSpeed * 0.6f + 0.35f)) * (waitHeadYawAngle * 0.25f);
        headPivot.localRotation = headBaseLocalRot * Quaternion.Euler(nod, yaw, 0f);

        float stomp = Mathf.Sin(t * waitLegStompSpeed) * waitLegStompAngle;
        legFL.localRotation = Quaternion.Euler(stomp, 0f, 0f);
        legFR.localRotation = Quaternion.Euler(-stomp, 0f, 0f);
        legBL.localRotation = Quaternion.Euler(-stomp * 0.6f, 0f, 0f);
        legBR.localRotation = Quaternion.Euler(stomp * 0.6f, 0f, 0f);

        float tailSwing = Mathf.Sin(t * (waitHeadYawSpeed + 0.55f)) * waitTailSwingAngle;
        tail.localRotation = tailBaseLocalRot * Quaternion.Euler(0f, tailSwing, 0f);

        float mouthPulse = 0.5f + 0.5f * Mathf.Sin(t * 3.8f);
        float mouthOpen = waitMouthOpenBase + mouthPulse * (mouthOpenAngle * 0.18f);
        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpen, 0f, 0f);
    }

    private void AnimateStunned(float t)
    {
        float shake = Mathf.Sin(t * stunnedShakeSpeed) * stunnedShakeAngle;
        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(0f, 0f, shake);

        jawPivot.localRotation = jawBaseLocalRot * Quaternion.Euler(mouthOpenAngle * 0.08f, 0f, 0f);
    }

    private void UpdateEyes(float t)
    {
        if (IsEyesTrackingState(state))
        {
            float lookPhase = Mathf.Sin(t * eyeLookSpeed);

            float eyeRadius = 0.5f;
            float appliedPupilScale = pupilBaseScale.sqrMagnitude > 0.000001f ? pupilBaseScale.x : pupilScale;
            float pupilRadius = appliedPupilScale * 0.5f;
            float maxTravelOnSclera = (eyeRadius - pupilRadius) * 0.95f;
            float minTravel = pupilLookSideOffset;
            float angleFactor = Mathf.Sin(eyeLookSideAngle * Mathf.Deg2Rad);
            float sideAmplitude = Mathf.Lerp(minTravel, maxTravelOnSclera, angleFactor);
            float pupilSide = lookPhase * sideAmplitude;
            Vector3 pupilPos = pupilBaseLocalPos + new Vector3(pupilSide, 0f, 0f);
            SetPupilLocalPositionImmediate(pupilPos);
        }
        else
        {
            SetPupilLocalPositionImmediate(pupilBaseLocalPos);
        }
    }

    private static bool IsChaseLikeState(CreatureVisualState visualState)
    {
        return visualState == CreatureVisualState.Follow ||
               visualState == CreatureVisualState.Overtake ||
               visualState == CreatureVisualState.Hunt ||
               visualState == CreatureVisualState.Recenter ||
               visualState == CreatureVisualState.LeashReturn;
    }

    private static bool IsEyesTrackingState(CreatureVisualState visualState)
    {
        return IsChaseLikeState(visualState) || visualState == CreatureVisualState.WaitPlayer;
    }

    private void SetEyeScaleImmediate(Vector3 scale)
    {
        if (eyeL != null)
            eyeL.localScale = scale;

        if (eyeR != null)
            eyeR.localScale = scale;
    }

    private void SetEyeLocalPositionsImmediate(Vector3 leftLocalPos, Vector3 rightLocalPos)
    {
        if (eyeL != null)
            eyeL.localPosition = leftLocalPos;

        if (eyeR != null)
            eyeR.localPosition = rightLocalPos;
    }

    private void SetPupilScaleImmediate(Vector3 scale)
    {
        if (pupilL != null)
            pupilL.localScale = scale;

        if (pupilR != null)
            pupilR.localScale = scale;
    }

    private void SetPupilLocalPositionImmediate(Vector3 localPos)
    {
        if (pupilL != null)
            pupilL.localPosition = localPos;

        if (pupilR != null)
            pupilR.localPosition = localPos;
    }

    // Live-apply structural parameters without rebuilding:
    // overall scale, body/head proportions, mouth, legs, ear placement and tail geometry.
    private void ApplyStructurePlacement()
    {
        if (root != null)
            root.localScale = Vector3.one * overallScale;

        if (body != null)
        {
            bodyBaseLocalPos = new Vector3(0f, 0.95f, 0f);
            bodyBaseLocalScale = new Vector3(2.8f, 1.5f * bodyHeightFactor, 3.5f);
            body.localPosition = bodyBaseLocalPos;
            body.localScale = bodyBaseLocalScale;
        }

        if (headPivot != null)
            headPivot.localPosition = new Vector3(0f, 1.02f, 2.05f);

        if (head != null)
        {
            float headLinearScale = Mathf.Pow(headVolumeFactor, 1f / 3f);
            head.localPosition = Vector3.zero;
            head.localScale = new Vector3(2.0f, 1.15f, 1.7f) * headLinearScale;
        }

        ApplyMouthStructurePlacement();
        ApplyLegStructurePlacement();
        ApplyTailPlacement();
        ApplyEarPlacement();
    }

    // Keep collisions deterministic:
    // - remove physical colliders from generated visual parts
    // - keep a single trigger sphere on Hippo root, auto-fitted to structure
    private void ConfigureCollisionSetup()
    {
        if (removeGeneratedPartColliders && !generatedPartCollidersPurged && root != null)
        {
            RemoveCollidersInHierarchy(root, includeRoot: false);
            generatedPartCollidersPurged = true;
        }
        else if (!removeGeneratedPartColliders)
        {
            generatedPartCollidersPurged = false;
        }

        if (!autoStructureTriggerCollider)
        {
            if (structureTriggerCollider != null)
                RemoveCollider(structureTriggerCollider);

            structureTriggerCollider = null;
            rootColliderCleanupDone = false;
            return;
        }

        EnsureStructureTriggerCollider();
        if (!rootColliderCleanupDone)
        {
            RemoveExtraRootColliders();
            rootColliderCleanupDone = true;
        }
    }

    private void EnsureStructureTriggerCollider()
    {
        if (structureTriggerCollider == null)
            structureTriggerCollider = GetComponent<SphereCollider>();
        if (structureTriggerCollider == null)
            structureTriggerCollider = gameObject.AddComponent<SphereCollider>();

        structureTriggerCollider.isTrigger = true;
        structureTriggerCollider.enabled = true;

        if (!TryComputeStructureBounds(out Vector3 boundsCenter, out Vector3 boundsExtents))
            return;

        Vector3 center = boundsCenter + triggerCenterOffset;
        float baseRadius = Mathf.Max(boundsExtents.x, Mathf.Max(boundsExtents.y, boundsExtents.z));
        float radius = baseRadius * triggerRadiusScale + triggerRadiusPadding;
        if (radius < 0.01f)
            radius = 0.01f;

        structureTriggerCollider.center = center;
        structureTriggerCollider.radius = radius;
    }

    private void RemoveExtraRootColliders()
    {
        Collider[] rootColliders = GetComponents<Collider>();
        for (int i = 0; i < rootColliders.Length; i++)
        {
            Collider collider = rootColliders[i];
            if (collider == null)
                continue;
            if (collider == structureTriggerCollider)
                continue;
            if (collider is CharacterController)
                continue;

            RemoveCollider(collider);
        }
    }

    private bool TryComputeStructureBounds(out Vector3 center, out Vector3 extents)
    {
        center = Vector3.zero;
        extents = Vector3.zero;

        float headLinearScale = Mathf.Pow(headVolumeFactor, 1f / 3f);
        ResolveJawDimensions(
            out float upperJawWidth,
            out float lowerJawWidth,
            out float upperJawHeight,
            out float lowerJawHeight,
            out float upperJawLength,
            out float lowerJawLength);
        ResolveJawOffsets(
            out float upperJawHeightOffset,
            out float lowerJawHeightOffset,
            out float upperJawForwardOffset,
            out float lowerJawForwardOffset);

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        EncapsulateAabb(ref min, ref max, new Vector3(0f, 0.95f, 0f), new Vector3(2.8f, 1.5f * bodyHeightFactor, 3.5f));
        EncapsulateAabb(ref min, ref max, new Vector3(0f, 1.02f, 2.05f), new Vector3(2.0f, 1.15f, 1.7f) * headLinearScale);

        Vector3 headPivotPos = new Vector3(0f, 1.02f, 2.05f);
        EncapsulateAabb(ref min, ref max, headPivotPos + new Vector3(0f, upperJawHeightOffset, upperJawForwardOffset), new Vector3(upperJawWidth, upperJawHeight, upperJawLength));
        Vector3 jawPivotPos = headPivotPos + new Vector3(0f, offsetPivotHeightJaw, offsetPivotForwardJaw);
        EncapsulateAabb(ref min, ref max, jawPivotPos + new Vector3(0f, lowerJawHeightOffset, lowerJawForwardOffset), new Vector3(lowerJawWidth, lowerJawHeight, lowerJawLength));

        Vector3 baseEarPosL = new Vector3(-0.56f, 0.30f, -0.08f);
        Vector3 baseEarPosR = new Vector3(0.56f, 0.30f, -0.08f);
        Vector3 earPosL = new Vector3(baseEarPosL.x - earPlacementOffset.x, baseEarPosL.y + earPlacementOffset.y, baseEarPosL.z + earPlacementOffset.z);
        Vector3 earPosR = new Vector3(baseEarPosR.x + earPlacementOffset.x, baseEarPosR.y + earPlacementOffset.y, baseEarPosR.z + earPlacementOffset.z);
        Vector3 earSize = new Vector3(0.20f * earWidthFactor, 0.28f, 0.12f);
        EncapsulateAabb(ref min, ref max, headPivotPos + earPosL, earSize);
        EncapsulateAabb(ref min, ref max, headPivotPos + earPosR, earSize);

        EncapsulateAabb(ref min, ref max, new Vector3(tailSideOffset, tailHeightOffset, tailForwardOffset), GetTailEllipsoidScale());

        float upperCenterY = upperLegLength * UpperLegCenterYRatio;
        float ankleCenterY = upperLegLength * AnkleCenterYRatio;
        float footCenterY = upperLegLength * FootCenterYRatio;
        float ankleForward = footWidth * AnkleForwardFromFootWidthRatio;
        float footForward = footWidth * FootForwardFromFootWidthRatio;
        float footDepth = footWidth * FootDepthFromFootWidthRatio;

        Vector3 upperSize = new Vector3(upperLegThickness, upperLegLength, upperLegThickness);
        Vector3 ankleSize = new Vector3(ankleDiameter, ankleDiameter, ankleDiameter);
        Vector3 footSize = new Vector3(footWidth, footHeight, footDepth);
        EncapsulateLeg(ref min, ref max, new Vector3(-LegSideOffset, legAttachHeight, LegFrontOffset), upperCenterY, ankleCenterY, footCenterY, ankleForward, footForward, upperSize, ankleSize, footSize);
        EncapsulateLeg(ref min, ref max, new Vector3(LegSideOffset, legAttachHeight, LegFrontOffset), upperCenterY, ankleCenterY, footCenterY, ankleForward, footForward, upperSize, ankleSize, footSize);
        EncapsulateLeg(ref min, ref max, new Vector3(-LegSideOffset, legAttachHeight, -LegFrontOffset), upperCenterY, ankleCenterY, footCenterY, ankleForward, footForward, upperSize, ankleSize, footSize);
        EncapsulateLeg(ref min, ref max, new Vector3(LegSideOffset, legAttachHeight, -LegFrontOffset), upperCenterY, ankleCenterY, footCenterY, ankleForward, footForward, upperSize, ankleSize, footSize);

        if (float.IsPositiveInfinity(min.x) || float.IsNegativeInfinity(max.x))
            return false;

        min *= overallScale;
        max *= overallScale;
        center = (min + max) * 0.5f;
        extents = (max - min) * 0.5f;
        return true;
    }

    private static void EncapsulateAabb(ref Vector3 min, ref Vector3 max, Vector3 center, Vector3 size)
    {
        Vector3 half = size * 0.5f;
        Vector3 localMin = center - half;
        Vector3 localMax = center + half;
        min = Vector3.Min(min, localMin);
        max = Vector3.Max(max, localMax);
    }

    private static void EncapsulateLeg(
        ref Vector3 min,
        ref Vector3 max,
        Vector3 legRootPosition,
        float upperCenterY,
        float ankleCenterY,
        float footCenterY,
        float ankleForward,
        float footForward,
        Vector3 upperSize,
        Vector3 ankleSize,
        Vector3 footSize)
    {
        EncapsulateAabb(ref min, ref max, legRootPosition + new Vector3(0f, upperCenterY, 0f), upperSize);
        EncapsulateAabb(ref min, ref max, legRootPosition + new Vector3(0f, ankleCenterY, ankleForward), ankleSize);
        EncapsulateAabb(ref min, ref max, legRootPosition + new Vector3(0f, footCenterY, footForward), footSize);
    }

    // Live-update mouth structure (upper jaw, lower jaw and pivot/rest orientation).
    private void ApplyMouthStructurePlacement()
    {
        ResolveJawDimensions(
            out float upperJawResolvedWidth,
            out float lowerJawResolvedWidth,
            out float upperJawResolvedHeight,
            out float lowerJawResolvedHeight,
            out float upperJawResolvedLength,
            out float lowerJawResolvedLength);
        ResolveJawOffsets(
            out float upperJawResolvedHeightOffset,
            out float lowerJawResolvedHeightOffset,
            out float upperJawResolvedForwardOffset,
            out float lowerJawResolvedForwardOffset);

        if (upperJaw != null)
        {
            upperJaw.localPosition = new Vector3(0f, upperJawResolvedHeightOffset, upperJawResolvedForwardOffset);
            upperJaw.localScale = new Vector3(upperJawResolvedWidth, upperJawResolvedHeight, upperJawResolvedLength);
        }

        if (jawPivot != null)
        {
            jawPivot.localPosition = new Vector3(0f, offsetPivotHeightJaw, offsetPivotForwardJaw);
            jawBaseLocalRot = Quaternion.Euler(basePitchJaw, 0f, 0f);
            jawPivot.localRotation = jawBaseLocalRot;
        }

        if (lowerJaw != null)
        {
            lowerJaw.localPosition = new Vector3(0f, lowerJawResolvedHeightOffset, lowerJawResolvedForwardOffset);
            lowerJaw.localScale = new Vector3(lowerJawResolvedWidth, lowerJawResolvedHeight, lowerJawResolvedLength);
        }
    }

    private void ResolveJawDimensions(
        out float upperWidth,
        out float lowerWidth,
        out float upperHeight,
        out float lowerHeight,
        out float upperLength,
        out float lowerLength)
    {
        SplitUpperLowerByRatio(widthJaw, ratioWidthUpperToLowerJaw, out upperWidth, out lowerWidth);
        SplitUpperLowerByRatio(heightJaw, ratioHeightUpperToLowerJaw, out upperHeight, out lowerHeight);
        SplitUpperLowerByRatio(lengthJaw, ratioLengthUpperToLowerJaw, out upperLength, out lowerLength);
    }

    private void ResolveJawOffsets(
        out float upperHeightOffset,
        out float lowerHeightOffset,
        out float upperForwardOffset,
        out float lowerForwardOffset)
    {
        SplitUpperLowerByRatio(offsetHeightJaw, ratioOffsetHeightUpperToLowerJaw, out upperHeightOffset, out lowerHeightOffset);
        SplitUpperLowerByRatio(offsetForwardJaw, ratioOffsetForwardUpperToLowerJaw, out upperForwardOffset, out lowerForwardOffset);
    }

    private static void SplitUpperLowerByRatio(
        float combined,
        float upperToLowerRatio,
        out float upper,
        out float lower)
    {
        lower = combined / (1f + upperToLowerRatio);
        upper = combined - lower;
    }

    // Live-update leg structure (attachment height and upper/ankle/foot dimensions).
    private void ApplyLegStructurePlacement()
    {
        ApplyLegRootPlacement(legFL, -LegSideOffset, LegFrontOffset);
        ApplyLegRootPlacement(legFR, LegSideOffset, LegFrontOffset);
        ApplyLegRootPlacement(legBL, -LegSideOffset, -LegFrontOffset);
        ApplyLegRootPlacement(legBR, LegSideOffset, -LegFrontOffset);

        ApplyLegPartPlacement(legFL);
        ApplyLegPartPlacement(legFR);
        ApplyLegPartPlacement(legBL);
        ApplyLegPartPlacement(legBR);

        if (driveChasePhaseFromDisplacement)
            RecomputeChaseCalibration();
    }

    private void ApplyLegRootPlacement(Transform legRoot, float side, float forward)
    {
        if (legRoot == null)
            return;

        legRoot.localPosition = new Vector3(side, legAttachHeight, forward);
    }

    private void ApplyLegPartPlacement(Transform legRoot)
    {
        if (legRoot == null)
            return;

        Transform upper = legRoot.Find("Upper");
        Transform ankle = legRoot.Find("Ankle");
        Transform foot = legRoot.Find("Foot");

        float upperCenterY = upperLegLength * UpperLegCenterYRatio;
        float ankleCenterY = upperLegLength * AnkleCenterYRatio;
        float footCenterY = upperLegLength * FootCenterYRatio;
        float ankleForward = footWidth * AnkleForwardFromFootWidthRatio;
        float footForward = footWidth * FootForwardFromFootWidthRatio;
        float footDepth = footWidth * FootDepthFromFootWidthRatio;

        if (upper != null)
        {
            upper.localPosition = new Vector3(0f, upperCenterY, 0f);
            upper.localScale = new Vector3(upperLegThickness, upperLegLength, upperLegThickness);
        }

        if (ankle != null)
        {
            ankle.localPosition = new Vector3(0f, ankleCenterY, ankleForward);
            ankle.localScale = new Vector3(ankleDiameter, ankleDiameter, ankleDiameter);
        }

        if (foot != null)
        {
            foot.localPosition = new Vector3(0f, footCenterY, footForward);
            foot.localScale = new Vector3(footWidth, footHeight, footDepth);
        }
    }

    // Live-update tail transform from inspector parameters.
    private void ApplyTailPlacement()
    {
        if (tail == null)
            return;

        tail.localPosition = new Vector3(tailSideOffset, tailHeightOffset, tailForwardOffset);
        tailBaseLocalRot = Quaternion.Euler(tailPitchDegrees, 0f, 0f);
        tail.localRotation = tailBaseLocalRot;
        tail.localScale = GetTailEllipsoidScale();
    }

    private Vector3 GetTailEllipsoidScale()
    {
        // Sphere primitive has unit diameter; convert half-dimensions to full local scale.
        return new Vector3(tailRadiusX * 2f, tailLength * 2f, tailRadiusZ * 2f);
    }

    // Live-update ear transforms from inspector parameters.
    private void ApplyEarPlacement()
    {
        if (headPivot != null)
        {
            if (earL == null)
                earL = headPivot.Find("Ear_L");
            if (earR == null)
                earR = headPivot.Find("Ear_R");
        }

        Vector3 baseEarPosL = new Vector3(-0.56f, 0.30f, -0.08f);
        Vector3 baseEarPosR = new Vector3(0.56f, 0.30f, -0.08f);
        Vector3 earPosL = new Vector3(baseEarPosL.x - earPlacementOffset.x, baseEarPosL.y + earPlacementOffset.y, baseEarPosL.z + earPlacementOffset.z);
        Vector3 earPosR = new Vector3(baseEarPosR.x + earPlacementOffset.x, baseEarPosR.y + earPlacementOffset.y, baseEarPosR.z + earPlacementOffset.z);
        Vector3 earScale = new Vector3(0.20f * earWidthFactor, 0.28f, 0.12f);

        if (earL != null)
        {
            earL.localPosition = earPosL;
            earL.localRotation = Quaternion.Euler(0f, 0f, 18f);
            earL.localScale = earScale;
        }

        if (earR != null)
        {
            earR.localPosition = earPosR;
            earR.localRotation = Quaternion.Euler(0f, 0f, -18f);
            earR.localScale = earScale;
        }
    }

    // Live-update eyes and pupils from inspector parameters.
    // This is intentionally separate from full rig rebuild for fast iteration.
    private void ApplyEyePlacement()
    {
        float side = Mathf.Abs(eyePivotSideOffset);
        float height = eyePivotHeightOffset;
        float forward = eyePivotForwardOffset;

        eyeLBaseLocalPos = new Vector3(-side, height, forward);
        eyeRBaseLocalPos = new Vector3(side, height, forward);
        SetEyeLocalPositionsImmediate(eyeLBaseLocalPos, eyeRBaseLocalPos);

        eyeBaseScale = new Vector3(eyeScale, eyeScale, eyeScale);
        SetEyeScaleImmediate(eyeBaseScale);

        // Pupil values are expressed in eye local space (normalized to a unit sphere).
        pupilBaseScale = new Vector3(pupilScale, pupilScale, pupilScale);
        SetPupilScaleImmediate(pupilBaseScale);

        pupilBaseLocalPos = new Vector3(0f, 0f, pupilForwardOffset);
        SetPupilLocalPositionImmediate(pupilBaseLocalPos);
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
            new Vector3(0f, upperLegLength * UpperLegCenterYRatio, 0f),
            Vector3.zero,
            new Vector3(upperLegThickness, upperLegLength, upperLegThickness),
            CreatureMaterialSlot.Body);

        Transform ankle = CreatePart(
            "Ankle",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, upperLegLength * AnkleCenterYRatio, footWidth * AnkleForwardFromFootWidthRatio),
            Vector3.zero,
            new Vector3(ankleDiameter, ankleDiameter, ankleDiameter),
            CreatureMaterialSlot.Body);

        Transform foot = CreatePart(
            "Foot",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, upperLegLength * FootCenterYRatio, footWidth * FootForwardFromFootWidthRatio),
            Vector3.zero,
            new Vector3(footWidth, footHeight, footWidth * FootDepthFromFootWidthRatio),
            CreatureMaterialSlot.Body);

        RemoveCollider(upper.gameObject);
        RemoveCollider(ankle.gameObject);
        RemoveCollider(foot.gameObject);

        return legRoot;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, 0.25f);
    }
}
