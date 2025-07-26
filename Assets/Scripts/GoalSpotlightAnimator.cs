using UnityEngine;

namespace MusicRun
{
    public class GoalSpotlightAnimator : MonoBehaviour
    {
        public Light spotLight;

        // Pulse settings
        public float pulseSpeed = 2f;
        public float baseIntensity = 30f;
        public float pulseAmplitude = 10f;

        // Colors
        public Color defaultColor = Color.red;
        public Color goalReachedColor = Color.green;

        public bool goalReached = false;

        void Start()
        {
            spotLight.color = defaultColor;
        }
        void Update()
        {
            spotLight.intensity = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude; ;
        }

        public void TriggerGoal()
        {
            spotLight.color = goalReachedColor;
        }
    }
}