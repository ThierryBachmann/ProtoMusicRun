using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HippoVisual))]
[CanEditMultipleObjects]
public class HippoVisualEditor : Editor
{
    private const string FoldoutStatePrefix = "HippoVisualEditor.Foldout.";

    private sealed class SectionDefinition
    {
        public readonly string Key;
        public readonly string Title;
        public readonly string HelpText;
        public readonly bool DefaultExpanded;
        public readonly string[] PropertyNames;

        public SectionDefinition(string key, string title, string helpText, bool defaultExpanded, params string[] propertyNames)
        {
            Key = key;
            Title = title;
            HelpText = helpText;
            DefaultExpanded = defaultExpanded;
            PropertyNames = propertyNames;
        }
    }

    private static readonly SectionDefinition[] Sections =
    {
        new SectionDefinition(
            "animation",
            "Animation",
            "Current visual state and edit-mode preview. In Play mode, the controller drives this automatically.",
            true,
            "state",
            "previewInEditMode"),
        new SectionDefinition(
            "structure_global",
            "STRUCTURE / Global",
            "Global scale for the procedural creature.",
            true,
            "overallScale"),
        new SectionDefinition(
            "structure_geometry",
            "STRUCTURE / Geometry",
            "Global geometry detail for generated parts (including Blocky cube mode and VeryLow low-poly spheres).",
            true,
            "sphereDetailLevel"),
        new SectionDefinition(
            "structure_body_head",
            "STRUCTURE / Body & Head",
            "Overall body and head volume proportions.",
            true,
            "bodyHeightFactor",
            "headVolumeFactor"),
        new SectionDefinition(
            "structure_ears",
            "STRUCTURE / Ears",
            "Ear width and placement on the head.",
            false,
            "earWidthFactor",
            "earPlacementOffset"),
        new SectionDefinition(
            "structure_tail",
            "STRUCTURE / Tail",
            "Tail position, orientation, and ellipsoid shape.",
            false,
            "tailSideOffset",
            "tailHeightOffset",
            "tailForwardOffset",
            "tailPitchDegrees",
            "tailRadiusX",
            "tailRadiusZ",
            "tailLength"),
        new SectionDefinition(
            "structure_mouth",
            "STRUCTURE / Mouth",
            "Coupled upper/lower jaw parameters: dimensions, offsets, pivot, and base pitch.",
            false,
            "heightJaw",
            "ratioHeightUpperToLowerJaw",
            "lengthJaw",
            "ratioLengthUpperToLowerJaw",
            "widthJaw",
            "ratioWidthUpperToLowerJaw",
            "offsetHeightJaw",
            "ratioOffsetHeightUpperToLowerJaw",
            "offsetForwardJaw",
            "ratioOffsetForwardUpperToLowerJaw",
            "offsetPivotHeightJaw",
            "offsetPivotForwardJaw",
            "basePitchJaw"),
        new SectionDefinition(
            "structure_legs",
            "STRUCTURE / Legs",
            "Leg geometry: upper, ankle, foot, and body attachment height.",
            false,
            "upperLegThickness",
            "upperLegLength",
            "legAttachHeight",
            "ankleDiameter",
            "footHeight",
            "footWidth"),
        new SectionDefinition(
            "structure_eyes",
            "STRUCTURE / Eyes Geometry",
            "Eye and pupil position/size inside the head.",
            false,
            "eyePivotSideOffset",
            "eyePivotHeightOffset",
            "eyePivotForwardOffset",
            "eyeScale",
            "pupilScale",
            "pupilForwardOffset"),
        new SectionDefinition(
            "structure_collision",
            "STRUCTURE / Collision",
            "Collider policy: remove colliders on generated parts and auto-fit one root trigger sphere.",
            false,
            "removeGeneratedPartColliders",
            "autoStructureTriggerCollider",
            "triggerCenterOffset",
            "triggerRadiusScale",
            "triggerRadiusPadding"),
        new SectionDefinition(
            "structure_attachment",
            "STRUCTURE / Attachment",
            "Visual anchoring constraints relative to parent (useful on slopes).",
            false,
            "lockLocalXZToParent"),
        new SectionDefinition(
            "materials",
            "Materials",
            "Explicit materials by visual slot.",
            false,
            "bodyMaterial",
            "earMaterial",
            "scleraMaterial",
            "pupilMaterial",
            "mouthMaterial",
            "tailMaterial"),
        new SectionDefinition(
            "fallback_colors",
            "Fallback Colors",
            "Colors used when no material is assigned for a slot.",
            false,
            "fallbackBodyColor",
            "fallbackEarColor",
            "fallbackTailColor",
            "fallbackScleraColor",
            "fallbackPupilColor",
            "fallbackMouthColor"),
        new SectionDefinition(
            "coupling",
            "COUPLING / Movement-Driven Gait",
            "Animation/movement coupling: gait phase advances from real traveled distance.",
            false,
            "driveChasePhaseFromDisplacement",
            "chaseMinDisplacement",
            "chaseMaxDisplacementPerFrame"),
        new SectionDefinition(
            "anim_idle",
            "ANIMATION / Idle",
            "Idle breathing, head nod, and tail sway.",
            false,
            "idleBreathSpeed",
            "idleBreathAmount",
            "idleHeadNodAngle",
            "idleHeadNodSpeed",
            "idleTailSwingFactor"),
        new SectionDefinition(
            "anim_chase",
            "ANIMATION / Chase",
            "Locomotion animation for Follow/Overtake/Hunt/Recenter/LeashReturn.",
            false,
            "animateWalk",
            "walkCycleSpeed",
            "legAngle",
            "bodyBobAmount",
            "headSwingAngle",
            "tailSwingAngle",
            "chaseMouthOpenAngle"),
        new SectionDefinition(
            "anim_wait",
            "ANIMATION / Wait Player",
            "Impatient creature behavior: leg stomps, talking mouth, and irritated eyes.",
            false,
            "waitBodyBobAmount",
            "waitBodyBobSpeed",
            "waitHeadYawAngle",
            "waitHeadYawSpeed",
            "waitLegStompAngle",
            "waitLegStompSpeed",
            "waitLegStompJitter",
            "waitTailSwingAngle",
            "waitMouthOpenBase",
            "waitMouthTalkAngle",
            "waitMouthTalkSpeed",
            "waitEyeLookSideAngle",
            "waitEyeLookSpeed",
            "waitPupilLookSideOffset",
            "waitEyeDartSpeed",
            "waitEyeDartSnap",
            "waitEyeDartSideMultiplier"),
        new SectionDefinition(
            "anim_eat_attack",
            "ANIMATION / Eat Attack",
            "Attack leap pose and timing toward target.",
            false,
            "eatAnimDuration",
            "eatJumpHeight",
            "eatForwardStretch",
            "eatLegFoldAngle",
            "eatAttackMouthOpenAngle",
            "eatAttackGlobalPitch"),
        new SectionDefinition(
            "anim_eat_recovery",
            "ANIMATION / Eat Recovery",
            "Landing pose and post-attack inertia recovery.",
            false,
            "eatRecoveryMouthOpenAngle",
            "eatRecoveryGlobalPitch",
            "eatRecoveryPitchBlendDuration",
            "eatRecoveryLegForwardAngle"),
        new SectionDefinition(
            "anim_stunned",
            "ANIMATION / Stunned",
            "Shake and facial pose when stunned.",
            false,
            "stunnedShakeAngle",
            "stunnedShakeSpeed",
            "stunnedMouthOpenAngle"),
        new SectionDefinition(
            "debug",
            "Debug",
            "Visual debug helpers.",
            false,
            "drawDebug"),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawCaption();
        DrawScriptField();
        EditorGUILayout.Space(2f);

        for (int i = 0; i < Sections.Length; i++)
            DrawSection(Sections[i]);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCaption()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Hippo Visual", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tune creature structure, part layout, materials, and per-mode animation from one inspector.", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Rebuild Visual", "Fully rebuilds the procedural creature hierarchy."), GUILayout.Height(20f)))
                    RebuildSelectedVisuals();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(GetIconContent("IN_foldout_act_on", "Expand all sections", "+"), EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(18f)))
                    SetAllSectionStates(true);

                if (GUILayout.Button(GetIconContent("IN_foldout_act", "Collapse all sections", "-"), EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(18f)))
                    SetAllSectionStates(false);
            }
        }
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            MonoScript script = MonoScript.FromMonoBehaviour((HippoVisual)target);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }

    private void DrawSection(SectionDefinition section)
    {
        bool expanded = SessionState.GetBool(FoldoutStatePrefix + section.Key, section.DefaultExpanded);

        using (new EditorGUILayout.HorizontalScope())
        {
            expanded = EditorGUILayout.Foldout(expanded, section.Title, true);
            if (GUILayout.Button(GetIconContent("P4_Conflicted", section.HelpText, "?"), EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(18f)))
                EditorUtility.DisplayDialog(section.Title, section.HelpText, "OK");
        }

        SessionState.SetBool(FoldoutStatePrefix + section.Key, expanded);
        if (!expanded)
            return;

        EditorGUI.indentLevel++;
        DrawSectionProperties(section);
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2f);
    }

    private void DrawSectionProperties(SectionDefinition section)
    {
        for (int i = 0; i < section.PropertyNames.Length; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(section.PropertyNames[i]);
            if (property != null)
                EditorGUILayout.PropertyField(property, true);
        }
    }

    private static GUIContent GetIconContent(string iconName, string tooltip, string fallbackText)
    {
        GUIContent icon = EditorGUIUtility.IconContent(iconName);
        if (icon != null && icon.image != null)
        {
            icon.tooltip = tooltip;
            return icon;
        }

        return new GUIContent(fallbackText, tooltip);
    }

    private void SetAllSectionStates(bool expanded)
    {
        for (int i = 0; i < Sections.Length; i++)
            SessionState.SetBool(FoldoutStatePrefix + Sections[i].Key, expanded);
    }

    private void RebuildSelectedVisuals()
    {
        serializedObject.ApplyModifiedProperties();

        for (int i = 0; i < targets.Length; i++)
        {
            HippoVisual visual = targets[i] as HippoVisual;
            if (visual == null)
                continue;

            Undo.RegisterFullObjectHierarchyUndo(visual.gameObject, "Rebuild Hippo Visual");
            visual.RebuildVisual();
            EditorUtility.SetDirty(visual);
        }

        SceneView.RepaintAll();
    }
}
