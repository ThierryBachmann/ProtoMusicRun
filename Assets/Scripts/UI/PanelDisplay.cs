using UnityEngine;
using System.Collections;

public class PanelDisplay : MonoBehaviour
{
    public float animationDuration = 0.4f;
    public Vector3 startScale = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 endScale = Vector3.one;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    public void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Start hidden and small
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = startScale;
    }

    public void Start()
    {
        Hide();
    }

    public void Show()
    {
        if (Visible < 0.5f)
        {
            StartCoroutine(AnimateIn());
        }
    }

    public void Hide()
    {
        if (Visible >= 0.5f)
            StartCoroutine(AnimateOut());
    }
    public void SwitchVisible()
    {
        if (Visible > 0.5f)
            Hide();
        else
            Show();
    }

    public float Visible => canvasGroup.alpha;

    private IEnumerator AnimateIn()
    {
        float timer = 0f;

        // Initial state
        rectTransform.localScale = startScale;
        canvasGroup.alpha = 0;

        while (timer < animationDuration)
        {
            float t = timer / animationDuration;
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            canvasGroup.alpha = t;
            timer += Time.deltaTime;
            yield return null;
        }

        // Final state
        rectTransform.localScale = endScale;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator AnimateOut()
    {
        float timer = 0f;

        // Initial state
        rectTransform.localScale = endScale;
        float animateOut = animationDuration / 3f;
        while (timer < animateOut)
        {
            float t = 1f - timer / animateOut;
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            canvasGroup.alpha = t;
            timer += Time.deltaTime;
            yield return null;
        }

        // Final state
        rectTransform.localScale = startScale;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
