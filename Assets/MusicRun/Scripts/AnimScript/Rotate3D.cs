using UnityEngine;

namespace MusicRun
{
    public class Rotate3D : MonoBehaviour
    {
        public GameObject gameObjectToRotate;
        public float rotationSpeedX = 0f; // degrees per second
        public float rotationSpeedY = 90f; // degrees per second
        public float rotationSpeedZ = 0f; // degrees per second

        void Awake()
        {
            if (gameObjectToRotate == null)
            {
                gameObjectToRotate= gameObject;
            }
        }
        void Start()
        {
        }

        void LateUpdate()
        {
            gameObjectToRotate.transform.Rotate(
                 rotationSpeedX * Time.deltaTime,
                 rotationSpeedY * Time.deltaTime,
                 rotationSpeedZ * Time.deltaTime,
                 Space.Self
             );
        }
    }
}