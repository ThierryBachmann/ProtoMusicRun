using UnityEngine;


public class TestContextMenuItem : MonoBehaviour
{
    [SerializeField]
    [ContextMenuItem("Reset Value", "ResetMyValue")]
    private int myValue = 10;

    private void ResetMyValue()
    {
        myValue = 0;
        Debug.Log("Value reset to 0!");
    }
    void Start()
    {

    }

    void Update()
    {

    }
}
