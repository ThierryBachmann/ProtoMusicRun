using log4net.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MusicRun
{

    public class TestTerrain : MonoBehaviour
    {
        public TerrainGenerator terrainGenerator;

        void Awake()
        {
            Utilities.Init();
        }

        void Start()
        {
            terrainGenerator.CreateLevel(0);
        }

        int currentLevelIndex;
        void Update()
        {
            // Exemple : touche R pour redémarrer la partie
            if (Input.GetKeyDown(KeyCode.LeftControl)|| Input.GetKeyDown(KeyCode.RightControl))
            {
                terrainGenerator.ClearChunks(0);
                currentLevelIndex = terrainGenerator.SelectNextLevel(currentLevelIndex);
                terrainGenerator.CreateLevel(currentLevelIndex);
            }

        }

    }
}