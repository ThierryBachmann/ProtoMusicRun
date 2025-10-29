using MusicRun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleItem : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI valueText;

    private Image imageBackground;
    private Color backgroundColor;
    private Coroutine blinkCoroutine;


    void Awake()
    {
        imageBackground = GetComponent<Image>();
        backgroundColor = imageBackground.color;
    }

    public void SetColor(Color color)
    {
        imageBackground.color = color;
    }
    public void ResetColor()
    {
        imageBackground.color = backgroundColor;
    }

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetValue(string value)
    {
        valueText.text = value;
    }

    /// <summary>
    /// Blink the background between the original color and <paramref name="color"/> for <paramref name="duration"/> seconds.
    /// </summary>
    /// <param name="color">Target blink color.</param>
    /// <param name="duration">Total blink duration in seconds.</param>
    /// <param name="interval">Time between color switches in seconds (default 0.2s).</param>
    public void BlinkBackground(Color color, float duration, float interval = 0.2f)
    {
        if (imageBackground == null) imageBackground = GetComponent<Image>();
        blinkCoroutine = StartCoroutine(BlinkRoutine(color, duration, interval));
    }

    private IEnumerator BlinkRoutine(Color color, float duration, float interval)
    {
        float elapsed = 0f;

        // Defensive: clamp values
        interval = Mathf.Max(0.01f, interval);
        duration = Mathf.Max(0f, duration);

        while (elapsed < duration)
        {
            imageBackground.color = color;
            yield return new WaitForSeconds(interval);
            imageBackground.color = backgroundColor;
            yield return new WaitForSeconds(interval);
            elapsed += 2 * interval;
        }
    }
}
