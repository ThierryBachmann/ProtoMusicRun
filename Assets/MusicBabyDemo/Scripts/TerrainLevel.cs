using MusicRun;
using System;
using UnityEngine;

namespace MusicRun
{

    [Serializable]
    public class TerrainLevel
    {
        //[SerializeField]
        //[ContextMenuItem("Reset Health", nameof(ResetHealth))]
        //private string health = "";

        //private void ResetHealth()
        //{
        //    health = "reset";
        //    Debug.Log("Health reset to 100!");
        //}
        [Header("Whether to use this scene.")]
        public bool enabled;
        [Header("Title and Description displayed at the scene start.")]
        public string name;
        [TextArea]
        public string description;
        [Header("Select a camera with a dedicated skybox for this scene.")]
        public Camera Skybox;
        [Header("Defined MIDI associated to the level")]
        public int indexMIDI;

        [Range(0.1f, 5f)]
        public float RatioSpeedMusic = 0.3f;
        [Range(0.1f, 5f)]
        public float MinSpeedMusic = 0.1f;
        [Range(0.1f, 5f)]
        public float MaxSpeedMusic = 5f;


        [Header("Delta chunk position with last goal")]
        public Vector2Int deltaCurrentChunk;

        [Header("Defined start and goal game object")]
        public GameObject startGO;
        public GameObject goalGO;

        [Header("How much vegetable must be spread on chunk")]
        [Range(0f, 10f)]
        public float perlinVegetable = 0.3f;

        [Header("How much vegetable must be spread by chunk")]
        [Range(0f, 10f)]
        public float perlinChunk = 100f;

        [Header("Min Max fpr random vegetable scale")]
        [Range(0.1f, 15f)]
        public float minScaleVegetable = 0.5f;

        [Range(0.1f, 15f)]
        public float maxScaleVegetable = 0.5f;

        [Header("Defined levels")]
        public GameObject[] runChunks;

        [Header("Defined Vegetables")]
        public Vegetable[] vegetables;

        [Header("Score Bonus")]
        [Range(0, 10)]
        [Tooltip("Description")]
        public float bonusScoreDensity = 1;
        public GameObject[] bonusScorePrefab;

        [Header("Instrument Bonus")]
        [Range(0, 10)]
        [Tooltip("Description")]
        public float bonusInstrumentDentity = 1;
        public GameObject[] bonusInstrumentPrefab;
    }
}