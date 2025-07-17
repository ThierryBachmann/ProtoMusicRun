using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MidiPlayerTK;
public class MusicSlider : MonoBehaviour
{
    public Slider musicSlider;
    public TextMeshProUGUI musicText;

    public MidiFilePlayer midiPlayer;
    public Transform player;

    private float midiLength;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (midiPlayer.MPTK_IsPlaying)
        {
            musicSlider.maxValue = midiPlayer.MPTK_TickLastNote;
            musicSlider.value=(float)midiPlayer.MPTK_TickCurrent;
        }
    }
}
