/*
 * Hold a VideoPlayer to play success or failed video.
 * A 5-second looping 1.85:1 video of a cartoon pop band performing on stage. The band consists of four anthropomorphic animals: 
 * a drummer, a guitarist, a singer, and a bass player. Each character has a unique, expressive design inspired by modern pop animation, 
 * with lively movements and rhythmic energy. The scene features warm stage lighting in pink and blue tones, with spotlights that pulse gently to the beat, 
 * and energetic crowd silhouettes in the background. 
 * The camera slowly pans across the band as they play, creating a smooth, seamless loop. 
 * The visual style is colorful, slightly exaggerated, and vibrant. Perfectly looping animation.
 * */

using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class ScreenDisplay : MonoBehaviour
{
    public float duration = 1.5f;
    public float startY = -1.5f;
    public float riseY = 2f;
    public VideoPlayer playerVideo;
    private string[] video;

    // Animation curve for a smooth motion (starts fast, ends slow)
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public void Awake()
    {
        video = new string[2];
        video[0] = "https://mptkapi.paxstellar.com/MusicRunBaby-L1-Failed.mp4";
        video[1] = "https://mptkapi.paxstellar.com/MusicRunBaby-L1-Success.mp4";
    }

    /// <summary>
    /// Resets the vertical position of the game object to its initial starting value.
    /// </summary>
    /// <remarks>This method modifies the y-coordinate of the game object's position, setting it to the
    /// predefined starting height. It does not affect the x or z coordinates.</remarks>
    public void ResetPosition()
    {
        Vector3 pos = gameObject.transform.position;
        pos.y = startY;
        gameObject.transform.position = pos;
    }

    /// <summary>
    /// Plays the video at the specified index in the playlist.
    /// </summary>
    /// <remarks>The video will play in a looping mode. Ensure that the index is valid to avoid runtime
    /// exceptions.</remarks>
    /// <param name="index">The zero-based index of the video to play. Must be within the bounds of the playlist.</param>
    public void PlayVideo(int index)
    {
        playerVideo.url = video[index];
        playerVideo.isLooping = true;
        playerVideo.Play();
    }
    /// <summary>
    /// Stops the currently playing video.
    /// </summary>
    /// <remarks>This method halts video playback immediately. Ensure that the video player is initialized and
    /// a video is currently playing before calling this method.</remarks>
    public void StopVideo()
    {
        playerVideo.Stop();
    }

    /// <summary>
    /// When player reaches the goal, the video screen is slowly rise from the ground.
    /// </summary>
    public void Rise()
    {
        StartCoroutine(RiseCoroutine());
    }

    private IEnumerator RiseCoroutine()
    {
        float elapsed = 0f;
        // Reminder,
        // for child GameObjects, the "Position" field in the Unity Inspector shows the value of transform.localPosition, not transform.position.
        // But World position (absolute in the scene) by script. So with this script we are dealing with world position.
        Vector3 pos = gameObject.transform.position;
        Debug.Log($"RiseCoroutine Start {gameObject.name} at {pos} from: {startY} to: {riseY}");
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = riseCurve.Evaluate(t);

            pos.y = startY + (riseY - startY) * easedT;
            gameObject.transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"RiseCoroutine End {gameObject.name} at {pos} from: {startY} to: {riseY}");
        pos.y = riseY;
        gameObject.transform.position = pos;
    }
}
