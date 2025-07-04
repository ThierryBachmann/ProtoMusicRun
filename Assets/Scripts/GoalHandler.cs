using Unity.VisualScripting;
using UnityEngine;

public class GoalHandler : MonoBehaviour
{
    public PlayerController player;
    public float Distance;
    public GameObject sphere;
    public float distance;            // (debug) distance actuelle
    public float goalRadius = 1.5f;   // rayon d’arrivée (en mètres)
    public float forward;
    private Vector3 lastPos;
    public bool goalReached;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastPos = player.transform.position;
        goalReached=false;
    }

    // Update is called once per frame
    void Update()
    {
        // 1) calcul de la distance
        distance = Vector3.Distance(player.transform.position,
                                    sphere.transform.position);

        // Direction vers le goal
        Vector3 currentPos = player.transform.position;
        Vector3 toGoal = sphere.transform.position - currentPos;
        Vector3 dirGoal = toGoal.normalized;

        // Déplacement depuis la frame précédente
        Vector3 delta = currentPos - lastPos;

        // Projection scalaire (dot product) 0.5 direction vers le goal, -0.5 s'éloigne du goal
        //      Si le joueur avance vers le but : forwardMeters > 0
        //      S’il zigzague ou repart en arrière : forwardMeters ≤ 0

        forward = Vector3.Dot(delta, dirGoal); // mètres réellement gagnés
        // 2) le joueur est‑il arrivé ?
        if (distance <= goalRadius)
        {
            Debug.Log("But atteint !");
            // ➜ ici, lance la fin de niveau, un son, etc.
            // …
            goalReached = true;
        }
        lastPos = currentPos;

    }
}
