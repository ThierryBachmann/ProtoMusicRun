using System;
using System.Collections;
using UnityEngine;

namespace MusicRun
{
    /// <summary>
    /// Provides utility methods for common operations.
    /// </summary>
    public class Utilities
    {

        /// <summary>
        /// Executes the specified action after a delay.
        /// </summary>
        /// <remarks>This method is typically used in Unity's coroutine system to schedule an action to
        /// run after a specified delay. Ensure that the method is called within a MonoBehaviour using
        /// <c>StartCoroutine(Utilities.WaitAndCall(2.5f, NextLevel));</c>.</remarks>
        /// <param name="delay">The delay, in seconds, before the action is executed. Must be non-negative.</param>
        /// <param name="action">The action to execute after the delay. If null, no action is performed.</param>
        /// <returns>An enumerator that can be used to control the delay and execution of the action.</returns>
        static public IEnumerator WaitAndCall(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}