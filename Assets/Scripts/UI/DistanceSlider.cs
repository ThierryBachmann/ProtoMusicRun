using MidiPlayerTK;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace MusicRun
{
    public class DistanceSlider : MonoBehaviour
    {
        public Slider distanceSlider;
        public TextMeshProUGUI distanceText;

        private GameManager gameManager;
        private GoalHandler goalHandler;
        private Image sliderBackground;

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            goalHandler = gameManager.GoalHandler;
            if (distanceSlider != null)
            {
                Transform bgTransform = distanceSlider.transform.Find("Fill Area").transform.Find("Fill");
                if (bgTransform != null)
                {
                    sliderBackground = bgTransform.GetComponent<Image>();
                }
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (gameManager.gameRunning && gameManager.levelRunning)
            {
                distanceSlider.value = gameManager.GoalPercentage;
                distanceText.text = $"Distance {goalHandler.distance:F0} m";
                if (gameManager.GoalPercentage > gameManager.MusicPercentage)
                    sliderBackground.color = Utilities.ColorWarning;
                else
                    sliderBackground.color = Utilities.ColorBase;

            }
        }
    }
}