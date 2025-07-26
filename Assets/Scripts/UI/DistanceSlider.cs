using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MidiPlayerTK;
namespace MusicRun
{
    public class DistanceSlider : MonoBehaviour
    {
        public Slider distanceSlider;
        public TextMeshProUGUI distanceText;
        public GoalHandler goal;
        public GameManager gameManager;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (gameManager.gameRunning && gameManager.levelRunning)
            {
                if (goal.distanceAtStart > 0)
                    distanceSlider.value = 100f - (goal.distance / goal.distanceAtStart * 100f);
            }
        }
    }
}