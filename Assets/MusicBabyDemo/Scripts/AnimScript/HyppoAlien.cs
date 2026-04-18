using UnityEngine;

[ExecuteAlways]
public class HippoVisual : CreatureVisualBase
{
    [Header("Build")]
    [Min(0.01f)] public float overallScale = 1f;

    [Header("Materials")]
    public Material bodyMaterial;
    public Material scleraMaterial;
    public Material eyeMaterial;
    public Material pupilMaterial;
    public Material mouthMaterial;

    public Color fallbackBodyColor = new Color(0.56f, 0.58f, 0.63f);
    public Color fallbackScleraColor = new Color(0.92f, 0.94f, 0.97f);
    public Color fallbackEyeColor = new Color(0.08f, 0.08f, 0.08f);
    public Color fallbackMouthColor = new Color(0.55f, 0.22f, 0.24f);

    [Header("Idle")]
    public float idleBreathSpeed = 1.6f;
    public float idleBreathAmount = 0.03f;
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

    [Header("Chase Displacement Coupling")]
    [Tooltip("When enabled, chase gait phase advances from real horizontal displacement instead of Time.time.")]
    public bool driveChasePhaseFromDisplacement = true;
    [Tooltip("Ignore tiny displacements to avoid micro-jitter in gait progression.")]
    public float chaseMinDisplacement = 0.0005f;
    [Tooltip("Ignore a single-frame displacement above this threshold (teleport/recovery safety).")]
    public float chaseMaxDisplacementPerFrame = 1.25f;
 
    [Header("Eyes")]
    public float eyeLookSideAngle = 35f;
    public float eyeLookSpeed = 2.2f;
    [Tooltip("Horizontal offset of eye pivots from head center.")]
    public float eyePivotSideOffset = 0.45f;
    [Tooltip("Vertical offset of eye pivots from head center.")]
    public float eyePivotHeightOffset = 0.24f;
    [Tooltip("Forward offset of eye pivots from head center. Increase to pull eyes out of the head.")]
    public float eyePivotForwardOffset = 0.62f;
    [Tooltip("Uniform eye sphere scale.")]
    public float eyeScale = 0.22f;
    [Tooltip("Forward offset of each eyeball from its pivot (socket center).")]
    public float eyeBallForwardOffset = 0.08f;
    [Tooltip("Pupil size ratio relative to eye size (0.5 = half-eye diameter).")]
    public float pupilScale = 0.48f;
    [Tooltip("Pupil center forward offset in eye local space (0=center, 0.5=eye surface).")]
    public float pupilForwardOffset = 0.27f;
    [Tooltip("Horizontal pupil travel in eye local space when looking left/right.")]
    public float pupilLookSideOffset = 0.08f;

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
    private Transform pupilL;
    private Transform pupilR;

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
    private Vector3 pupilBaseScale;
    private Vector3 pupilBaseLocalPos;

    private Material fallbackBodyMat;
    private Material fallbackScleraMat;
    private Material fallbackPupilMat;
    private Material fallbackMouthMat;
    private float chaseGaitPhase;
    private Vector3 chaseLastWorldPosition;
    private bool chaseHasLastWorldPosition;
    private float chaseLegLengthEstimate = 0.8f;
    private float chaseCycleDistance = 1f;
    private float chaseCachedLegAngle = float.NaN;

    private enum MaterialSlot
    {
        Body,
        Eye,
        EyeSclera,
        EyePupil,
        Mouth
    }

    public override void SetControllerState(MusicRun.CreatureState controllerState)
    {
        SetState(MapControllerState(controllerState));
    }

    [ContextMenu("Rebuild Visual")]
    public void Rebuild()
    {
        RebuildVisual();
    }

    [ContextMenu("Apply All Preset")]
    public void ApplyAllPreset()
    {
        eyeLookSideAngle = 35f;
        eyeLookSpeed = 2.2f;
        eyePivotSideOffset = 0.44f;
        eyePivotHeightOffset = 0.25f;
        eyePivotForwardOffset = 0.64f;
        eyeScale = 0.22f;
        eyeBallForwardOffset = 0.08f;
        pupilScale = 0.48f;
        pupilForwardOffset = 0.27f;
        pupilLookSideOffset = 0.08f;

        BuildIfNeeded(false);
        ApplyEyePlacement();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            if (root != null)
                UnityEditor.EditorUtility.SetDirty(root.gameObject);
        }
#endif
    }

    protected override void OnStateChanged(CreatureVisualState oldState, CreatureVisualState newState)
    {
        base.OnStateChanged(oldState, newState);

        if (newState == CreatureVisualState.Chase || oldState == CreatureVisualState.Chase)
            ResetChaseDisplacementSampling();
    }

    protected override void BuildIfNeeded(bool force)
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

                ApplyEyePlacement();
                return;
            }

            if (root != null)
            {
                if (materialsDirty)
                {
                    ApplyCurrentMaterialsToRig();
                    materialsDirty = false;
                }
                return;
            }
        }

        ClearExisting();
        BuildVisual();
        RecoverReferences();
        CacheBases();
        ApplyCurrentMaterialsToRig();
        materialsDirty = false;
        ApplyEyePlacement();
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
            ApplyMaterial(body.gameObject, MaterialSlot.Body);

        if (head != null)
            ApplyMaterial(head.gameObject, MaterialSlot.Body);

        if (tail != null)
            ApplyMaterial(tail.gameObject, MaterialSlot.Body);

        if (upperJaw != null)
            ApplyMaterial(upperJaw.gameObject, MaterialSlot.Mouth);

        if (lowerJaw != null)
            ApplyMaterial(lowerJaw.gameObject, MaterialSlot.Mouth);

        if (eyeL != null)
            ApplyMaterial(eyeL.gameObject, MaterialSlot.EyeSclera);

        if (eyeR != null)
            ApplyMaterial(eyeR.gameObject, MaterialSlot.EyeSclera);

        if (pupilL != null)
            ApplyMaterial(pupilL.gameObject, MaterialSlot.EyePupil);

        if (pupilR != null)
            ApplyMaterial(pupilR.gameObject, MaterialSlot.EyePupil);

        if (headPivot != null)
        {
            Transform earL = headPivot.Find("Ear_L");
            if (earL != null)
                ApplyMaterial(earL.gameObject, MaterialSlot.Body);

            Transform earR = headPivot.Find("Ear_R");
            if (earR != null)
                ApplyMaterial(earR.gameObject, MaterialSlot.Body);
        }

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
            ApplyMaterial(upper.gameObject, MaterialSlot.Body);

        Transform ankle = legRoot.Find("Ankle");
        if (ankle != null)
            ApplyMaterial(ankle.gameObject, MaterialSlot.Body);

        Transform foot = legRoot.Find("Foot");
        if (foot != null)
            ApplyMaterial(foot.gameObject, MaterialSlot.Body);
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

            if (eyeL != null)
                pupilL = eyeL.Find("Pupil_L");

            if (eyeR != null)
                pupilR = eyeR.Find("Pupil_R");
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
        SetEyeLocalPositionImmediate(eyeBaseLocalPos);
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
        tail = null;
        legFL = null;
        legFR = null;
        legBL = null;
        legBR = null;
        eyePivotL = null;
        eyePivotR = null;
        eyeL = null;
        eyeR = null;
        pupilL = null;
        pupilR = null;
        chaseHasLastWorldPosition = false;
        chaseGaitPhase = 0f;
        chaseLegLengthEstimate = 0.8f;
        chaseCycleDistance = 1f;
        chaseCachedLegAngle = float.NaN;
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
            MaterialSlot.EyeSclera);

        eyeR = CreatePart(
            "Eye_R",
            PrimitiveType.Sphere,
            eyePivotR,
            new Vector3(0f, 0f, eyeBallForwardOffset),
            Vector3.zero,
            new Vector3(eyeScale, eyeScale, eyeScale),
            MaterialSlot.EyeSclera);

        pupilL = CreatePart(
            "Pupil_L",
            PrimitiveType.Sphere,
            eyeL,
            new Vector3(0f, 0f, pupilForwardOffset),
            Vector3.zero,
            new Vector3(pupilScale, pupilScale, pupilScale),
            MaterialSlot.EyePupil);

        pupilR = CreatePart(
            "Pupil_R",
            PrimitiveType.Sphere,
            eyeR,
            new Vector3(0f, 0f, pupilForwardOffset),
            Vector3.zero,
            new Vector3(pupilScale, pupilScale, pupilScale),
            MaterialSlot.EyePupil);

        RemoveCollider(pupilL.gameObject);
        RemoveCollider(pupilR.gameObject);

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

        legFL = CreateLeg("Leg_FL", new Vector3(-0.76f, 0.58f, 1.0f));
        legFR = CreateLeg("Leg_FR", new Vector3(0.76f, 0.58f, 1.0f));
        legBL = CreateLeg("Leg_BL", new Vector3(-0.76f, 0.58f, -1.0f));
        legBR = CreateLeg("Leg_BR", new Vector3(0.76f, 0.58f, -1.0f));
    }

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

            case CreatureVisualState.Chase:
                AnimateChase(t);
                break;

            case CreatureVisualState.Eat:
                AnimateEat();
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

        float minDistance = Mathf.Max(0f, chaseMinDisplacement);
        if (distance <= minDistance)
            return chaseGaitPhase;

        float maxDistance = Mathf.Max(minDistance + 0.001f, chaseMaxDisplacementPerFrame);
        if (distance > maxDistance)
            return chaseGaitPhase;

        float cycleDistance = Mathf.Max(0.01f, chaseCycleDistance);
        float phaseAdvance = (distance / cycleDistance) * Mathf.PI * 2f;
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
        float clampedLegAngle = Mathf.Clamp(legAngle, 0.01f, 85f);
        float stepDistance = 2f * chaseLegLengthEstimate * Mathf.Sin(clampedLegAngle * Mathf.Deg2Rad);
        chaseCycleDistance = Mathf.Max(0.01f, stepDistance * 2f);
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
            return Mathf.Max(0.2f, overallScale * 0.8f);

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
        if (state == CreatureVisualState.Chase)
        {
            float lookPhase = Mathf.Sin(t * eyeLookSpeed);

            float clampedLookAngle = Mathf.Clamp(eyeLookSideAngle, 0f, 75f);
            float eyeRadius = 0.5f;
            float appliedPupilScale = pupilBaseScale.sqrMagnitude > 0.000001f ? pupilBaseScale.x : Mathf.Clamp(pupilScale, 0.05f, 0.85f);
            float pupilRadius = Mathf.Max(0.005f, appliedPupilScale * 0.5f);
            float maxTravelOnSclera = Mathf.Max(0f, (eyeRadius - pupilRadius) * 0.95f);
            float minTravel = Mathf.Clamp(pupilLookSideOffset, 0f, maxTravelOnSclera);
            float angleFactor = Mathf.Sin(clampedLookAngle * Mathf.Deg2Rad);
            float sideAmplitude = Mathf.Lerp(minTravel, maxTravelOnSclera, angleFactor);
            float pupilSide = lookPhase * sideAmplitude;
            Vector3 pupilPos = pupilBaseLocalPos + new Vector3(pupilSide, 0f, 0f);
            SetPupilLocalPositionImmediate(pupilPos);

            // Keep pivots neutral: visible eye motion comes from pupil translation.
            eyePivotL.localRotation = eyePivotLBaseLocalRot;
            eyePivotR.localRotation = eyePivotRBaseLocalRot;
        }
        else
        {
            eyePivotL.localRotation = eyePivotLBaseLocalRot;
            eyePivotR.localRotation = eyePivotRBaseLocalRot;
            SetPupilLocalPositionImmediate(pupilBaseLocalPos);
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

        // Pupil values are expressed in eye local space (normalized to a unit sphere).
        float uniformPupilScale = Mathf.Clamp(pupilScale, 0.05f, 0.85f);
        pupilBaseScale = new Vector3(uniformPupilScale, uniformPupilScale, uniformPupilScale);
        SetPupilScaleImmediate(pupilBaseScale);

        float pupilRadius = uniformPupilScale * 0.5f;
        float maxPupilForward = Mathf.Max(0f, 0.5f - pupilRadius + 0.02f);
        float pupilForward = Mathf.Clamp(pupilForwardOffset, 0f, maxPupilForward);
        pupilBaseLocalPos = new Vector3(0f, 0f, pupilForward);
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
            new Vector3(0f, -0.18f, 0f),
            Vector3.zero,
            new Vector3(0.52f, 1.16f, 0.52f),
            MaterialSlot.Body);

        Transform ankle = CreatePart(
            "Ankle",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, -0.72f, 0.06f),
            Vector3.zero,
            new Vector3(0.42f, 0.36f, 0.42f),
            MaterialSlot.Body);

        Transform foot = CreatePart(
            "Foot",
            PrimitiveType.Sphere,
            legRoot,
            new Vector3(0f, -0.92f, 0.14f),
            Vector3.zero,
            new Vector3(0.72f, 0.34f, 0.84f),
            MaterialSlot.Body);

        RemoveCollider(upper.gameObject);
        RemoveCollider(ankle.gameObject);
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

            case MaterialSlot.EyeSclera:
                assigned = scleraMaterial != null ? scleraMaterial : CreateFallbackMaterial("HippoSclera_Fallback", fallbackScleraColor);
                break;

            case MaterialSlot.EyePupil:
                assigned = pupilMaterial != null
                    ? pupilMaterial
                    : CreateFallbackMaterial("HippoPupil_Fallback", fallbackEyeColor);
                break;

            case MaterialSlot.Eye:
                assigned = eyeMaterial != null
                    ? eyeMaterial
                    : (pupilMaterial != null ? pupilMaterial : CreateFallbackMaterial("HippoEye_Fallback", fallbackEyeColor));
                break;

            case MaterialSlot.Mouth:
                assigned = mouthMaterial != null ? mouthMaterial : CreateFallbackMaterial("HippoMouth_Fallback", fallbackMouthColor);
                break;
        }

        renderer.sharedMaterial = assigned;
    }

    private Material CreateFallbackMaterial(string matName, Color color)
    {
        // Force URP-compatible fallbacks to avoid pink/magenta materials in URP projects.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (shader == null)
        {
            Debug.LogWarning(
                $"[{nameof(HippoVisual)}] URP fallback shaders not found. " +
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
