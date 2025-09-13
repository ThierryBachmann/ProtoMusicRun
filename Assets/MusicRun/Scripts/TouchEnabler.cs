using MusicRun;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchEnabler : MonoBehaviour
{
    public float minSwipeDistanceX = 1f; // pixels
    public float minSwipeDistanceY = 50f; // pixels
    public InputSystemAction controls;

    private Vector2 startPos;
    private bool swipeInProgress = false;
    private int swipHorizontalDirection;
    private int swipVerticalDirection;

    void Awake()
    {
        controls = new InputSystemAction();
        swipeInProgress = false;
        swipHorizontalDirection = 0;
        swipVerticalDirection = 0;

    }
    void OnEnable()
    {
        controls.Gameplay.Enable();
        // Finally, enabled with Window → Analysis → Input Debugger.
        //    EnhancedTouchSupport.Enable();
        //    TouchSimulation.Enable();
    }

    void OnDisable()
    {
        controls.Gameplay.Disable();
        EnhancedTouchSupport.Disable();
    }

    public bool TurnLeftIsPressed => controls.Gameplay.TurnLeft.IsPressed() || swipHorizontalDirection > 0;
    public bool TurnRightIsPressed => controls.Gameplay.TurnRight.IsPressed() || swipHorizontalDirection < 0;
    public bool TurnUpIsPressed => controls.Gameplay.Jump.IsPressed() || swipVerticalDirection > 0;


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

                        if (delta.magnitude > minSwipeDistanceX)
                        {
                            swipHorizontalDirection = delta.x > 0 ? -1 : 1;
                            startPos.x = endPos.x;
                        }
                        if (delta.magnitude > minSwipeDistanceY)
                        {
                            swipVerticalDirection = delta.y > 0 ? 1 : -1;
                            startPos.y = endPos.y;
                        }
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                    swipeInProgress = false;
                    swipHorizontalDirection = 0;
                    swipVerticalDirection = 0;
                    break;
            }
        }
    }
}
