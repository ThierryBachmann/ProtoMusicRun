using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class SettingScreen : PanelDisplay
    {
        public Toggle toggleLiteMode;
        public TMP_InputField inputName;

        public Action OnSettingChange;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            toggleLiteMode.isOn = gameManager.liteMode;
            toggleLiteMode.onValueChanged.AddListener(val =>
            {
                gameManager.liteModeSetting = toggleLiteMode.isOn;
                OnSettingChange.Invoke();
            });
            inputName.onValueChanged.AddListener(val =>
            {
                player.playerName = inputName.text;
                OnSettingChange.Invoke();
            });

            base.Start();
        }

        public new void Show()
        {
            gameManager.enableShortcutKeys = false;
            base.Show();
        }

        public new void Hide()
        {
            gameManager.enableShortcutKeys = true;
            base.Hide();
        }

        public void SetValue()
        {
            toggleLiteMode.isOn = gameManager.liteMode;
            inputName.SetTextWithoutNotify(player.playerName);
        }
        private string GeneratePlayerName()
        {
            // Generate a fun, music-themed name
            string[] adjectives = { "Melodic", "Rhythmic", "Sonic", "Harmonic", "Beat", "Bass", "Treble", "Echo", "Tempo", "Groove", "Baby" };
            string[] nouns = { "Runner", "Dasher", "Sprinter", "Racer", "Seeker", "Explorer", "Navigator", "Traveler", "Wanderer", "Pathfinder" };

            string adjective = adjectives[UnityEngine.Random.Range(0, adjectives.Length)];
            string noun = nouns[UnityEngine.Random.Range(0, nouns.Length)];
            int number = UnityEngine.Random.Range(100, 999);

            return $"{adjective}{noun}{number}";
        }
    }
}