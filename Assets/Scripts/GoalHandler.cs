using UnityEngine;
using System;
using TMPro;
namespace MusicRun
{

    public class GoalHandler : MonoBehaviour
    {
        public GoalSpotlightAnimator goalSpotlightAnimator;
        public GameObject sphere;
        public GameObject Goal;
        public float distance;            // (debug) distance actuelle
        public float distanceAtStart;            // (debug) distance actuelle
        public float goalRadius = 1.5f;   // rayon d’arrivée (en mètres)
        public float goalDirection;
        public float goalAngle;
        public bool goalReached;
        public Action<bool> OnLevelCompleted;

        private GameManager gameManager;
        private PlayerController player;
        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.Player;
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
            // Convert goal world position to player's local space
            Vector3 localToGoal = player.transform.InverseTransformPoint(Goal.transform.position);

            // Flatten to horizontal plane by zeroing the Y axis
            localToGoal.y = 0f;

            // Get planar distance (ignores height difference)
            distance = localToGoal.magnitude;
            if (distanceAtStart < 0) distanceAtStart = distance;

            // Get the direction in the player's plane (normalized)
            Vector3 planarDirection = localToGoal.normalized;

            // Angle signé autour de l'axe vertical (Y)
            goalAngle = Mathf.Atan2(localToGoal.x, localToGoal.z) * Mathf.Rad2Deg;

            ////  dans l’espace global mais projeté (just for fun)
            //// -------------------------------------------------
            //// Vecteur direction du joueur vers le but
            //Vector3 toGoal = Goal.transform.position - player.transform.position;

            //// Projeter dans le plan horizontal (XZ)
            //Vector3 flatToGoal = toGoal;
            //flatToGoal.y = 0f;

            //// Distance horizontale
            //distance = flatToGoal.magnitude;

            //// Direction du joueur, projetée aussi
            //Vector3 playerForward = player.transform.forward;
            //playerForward.y = 0f;
            //playerForward.Normalize();
            //flatToGoal.Normalize();

            //// Angle signé dans le plan horizontal
            //goalAngle = Vector3.SignedAngle(playerForward, flatToGoal, Vector3.up);


            // Player reach the goal ?
            // -----------------------
            if (distance <= goalRadius && !goalReached)
            {
                Debug.Log("But atteint !");
                goalReached = true;
                goalSpotlightAnimator.TriggerGoal();
                OnLevelCompleted?.Invoke(true);
            }
        }
    }
}