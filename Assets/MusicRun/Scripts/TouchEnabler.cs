using MusicRun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace MusicRun
{
    public class TouchEnabler : MonoBehaviour
    {
        public float minSwipeDistanceX = 1f; // pixels
        public float minSwipeDistanceY = 50f; // pixels
        public float SwipeHorizontalValue;
        public float SwipeVerticalValue;
        public InputSystemAction controls;
        public bool TurnLeftIsPressed => controls.Gameplay.TurnLeft.IsPressed();
        public bool TurnRightIsPressed => controls.Gameplay.TurnRight.IsPressed();
        public bool TurnUpIsPressed => controls.Gameplay.Jump.IsPressed();

        private Vector2 startPos;
        private bool swipeInProgress = false;

        void Awake()
        {
            controls = new InputSystemAction();
            swipeInProgress = false;
            SwipeHorizontalValue = 0;
            SwipeVerticalValue = 0;
        }

        void OnEnable()
        {
            controls.Gameplay.Enable();
            // Finally, enabled with Window → Analysis → Input Debugger.
            //    EnhancedTouchSupport.Enable();
            //    TouchSimulation.Enable();
    //        controls.Gameplay.Start.performed += OnStartPressed;
        }

        void OnDisable()
        {
            controls.Gameplay.Disable();
            EnhancedTouchSupport.Disable();
        }

     

        void Update()
        {
            foreach (var t in Touch.activeTouches)
            {
                //Debug.Log($"touch id:{t.touchId} pos:{t.screenPosition} phase:{t.phase}");
                switch (t.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        startPos = t.screenPosition;
                        swipeInProgress = true;
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        if (swipeInProgress)
                        {
                            Vector2 endPos = t.screenPosition;
                            Vector2 delta = endPos - startPos;
                            Debug.Log(delta);
                            if (Mathf.Abs(delta.x) > minSwipeDistanceX)
                            {
                                SwipeHorizontalValue = delta.x;
                                //startPos.x = endPos.x;
                            }
                            if (Mathf.Abs(delta.y) > minSwipeDistanceY)
                            {
                                SwipeVerticalValue = delta.y;
                                startPos.y = endPos.y;
                            }
                        }
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        swipeInProgress = false;
                        // The swipe direction maintains theirs values even without move but until the ended.
                        SwipeHorizontalValue = 0;
                        SwipeVerticalValue = 0;
                        break;
                }
            }
        }

        /// <summary>
        /// The swipe direction maintains theirs values even without move but until the ended.
        /// To change this behavior client can reset the swipe.
        /// Not used, player turn continuously until the gesture end.
        /// </summary>
        public void ResetSwipeHorizontal(){ SwipeHorizontalValue = 0; }
        /// <summary>
        /// The swipe direction maintains theirs values even without move but until the ended.
        /// To change this behavior client can reset the swipe.
        /// Player stops jumping at the first detection.
        /// </summary>
        public void ResetSwipeVertical() { SwipeVerticalValue = 0; }
    }
}