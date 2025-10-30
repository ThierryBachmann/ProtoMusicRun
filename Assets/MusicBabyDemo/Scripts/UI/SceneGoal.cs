/*
 * A 5-second looping 1.85:1 video of a cartoon pop band performing on stage. The band consists of four anthropomorphic animals: a drummer, a guitarist, a singer, and a bass player. Each character has a unique, expressive design inspired by modern pop animation, with lively movements and rhythmic energy. The scene features warm stage lighting in pink and blue tones, with spotlights that pulse gently to the beat, and energetic crowd silhouettes in the background. The camera slowly pans across the band as they play, creating a smooth, seamless loop. The visual style is colorful, slightly exaggerated, and vibrant. Perfectly looping animation.
 * */
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class SceneGoal : PanelDisplay
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI infoText;


        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            base.Start();
        }
        public void SetInfo(string title, string info)
        {
            titleText.text=title;
            infoText.text=info;
        }
    }
}