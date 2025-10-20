using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{
    public class PanelDisplay : MonoBehaviour, IPointerClickHandler
    {
        public float animationDuration = 0.4f;
        public bool closeOnClick = true;
        public Vector3 startScale = new Vector3(0.5f, 0.5f, 0.5f);
        public Vector3 endScale = Vector3.one;
        public System.Action<bool> OnClose;
        public bool IsVisible;

        protected GameManager gameManager;
        protected PlayerController player;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;


        public void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Start hidden and small
            canvasGroup.alpha = 0;
            IsVisible = false;
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
            Debug.Log($"Panel Display {name} Show - Visible: {IsVisible}");
            if (!IsVisible)
            {
                StartCoroutine(AnimateIn());
            }
        }

        public void Hide()
        {
            Debug.Log($"Panel Display {name} Hide - Visible:{IsVisible}");
            if (IsVisible)
            {
                StartCoroutine(AnimateOut());
            }
        }
        public void SwitchVisible()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        // Check if the click target is the panel itself (not a child like your button)
        public void OnPointerClick(PointerEventData eventData)
        {
            if (closeOnClick)
            {
                GameObject clicked = eventData.pointerCurrentRaycast.gameObject;
                //Debug.Log($"Panel clicked! {clicked} {eventData}");

                if (clicked != null && clicked.GetComponentInParent<Button>() != null)
                    return;
               

                Hide();
            }
        }


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
            IsVisible = true;
        }

        private IEnumerator AnimateOut()
        {
            float timer = 0f;

            // Initial state
            rectTransform.localScale = endScale;
            float animateOut = animationDuration / 2f;
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
            IsVisible = false;
            OnClose?.Invoke(true);
        }
    }
}