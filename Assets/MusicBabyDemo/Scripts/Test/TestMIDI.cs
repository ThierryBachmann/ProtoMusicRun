using UnityEngine;

namespace MusicRun
{

    public class TestMIDI : MonoBehaviour
    {
       public MidiManagerTest midiManagerTest;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void StartLevel1()
        {
            midiManagerTest.LoadMIDI(98);
            midiManagerTest.PlayMIDI();
        }
        public void StartLevel2()
        {
            midiManagerTest.LoadMIDI(99);
            midiManagerTest.PlayMIDI();
        }
    }
}