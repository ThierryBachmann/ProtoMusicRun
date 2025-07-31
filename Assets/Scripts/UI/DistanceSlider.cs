using MidiPlayerTK;
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

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            goalHandler = gameManager.GoalHandler;
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
                if (goalHandler.distanceAtStart > 0)
                    distanceSlider.value = 100f - (goalHandler.distance / goalHandler.distanceAtStart * 100f);
            }
        }
    }
}