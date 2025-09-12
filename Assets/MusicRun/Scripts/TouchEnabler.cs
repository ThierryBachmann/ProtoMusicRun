using MusicRun;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchEnabler : MonoBehaviour
{
    public float minSwipeDistance = 50f; // pixels
    public PlayerControls controls;

    private Vector2 startPos;
    private bool swipeInProgress = false;
    private int swipHorizontalDirection;
    private int swipVerticalDirection;

    void Awake()
    {
        controls = new PlayerControls();
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


    void Update()
    {
        foreach (var t in Touch.activeTouches)
        {
            Debug.Log($"touch id:{t.touchId} pos:{t.screenPosition} phase:{t.phase}");
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

                        if (delta.magnitude > minSwipeDistance)
                        {
                            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                            {
                                if (delta.x > 0)
                                    swipHorizontalDirection = -1;

                                else
                                    swipHorizontalDirection = 1;
                            }
                            else
                            {
                                if (delta.y > 0)
                                    swipVerticalDirection = 1;
                                else
                                    swipVerticalDirection = -1;
                            }
                            startPos = endPos;
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
