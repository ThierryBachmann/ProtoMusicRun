using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StrangeCreature))]
[CanEditMultipleObjects]
public class StrangeCreatureEditor : Editor
{
    private const string FoldoutStatePrefix = "StrangeCreatureEditor.Foldout.";

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
            "Global scale for the generated visual hierarchy.",
            true,
            "overallScale"),
        new SectionDefinition(
            "structure_geometry",
            "STRUCTURE / Geometry",
            "Global mesh detail for generated parts. The fused body uses an icosphere-style mesh derived from this detail level.",
            true,
            "sphereDetailLevel"),
        new SectionDefinition(
            "structure_body",
            "STRUCTURE / Body Fusion",
            "Main body dimensions, rear lobe dimensions, anchor placement, and metaball-style merge smoothness.",
            true,
            "bodyWidth",
            "bodyHeight",
            "bodyLength",
            "rearBodyWidth",
            "rearBodyHeight",
            "rearBodyLength",
            "rearBodySeparationRatio",
            "rearBodyAnchorHeightRatio",
            "rearBodyAnchorLengthRatio",
            "rearBodyMergeSmoothness"),
        new SectionDefinition(
            "structure_head",
            "STRUCTURE / Head",
            "Head dimensions and placement relative to the fused body.",
            true,
            "headWidth",
            "headHeight",
            "headLength",
            "headAnchorHeightRatio",
            "headAnchorForwardOffset"),
        new SectionDefinition(
            "structure_ears",
            "STRUCTURE / Ears",
            "Ear dimensions and anchor placement on the head.",
            false,
            "earWidth",
            "earHeight",
            "earLength",
            "earAnchorHeightRatio",
            "earAnchorForwardOffset",
            "earSeparationRatio"),
        new SectionDefinition(
            "structure_eyes",
            "STRUCTURE / Eyes",
            "Eye and pupil dimensions, spacing, and forward placement.",
            false,
            "eyeWidth",
            "eyeHeight",
            "eyeLength",
            "eyeSeparationRatio",
            "eyeAnchorHeightRatio",
            "eyeAnchorForwardOffset",
            "pupilScale",
            "pupilForwardOffset"),
        new SectionDefinition(
            "structure_mouth",
            "STRUCTURE / Mouth",
            "Coupled upper/lower jaw dimensions, mouth anchor placement, jaw separation, and base pitch.",
            false,
            "jawWidth",
            "jawHeight",
            "jawLength",
            "jawWidthUpperToLowerRatio",
            "jawHeightUpperToLowerRatio",
            "jawLengthUpperToLowerRatio",
            "jawVerticalSeparation",
            "jawAnchorHeightRatio",
            "jawAnchorForwardOffset",
            "jawBasePitch"),
        new SectionDefinition(
            "structure_legs",
            "STRUCTURE / Legs",
            "Leg count, body attachment placement, upper leg, ankle, and foot dimensions.",
            false,
            "legPairCount",
            "legAnchorSideOffset",
            "legAnchorHeightRatio",
            "legAnchorLengthRatio",
            "upperLegThickness",
            "upperLegLength",
            "ankleDiameter",
            "footHeight",
            "footWidth"),
        new SectionDefinition(
            "structure_tail",
            "STRUCTURE / Tail",
            "Tail ellipsoid dimensions, anchor placement, and pitch orientation.",
            false,
            "tailWidth",
            "tailHeight",
            "tailLength",
            "tailAnchorSideOffset",
            "tailAnchorHeightRatio",
            "tailAnchorForwardOffset",
            "tailPitchDegrees"),
        new SectionDefinition(
            "structure_collision",
            "STRUCTURE / Collision",
            "Collider policy: remove generated visual colliders and optionally auto-fit one root trigger sphere.",
            false,
            "removeGeneratedPartColliders",
            "autoStructureTriggerCollider",
            "triggerCenterOffset",
            "triggerRadiusScale",
            "triggerRadiusPadding"),
        new SectionDefinition(
            "structure_attachment",
            "STRUCTURE / Attachment",
            "Visual anchoring constraints relative to the parent, useful when the controller tilts on slopes.",
            false,
            "lockLocalXZToParent"),
        new SectionDefinition(
            "materials",
            "Materials",
            "Explicit material assignments by visual slot.",
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
            "Colors used to create runtime fallback materials when no explicit material is assigned.",
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
            "Animation/movement coupling: gait phase can advance from real horizontal displacement.",
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
            "Impatient behavior: body bob, head yaw, leg stomps, talking mouth, tail swing, and darting pupils.",
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
            "Attack leap pose, folded legs, global pitch, and mouth opening.",
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
            "Landing/recovery pose, pitch blend from attack, legs, and mouth recovery.",
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
            EditorGUILayout.LabelField("Strange Creature", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tune procedural body fusion, part layout, materials, and per-mode animation from one inspector.", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Rebuild Visual", "Fully rebuilds the generated hierarchy and fused body mesh."), GUILayout.Height(20f)))
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
            MonoScript script = MonoScript.FromMonoBehaviour((StrangeCreature)target);
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
            StrangeCreature visual = targets[i] as StrangeCreature;
            if (visual == null)
                continue;

            Undo.RegisterFullObjectHierarchyUndo(visual.gameObject, "Rebuild Strange Creature");
            visual.RebuildVisual();
            EditorUtility.SetDirty(visual);
        }

        SceneView.RepaintAll();
    }
}
