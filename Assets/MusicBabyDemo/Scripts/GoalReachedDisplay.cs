using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace MusicRun
{
    public class GoalReachedDisplay : MonoBehaviour
    {
        public ScreenDisplay ScreenVideo;
        public ScreenDisplay BorderVideo;

        //public TMP_Text bestScoreText;
        public TMP_Text midiInfoDisplayed;

        [Tooltip("Bounciness value (0-1) for the Physic Material.")]
        public float bounciness = 0.5f;

        [Tooltip("Friction value for the Physic Material.")]
        public float friction = 0.6f;

        [Tooltip("Size of the Box Collider.")]
        public Vector3 colliderSize = new Vector3(2.5f, 1.7f, 0.5f);

        [Tooltip("Center of the Box Collider.")]
        public Vector3 colliderCenter = new Vector3(0f, -0.7f, 0f);

        [Tooltip("Air resistance (slows down movement).")]
        public float linearDamping = 0.5f;

        [Tooltip("Rotational resistance")]
        public float angularDamping = 0.5f;

        private GameManager gameManager;

        private Color colorTextGreen = new Color(0.07843138f, 0.5960785f, 0.1607843f, 1f);
        private Color colorTextGray = new Color(0.1764706f, 0.1764706f, 0.1764706f, 1f);

        public void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
        }


        public void HideVideo()
        {
            ScreenVideo.ResetPosition();
        }

        public void RiseVideo(int indexVideo)
        {
            ScreenVideo.PlayVideo(indexVideo);
            ScreenVideo.Rise();
        }

        public void UpdateText(string info = null)
        {
            //// "   9999         9999            999"
            //// "  9999       9999         9999
            //bestScoreText.text = $" {player.playerLastScore,4}       {player.playerBestScore,4}            {player.playerPosition,4}";
            string midiInfo = "";
            if (gameManager.midiManager.midiPlayer != null)
                midiInfo = gameManager.midiManager.midiPlayer.MPTK_MidiName;
            if (!gameManager.levelFailed)
            {
                midiInfoDisplayed.color = colorTextGreen;
                midiInfo += $" Score: {gameManager.playerController.playerLastScore,4}";
            }
            else
                midiInfoDisplayed.color = colorTextGray;

            if (info != null)
                midiInfo += "\n" + info;

            //if (gameManager.midiManager.midiPlayer.MPTK_MidiLoaded != null)
            //{
            //    if (!string.IsNullOrEmpty(gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.TrackInstrumentName))
            //        midiInfo += "\n" + gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.TrackInstrumentName;
            //    if (!string.IsNullOrEmpty(gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.Copyright))
            //        midiInfo += "\n" + gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.Copyright;
            //    // SequenceTrackName ProgramName    TrackInstrumentName
            //}
            midiInfoDisplayed.text = midiInfo;
        }

        public void FallingVideo(int indexVideo)
        {
            Debug.Log($"GoalReachedDisplay {gameObject.name}");
            // Add a Box Collider if the parent doesn't have one
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                // Set a default size (adjust as needed)
                boxCollider.center = colliderCenter;
                boxCollider.size = colliderSize;
            }

            // Add a Rigidbody to the parent GameObject
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            // Configure the Rigidbody for realistic physics
            rb.mass = 1f;                       // Mass of the object
            rb.linearDamping = linearDamping;   // Air resistance (slows down movement)
            rb.angularDamping = angularDamping; // Rotational resistance
            rb.useGravity = true;          // Enable gravity
            rb.isKinematic = false;        // Disable kinematic mode

            // Optional: Freeze rotation to prevent unwanted tilting
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // Create and assign a Physic Material for bounce
            PhysicsMaterial physicMaterial = new PhysicsMaterial
            {
                bounciness = bounciness,
                dynamicFriction = friction,
                staticFriction = friction,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Average
            };

            // Apply the Physic Material to the collider
            boxCollider.material = physicMaterial;
            ScreenVideo.PlayVideo(indexVideo);
        }
    }
}