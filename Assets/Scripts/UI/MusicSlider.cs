using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MidiPlayerTK;
namespace MusicRun
{
    public class MusicSlider : MonoBehaviour
    {
        public Slider musicSlider;
        public TextMeshProUGUI musicText;

        private GameManager gameManager;
        private MidiFilePlayer midiPlayer;

        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            midiPlayer = gameManager.MidiPlayer;
           
            Transform bgTransform = musicSlider.transform.Find("Fill Area").transform.Find("Fill");
            if (bgTransform != null)
            {
                Image sliderBackground = bgTransform.GetComponent<Image>();
                sliderBackground.color = Utilities.ColorBase;
            }

            midiPlayer.MPTK_StartPlayAtFirstNote = true;
            midiPlayer.MPTK_MidiAutoRestart = false;
            midiPlayer.OnEventStartPlayMidi.AddListener((string info) =>
            {
                if (info != null)
                {
                    Debug.Log($"Music started: {info}");
                }
            });
            midiPlayer.OnEventEndPlayMidi.AddListener((string info, EventEndMidiEnum endMidi) =>
            {
                if (info != null)
                {
                    Debug.Log($"Music ended: {endMidi} {info}");
                }
            });
        }

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (midiPlayer.MPTK_IsPlaying)
            {
                musicSlider.value = gameManager.MusicPercentage;
                musicText.text = $"Music {gameManager.MusicPercentage:F0} %";
            }
        }
    }
}