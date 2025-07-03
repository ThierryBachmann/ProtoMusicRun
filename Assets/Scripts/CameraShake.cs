using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 initialPosition;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;
    private float dampingSpeed = 1.0f;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            transform.localPosition = initialPosition + shakeOffset;

            shakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            shakeDuration = 0f;
            transform.localPosition = initialPosition;
        }
    }

    public void TriggerShake(float duration, float magnitude = 0.1f, float damping = 1f)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
        dampingSpeed = damping;
    }
}
