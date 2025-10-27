// Score calculation idea:
//
// Maximum score if the player reaches the goal at the same time as the MIDI ends.
// Example ratio calculation: (music progress percentage 0..100) / (distance progress percentage 0..100)
//     = 1  => score 100
//     < 1 => score = ratio * 100
// Example: if the goal is reached at 80% of the MIDI, 80 / 100 = 0.8 => score = 0.8 * 100 = 80
//
// Tactical aspect:
//     - The player can try to optimize score by causing collisions or avoiding collisions to secure a score below 100.
//     - If the player reaches the goal after the MIDI ended: level failed (maybe allow a few seconds tolerance?) and the level must be restarted.
//     - Obstacles and bonuses are not regenerated when retrying, so the player can learn an optimal path.
//
// Bonus examples:
//     - Upward transposition of the music by an octave for 10 seconds. If there are no collisions during that period,
//       the bonus is calculated based on the percentage of music played during the bonus window.
//       Max: +20 points for 10 seconds of uninterrupted music. The player will avoid collisions to get this bonus.
//     - Speed multiplier bonus for 10 seconds.
//
// Malus examples:
//     - Downward transposition of the music by an octave for 10 seconds. If there are no collisions during that period,
//       the malus is calculated based on the percentage of music played during the malus window.
//       Max: -20 points for 10 seconds of uninterrupted music. The player may provoke collisions to avoid the malus.
//
// Alternative scoring idea:
//     - Higher score for shorter travel time (using speedMultiplier which also affects MIDI speed?)
//       This favors collision avoidance and taking the most direct path.
//
// Example snippet (kept for reference):
// if (!player.goalHandler.goalReached)
// {
//     float bonusDirection = player.goalHandler.goalAngle >= -15f && player.goalHandler.goalAngle <= 15f ? 1f : -1f;
//     score += (long)(bonusDirection * player.speedMultiplier * coefficient);
//     if (score < 0) score = 0;
// }

using System;
using System.Collections;
using UnityEngine;

namespace MusicRun
{
    /// <summary>
    /// Manages temporary bonuses and maluses triggered by player interaction with world objects.
    /// Responsible for applying a temporary musical transpose, computing progressive bonus value
    /// and applying simple physics feedback on triggered objects.
    /// </summary>
    public class BonusManager : MonoBehaviour
    {
        /// <summary>
        /// Current accumulated bonus value during the active bonus window (progressive from 0 up to <see cref="valueBonus"/>).
        /// For malus (negative) this will go down toward the negative <see cref="valueBonus"/>.
        /// </summary>
        public float bonusInProgress;

        /// <summary>
        /// UTC DateTime when the current bonus started. Use <see cref="DateTime.MaxValue"/> when no bonus is active.
        /// </summary>
        public DateTime startBonusDateTime;

        /// <summary>
        /// True while a bonus/malus period is active.
        /// </summary>
        public bool startBonus;

        /// <summary>
        /// Duration of the bonus/malus window in seconds.
        /// </summary>
        public float durationBonus = 5f;

        /// <summary>
        /// Total value to award for a completed bonus window. Positive for bonus, negative for malus.
        /// </summary>
        public float valueBonus = 20f;

        public GameObject goal;

        private GameManager gameManager;
        private MidiManager midiManager;

        /// <summary>
        /// Resolve references to GameManager and MidiManager on Awake.
        /// </summary>
        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            midiManager = gameManager.midiManager;
        }

        /// <summary>
        /// Initialize internal state.
        /// </summary>
        void Start()
        {
            // No bonus active by default.
            startBonusDateTime = DateTime.MaxValue;
            bonusInProgress = 0;
            startBonus = false;
        }

        /// <summary>
        /// Update the progressive bonus value while a bonus is active and end it when its duration elapses.
        /// </summary>
        void Update()
        {
            if (startBonus)
            {
                // Increase bonus progressively according to elapsed time.
                bonusInProgress = ((float)(DateTime.Now - startBonusDateTime).TotalMilliseconds / 1000f * valueBonus) / durationBonus;

                // End the bonus when elapsed time exceeds configured duration.
                if ((DateTime.Now - startBonusDateTime).TotalMilliseconds > durationBonus * 1000f)
                {
                    EndBonus();
                }
            }
        }

        /// <summary>
        /// Trigger a generic bonus or malus when the player collides with a bonus object.
        /// Applies a physics impulse to the object's rigidbody (visual feedback) and schedules it for destruction.
        /// Also starts the musical transpose effect.
        /// </summary>
        /// <param name="collider">Collider of the bonus/malus object.</param>
        public void TriggerBonus(Collider collider)
        {
            // Decide whether this is a malus or a bonus based on tag.
            if (collider.CompareTag("Malus"))
                valueBonus = -20f;
            else
                valueBonus = 20f;
            StartBonus();

            Rigidbody rb = collider.attachedRigidbody;
            if (rb != null)
            {
                // Direction from player to the bonus object (horizontal only).
                Vector3 kickDir = (collider.transform.position - gameManager.playerController.transform.position).normalized;
                kickDir.y = 0;

                // Add a forward + upward impulse to the object (like a kick).
                // Force scales with player speed to make the interaction feel consistent.
                Vector3 force = kickDir * gameManager.playerController.Speed * 2f + Vector3.up * 8f;
                rb.AddForce(force, ForceMode.Impulse);
                rb.useGravity = true;

                // Add a random spin for visual variety.
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }

            // Remove the bonus object after a short delay to allow physics/animation to play.
            Destroy(collider.gameObject, 3f);
        }

        /// <summary>
        /// Trigger an instrument pickup. Provides a lighter impulse (no gravity) and restores one instrument channel in the MIDI.
        /// Also triggers a UI feedback blink on the instrument header item.
        /// </summary>
        /// <param name="collider">Collider of the instrument pickup object.</param>
        public void TriggerInstrument(Collider collider)
        {
            Debug.Log($"Bonus - TriggerInstrument:{collider.transform.name}");

            if (collider.attachedRigidbody != null)
            {
                // Start coroutine to move toward the goal
                StartCoroutine(MoveTowardGoal(collider));

                // Restore one MIDI channel's original instrument preset.
                gameManager.midiManager.RestoreMidiChannel();

                // Provide UI feedback: blink the instrument item background in green.
                gameManager.headerDisplay.itemInstrument.BlinkBackground(Utilities.ColorGreen, 3f, 0.1f);
            }
        }

        private IEnumerator MoveTowardGoal(Collider collider)
        {
            float speed = 15f; // Units per second
            float stopDistance = 1f;
            Rigidbody rb = collider.attachedRigidbody;
            GameObject obj = collider.gameObject;


            // Direction from player to the instrument (horizontal only).
            Vector3 kickDir = (obj.transform.position - gameManager.playerController.transform.position).normalized;
            kickDir.y = 0;

            // Add a gentle forward + slight upward impulse and keep the object floating (no gravity).
            Vector3 force = kickDir * gameManager.playerController.Speed * 2f + Vector3.up * 1f;
            rb.AddForce(force, ForceMode.Impulse);
            
            // Disable gravity so the object moves in a controlled straight line
            rb.useGravity = false;

            // Add spin for visual effect.
            //rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);

            yield return new WaitForSeconds(1f);

            // Disable any residual movement from physics before starting
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Prevent collisions with the goal to avoid unwanted rebounds
            Physics.IgnoreCollision(collider, goal.GetComponent<Collider>(), true);

            Vector3 goalPos = goal.transform.position;
            goalPos.y = 1f;

            // Use FixedUpdate timing for smooth, physics-consistent motion
            while (true)
            {
                Vector3 direction = goalPos - obj.transform.position;
                float distance = direction.magnitude;

                // Reset any physics-driven motion every frame to avoid drifts or rebounds
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                Debug.Log($"Bonus - Goal:{goalPos}  Bonus:{obj.transform.position} Dir:{direction} Dist:{distance} {obj.name}");
                if (distance < stopDistance)
                    break;

                // Wait for the next physics step instead of every rendered frame
                rb.MovePosition(rb.position + direction.normalized * speed * Time.deltaTime);


                yield return new WaitForFixedUpdate();
            }

            //float forceMagnitude = 0.05f; // Adjust for desired speed
            //rb.AddForce((goalPos - obj.transform.position) * forceMagnitude, ForceMode.Impulse);

            //while (true)
            //{
            //    Vector3 direction = goalPos - obj.transform.position;
            //    float distance = direction.magnitude;
            //    Debug.Log($"Bonus - Goal:{goalPos}  Bonus:{obj.transform.position} Dir:{direction} Dist:{distance} {obj.name}");
            //    if (distance < stopDistance)
            //        break;

            //    rb.AddForce(direction * forceMagnitude, ForceMode.Impulse);
            //    yield return null;
            //}

            Debug.Log($"Bonus - Destroy Instrument {obj.name}");
            //rb.isKinematic = true;
            Destroy(obj);
        }

        /// <summary>
        /// Start the bonus/malus period: apply a transpose to the MIDI and mark the start time.
        /// Positive <see cref="valueBonus"/> transposes up, negative transposes down.
        /// </summary>
        public void StartBonus()
        {
            Debug.Log($"Bonus - start Trans {valueBonus}");
            if (valueBonus > 0)
                midiManager.TransposeAdd(6); // transpose up one octave (approx. 6 semitones * 2 = octave depending on game convention)
            else
                midiManager.TransposeAdd(-6); // transpose down
            startBonusDateTime = DateTime.Now;
            startBonus = true;
        }

        /// <summary>
        /// End the current bonus/malus period: clear transpose and apply the accumulated bonus to the score.
        /// </summary>
        public void EndBonus()
        {
            if (startBonus)
            {
                Debug.Log("Bonus - end Trans");

                startBonus = false;
                midiManager.TransposeClear();

                // Award positive bonus only (malus applied by negative valueBonus already affects game rules if needed).
                if (bonusInProgress > 0f)
                {
                    gameManager.scoreManager.ScoreBonus += Mathf.RoundToInt(bonusInProgress);
                    bonusInProgress = 0f;
                }
            }
        }
    }
}