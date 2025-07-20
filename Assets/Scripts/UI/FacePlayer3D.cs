using UnityEngine;

public class FacePlayer3D : MonoBehaviour
{
    public GameObject gameObject;
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            Vector3 direction = gameObject.transform.position - cam.position;
            direction.y = 0; // optional: only rotate on Y-axis
            gameObject.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}