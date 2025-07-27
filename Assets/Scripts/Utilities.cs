using System;
using System.Collections;
using UnityEngine;

namespace MusicRun
{
    public class Utilities
    {
        static public IEnumerator WaitAndCall(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}