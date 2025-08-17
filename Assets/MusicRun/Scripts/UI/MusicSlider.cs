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
        private MidiTempoSync midiTempoSync;
        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            midiTempoSync = gameManager.midiTempoSync;
            Transform bgTransform = musicSlider.transform.Find("Fill Area").transform.Find("Fill");
            if (bgTransform != null)
            {
                Image sliderBackground = bgTransform.GetComponent<Image>();
                sliderBackground.color = Utilities.ColorBase;
            }
        }

        void Update()
        {
            if (midiTempoSync.midiPlayer.MPTK_IsPlaying)
            {
                musicSlider.value = gameManager.MusicPercentage;
                musicText.text = $"Music {midiTempoSync.Progress:F0} %";
            }
        }
    }
}