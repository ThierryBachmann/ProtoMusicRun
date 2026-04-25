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
            "Etat visuel courant et preview dans l'editeur. En Play mode, le controller pilote automatiquement l'etat.",
            true,
            "state",
            "previewInEditMode"),
        new SectionDefinition(
            "structure_global",
            "STRUCTURE / Global",
            "Echelle globale de la creature procedurale.",
            true,
            "overallScale"),
        new SectionDefinition(
            "structure_body_head",
            "STRUCTURE / Body & Head",
            "Volume general du corps et de la tete.",
            true,
            "bodyHeightFactor",
            "headVolumeFactor"),
        new SectionDefinition(
            "structure_ears",
            "STRUCTURE / Ears",
            "Largeur et placement des oreilles sur la tete.",
            false,
            "earWidthFactor",
            "earPlacementOffset"),
        new SectionDefinition(
            "structure_tail",
            "STRUCTURE / Tail",
            "Position, orientation et forme de la queue (ellipsoide).",
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
            "Parametres couples Upper/Lower jaw: dimensions, offsets, pivot et angle de base.",
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
            "Geometrie des pattes: upper, ankle, foot et hauteur d'ancrage.",
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
            "Position et taille des yeux/pupilles dans la tete.",
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
            "Politique collider: suppression des colliders de parties generees et trigger auto sur la racine.",
            false,
            "removeGeneratedPartColliders",
            "autoStructureTriggerCollider",
            "triggerCenterOffset",
            "triggerRadiusScale",
            "triggerRadiusPadding"),
        new SectionDefinition(
            "structure_attachment",
            "STRUCTURE / Attachment",
            "Contraintes d'ancrage visuel par rapport au parent (utile sur pentes).",
            false,
            "lockLocalXZToParent"),
        new SectionDefinition(
            "materials",
            "Materials",
            "Materiaux explicites par slot visuel.",
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
            "Couleurs utilisees quand aucun materiau n'est assigne pour un slot.",
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
            "Couplage animation/deplacement reel: la phase de foullee avance selon la distance parcourue.",
            false,
            "driveChasePhaseFromDisplacement",
            "chaseMinDisplacement",
            "chaseMaxDisplacementPerFrame"),
        new SectionDefinition(
            "anim_idle",
            "ANIMATION / Idle",
            "Respiration, hochement de tete et balancement de queue au repos.",
            false,
            "idleBreathSpeed",
            "idleBreathAmount",
            "idleHeadNodAngle",
            "idleHeadNodSpeed",
            "idleTailSwingFactor"),
        new SectionDefinition(
            "anim_chase",
            "ANIMATION / Chase",
            "Animation locomotion pour Follow/Overtake/Hunt/Recenter/LeashReturn.",
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
            "Animation creature impatiente: trepignement, bouche qui 'parle' et yeux agaces.",
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
            "Pose et dynamique de bond vers la cible.",
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
            "Pose d'atterrissage et recuperation d'inertie apres l'attaque.",
            false,
            "eatRecoveryMouthOpenAngle",
            "eatRecoveryGlobalPitch",
            "eatRecoveryPitchBlendDuration",
            "eatRecoveryLegForwardAngle"),
        new SectionDefinition(
            "anim_stunned",
            "ANIMATION / Stunned",
            "Oscillation et expression de creature sonnee.",
            false,
            "stunnedShakeAngle",
            "stunnedShakeSpeed",
            "stunnedMouthOpenAngle"),
        new SectionDefinition(
            "debug",
            "Debug",
            "Aides de debug visuelles.",
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
            EditorGUILayout.LabelField("Style MPTK: sections pliables, aide contextuelle et actions rapides.", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Rebuild Visual", "Reconstruit completement la creature procedurale."), GUILayout.Height(20f)))
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
