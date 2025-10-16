using MusicRun;
using System;
using UnityEngine;

namespace MusicRun
{

    [Serializable]
    public class TerrainLevel
    {
        [Header("Enable Scene")]
        [Tooltip("If checked, this scene is active and used in the game.")]
        public bool enabled;

        [Header("Title & Description")]
        [Tooltip("Displayed title for this level, shown at the beginning of the scene.")]
        public string name;

        [TextArea]
        [Tooltip("Description text shown at the start of the scene.")]
        public string description;

        [Header("Skybox Camera")]
        [Tooltip("Camera used to render the skybox for this scene. Select one with a dedicated skybox material.")]
        public Camera Skybox;

        [Header("MIDI Track")]
        [Tooltip("Index or reference of the MIDI file associated with this level.")]
        public int indexMIDI;
        [Tooltip("Number of times the MIDI music should loop before reaching the goal.")]
        [Range(1, 10)]
        public int LoopsToGoal;

        [Header("Music Speed")]
        [Range(0.1f, 5f)]
        [Tooltip("Playback speed multiplier for the MIDI music. 1 = normal speed.")]
        public float RatioSpeedMusic = 0.3f;

        [Range(0.1f, 5f)]
        [Tooltip("Minimum allowed playback speed for the MIDI music.")]
        public float MinSpeedMusic = 0.1f;

        [Range(0.1f, 5f)]
        [Tooltip("Maximum allowed playback speed for the MIDI music.")]
        public float MaxSpeedMusic = 5f;

        [Header("Start & Goal chunk")]
        [Tooltip("GameObject marking the start position of the level.")]
        public GameObject startGO;

        [Tooltip("GameObject marking the goal or end position of the level.")]
        public GameObject goalGO;

        [Tooltip("Offset of the goal chunk relative to the start chunk.")]
        public Vector2Int deltaGoalChunk;

        [Header("Vegetation Spread")]
        [Tooltip("Controls how much vegetation is distributed within each chunk.")]
        [Range(0f, 10f)]
        public float perlinVegetable = 0.3f;
        [Tooltip("Controls how vegetation density changes between chunks.")]
        [Range(0f, 10f)]
        public float perlinChunk = 100f;

        [Header("Vegetation Scale")]
        [Tooltip("Minimum scale factor applied to random vegetation instances.")]
        [Range(0.1f, 15f)]
        public float minScaleVegetable = 0.5f;

        [Tooltip("Maximum scale factor applied to random vegetation instances.")]
        [Range(0.1f, 15f)]
        public float maxScaleVegetable = 0.5f;

        [Header("Run Chunks")]
        [Tooltip("List of terrain chunk prefabs that compose the running path.")]
        public GameObject[] runChunks;

        [Header("Vegetables used for random placement in this level")]
        [Tooltip("Array of vegetation prefabs used for random placement in this level.")]
        public Vegetable[] vegetables;

        [Header("Score Bonus / Malus")]
        [Range(0, 10)]
        [Tooltip("Density of score bonus objects placed along the path. Higher values mean more bonuses.")]
        public float bonusScoreDensity = 1;

        [Range(0, 1)]
        [Tooltip("Balance between bonuses and penalties: 0 = only bonuses, 1 = only penalties.")]
        public float bonusMalusRatio = 0.5f;

        [Tooltip("Prefabs used for score bonus or penalty objects.")]
        public GameObject[] bonusScorePrefab;

        [Header("Instrument Bonus")]
        [Range(0, 10)]
        [Tooltip("Density of instrument bonus objects that can appear during the run.")]
        public float bonusInstrumentDensity = 1;

        [Tooltip("Prefabs used for instrument bonus objects (not yet used).")]
        public GameObject[] bonusInstrumentPrefab;
    }
}