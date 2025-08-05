using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{
    public class SwitchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsOn { get; private set; }

        public Image BackgroundImage;

        public void Start()
        {
            if (BackgroundImage == null)
            {
                BackgroundImage = GetComponent<Image>();
            }
            UpdateVisualState();

        }
        public void OnPointerDown(PointerEventData eventData)
        {
            IsOn = !IsOn;
            UpdateVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            UpdateVisualState();
        }
        public void SetState(bool state)
        {
            IsOn = state;
            UpdateVisualState();
        }
        private void UpdateVisualState()
        {
            if (BackgroundImage != null)
            {
                BackgroundImage.color = IsOn ? Color.red : Color.white;
            }
        }

    }
}