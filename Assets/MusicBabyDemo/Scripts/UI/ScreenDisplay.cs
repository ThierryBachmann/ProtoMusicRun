using System.Collections;
using UnityEngine;

public class ScreenDisplay : MonoBehaviour
{
    public float duration = 1.5f;
    public float startY = -1.5f;
    public float endY = 0f;
    // Animation curve for a smooth motion (starts fast, ends slow)
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public void ResetPosition()
    {
        Vector3 pos = gameObject.transform.position;
        pos.y = startY;
        gameObject.transform.position = pos;
    }

    public void ShowPanel()
    {
        StartCoroutine(RiseCoroutine());
    }

    private IEnumerator RiseCoroutine()
    {
        float elapsed = 0f;
        // Reminder,
        // for child GameObjects, the "Position" field in the Unity Inspector shows the value of transform.localPosition, not transform.position.
        // But World position (absolute in the scene) by script.
        Vector3 pos = gameObject.transform.position;
        Debug.Log($"RiseCoroutine got go at {pos} from: {startY} to: {endY}");
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = riseCurve.Evaluate(t);

            pos.y = startY + (endY - startY) * easedT;
            gameObject.transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        pos.y = endY;
        gameObject.transform.position = pos;
    }
}
