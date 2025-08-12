using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{
    public class ActionPlay : MonoBehaviour
    {
        public HoldButton leftButton;
        public HoldButton rightButton;
        public Button jumpButton;

        void Update()
        {
            if (leftButton.IsHeld) MoveLeft();
            if (rightButton.IsHeld) MoveRight();
        }

        void Start()
        {
            jumpButton.onClick.AddListener(Jump); // saut : clic simple, pas besoin de maintien
        }

        void MoveLeft() { Debug.Log("move left"); }
        void MoveRight() { Debug.Log("move right"); }
        void Jump() { /* saut */ }
    }

}