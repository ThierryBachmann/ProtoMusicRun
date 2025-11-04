using UnityEngine;

namespace MusicRun
{
    public class FacePlayer3D : MonoBehaviour
    {
        public GameObject gameObjectToFace;
        private Transform cam;

        void Start()
        {
            cam = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (cam != null)
            {
                if (gameObjectToFace == null)
                    gameObjectToFace = gameObject;
                Vector3 direction = gameObjectToFace.transform.position - cam.position;
                direction.y = 0; // optional: only rotate on Y-axis
                gameObjectToFace.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}