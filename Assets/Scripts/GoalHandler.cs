using UnityEngine;
using System;

public class GoalHandler : MonoBehaviour
{
    public PlayerController player;
    public GoalSpotlightAnimator goalSpotlightAnimator;
    public GameObject sphere;
    public GameObject Goal;
    public float distance;            // (debug) distance actuelle
    public float goalRadius = 1.5f;   // rayon d’arrivée (en mètres)
    public float goalDirection;
    public float goalAngle;
    public bool goalReached;
    public Action<bool> OnLevelCompleted;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //lastPos = player.transform.position;
        goalReached = false;
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

        // Get the direction in the player's plane (normalized)
        Vector3 planarDirection = localToGoal.normalized;

        // Angle signé autour de l'axe vertical (Y)
        goalAngle = Mathf.Atan2(localToGoal.x, localToGoal.z) * Mathf.Rad2Deg; 

        /*

        // 1) calcul de la distance
        distance = Vector3.Distance(player.transform.position,
                                    Goal.transform.position);

        Vector3 toGoal = (Goal.transform.position - player.transform.position).normalized;
        Vector3 playerDir = player.transform.forward;

        // Cosinus de l'angle entre les deux directions :
        goalDirection = Vector3.Dot(playerDir, toGoal);

        // Angle signé autour de l'axe vertical (Y)
        goalAngle = Vector3.SignedAngle(playerDir, toGoal, Vector3.up);
        // 0	Le joueur regarde directement vers le goal
        // > 0 Le goal est vers la droite du joueur
        // < 0 Le goal est vers la gauche du joueur
     */
        if (distance <= goalRadius && !goalReached)
        {
            Debug.Log("But atteint !");
            goalReached = true;
            goalSpotlightAnimator.TriggerGoal();
            OnLevelCompleted?.Invoke(true);
        }
    }
}
