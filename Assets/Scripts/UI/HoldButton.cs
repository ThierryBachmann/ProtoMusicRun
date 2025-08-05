using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
        }
    }
}