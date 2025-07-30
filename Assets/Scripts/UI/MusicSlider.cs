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

        public MidiFilePlayer midiPlayer;
        public Transform player;

        void Awake()
        {
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
                musicSlider.value = ((float)midiPlayer.MPTK_TickCurrent / (float)midiPlayer.MPTK_TickLastNote) * 100f;
            }
        }
    }
}