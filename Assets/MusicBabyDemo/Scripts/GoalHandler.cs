using UnityEngine;
using System;
namespace MusicRun
{

    public class GoalHandler : MonoBehaviour
    {
        public GoalSpotlightAnimator goalSpotlightAnimator;
        public GameObject Pyramid;
        public GameObject Goal;
        public float distancePlayerGoal;
        public float distanceAtStart;
        public float goalRadius = 1.5f;
        public float goalDirection;
        public float goalAngle;
        public bool goalReached;
        public Action<LevelEndedReason> OnGoalReached;

        private GameManager gameManager;
        private PlayerController player;
        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
        }

        void Start()
        {
            //lastPos = player.transform.position;
            goalReached = false;
            distanceAtStart = -1;
        }

        public void NewLevel()
        {
            goalReached = false;
            // Force calculation at next update
            distanceAtStart = -1;
        }

        // Update is called once per frame
        void Update()
        {

            // Get planar distance (ignores height difference)
            Vector3 delta = Goal.transform.position - player.transform.position;
            delta.y = 0f; // Ignore height difference
            distancePlayerGoal = delta.magnitude;
            if (distanceAtStart < 0)
                distanceAtStart = distancePlayerGoal;

            // Convert goal world position to player's local space
            Vector3 localToGoal = player.transform.InverseTransformPoint(Goal.transform.position);

            // Flatten to horizontal plane by zeroing the Y axis
            localToGoal.y = 0f;

            // Get the direction in the player's plane (normalized)
            Vector3 planarDirection = localToGoal.normalized;

            // Angle signé autour de l'axe vertical (Y)
            goalAngle = Mathf.Atan2(localToGoal.x, localToGoal.z) * Mathf.Rad2Deg;

            // Player reach the goal ?
            // -----------------------
            if (distancePlayerGoal <= goalRadius && !goalReached)
            {
                Debug.Log("GoalHandler - Goal Reached");
                goalReached = true;
                goalSpotlightAnimator.TriggerGoal();

                // Will trigger action in GameManager
                OnGoalReached?.Invoke(LevelEndedReason.GoalReached);
            }
        }
    }
}