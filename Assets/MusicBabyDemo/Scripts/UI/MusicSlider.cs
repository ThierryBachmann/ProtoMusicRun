using UnityEngine;
using UnityEngine.UI;
using MidiPlayerTK;
using TMPro;
namespace MusicRun
{
    public class MusicSlider : MonoBehaviour
    {
        public Slider musicSlider;
        public TextMeshProUGUI musicText;
        private GameManager gameManager;
        private MidiManager midiManager;
        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            midiManager = gameManager.midiManager;
            Transform bgTransform = musicSlider.transform.Find("Fill Area").transform.Find("Fill");
            if (bgTransform != null)
            {
                Image sliderBackground = bgTransform.GetComponent<Image>();
                sliderBackground.color = Utilities.ColorBase;
            }
        }

        void Update()
        {
            if (gameManager.gameRunning)
            {
                musicSlider.value = gameManager.MusicPercentage;
                musicText.text = $"Music {midiManager.Progress:F0} %";
            }
            else
            {
                musicSlider.value = 0;
                musicText.text = "";
            }
        }

    }
}