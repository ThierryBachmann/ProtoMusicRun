// Idée pour le calcul du score:
//  Score maxi si le joueur atteind l'objectif en meme temps que la fin du MIDI.
//      Example calcul ratio: pourcentage avancement du slide bar MIDI (0 a 100) / pourcentage avancement du slide bar distance (0 a 100) 
//          = 1 : score  100
//          < 1 : bonus ratio * 100
//          exemple : si objectif atteint à 80% du MIDI, 80 / 100 = 0.8, score = 0.8 * 100 = 80
//      Aspect tactique : le player peut essayer d'optimiser son score en atteignant l'objectif en provoquant des collisions
//      ou en evitant les collisions pour sécuriser un score en dessous de 100.
//  Mais si le player arrive apres la fin du MIDI : level failed (tolerance de quelques secondes?) et doit recommencer le niveau. 
//  Les obstacles et bonus ne sont régénérés, le player peut donc apprendre à optimiser son trajet.
//
//  Les bonus :
//      - transposition de la musique d'une octave pendant 10 secondes vers le haut. Si pas de collision pendant cette période, le bonus
//        est calculé en fonction du pourcentage de la musique jouée pendant cette période.
//        max : 20 points pour 10 secondes de musique jouée sans collision. Le joueur va donc éviter une collision pour obtenir du bonus.
//      - bonus speed multiplicateur pendant 10 secondes. 
//  Les malus :
//      - transposition de la musique d'une octave pendant 10 secondes vers le bas. Si pas de collision pendant cette période, le malus
//        est calculé en fonction du pourcentage de la musique jouée pendant cette période.
//        max : -20 points pour 10 secondes de musique jouée sans collision. Le joueur va donc provoquer une collision pour éviter le malus.

//  Non, Score plus élevé si temps de trajet plus court (utilisation de speedMultiplier qui agit aussi sur la vitesse MIDI?)
//      cela favorise l'évitement des colisions et le chemin le plus direct

//if (!player.goalHandler.goalReached)
//{
//    float bonusDirection = player.goalHandler.goalAngle >= -15f && player.goalHandler.goalAngle <= 15f ? 1f : -1f;
//    score += (long)(bonusDirection * player.speedMultiplier * coefficient);
//    if (score < 0) score = 0;
//}
using System;
using UnityEngine;

namespace MusicRun
{
    public class BonusManager : MonoBehaviour
    {

        public float bonusInProgress;
        public DateTime startBonusDateTime;
        public bool startBonus;
        public float durationBonus = 5f;
        public float valueBonus = 20f;

        private GameManager gameManager;
        private MidiManager midiManager;

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            midiManager = gameManager.midiManager;
        }
        void Start()
        {
            startBonusDateTime = DateTime.MaxValue;
            bonusInProgress = 0;
            startBonus = false;
        }
        void Update()
        {
            if (startBonus)
            {
                // increase bonus each delta time 
                bonusInProgress = ((float)(DateTime.Now - startBonusDateTime).TotalMilliseconds / 1000f * valueBonus) / durationBonus;
                if ((DateTime.Now - startBonusDateTime).TotalMilliseconds > durationBonus * 1000f)
                {
                    EndBonus();
                }
            }
        }

        public void TriggerBonus(Collider collider)
        {
            if (collider.CompareTag("Malus"))
                valueBonus = -20f;
            else
                valueBonus = 20f;
            StartBonus();

            Rigidbody rb = collider.attachedRigidbody;
            if (rb != null)
            {
                // Direction from player to bonus
                Vector3 kickDir = (collider.transform.position - transform.position).normalized;
                kickDir.y = 0;
                // Add a forward + upward impulse (like a foot kick)
                Vector3 force = kickDir * gameManager.playerController.Speed * 2f + Vector3.up * 8f;
                rb.AddForce(force, ForceMode.Impulse);
                rb.useGravity = true;
                // Optional: add spin
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
            Destroy(collider.gameObject, 3f);
        }

        public void StartBonus()
        {
            Debug.Log($"Start bonus Trans {valueBonus}");
            if (valueBonus > 0)
                midiManager.TransposeAdd(6);
            else
                midiManager.TransposeAdd(-6);
            startBonusDateTime = DateTime.Now;
            startBonus = true;
        }

        public void EndBonus()
        {
            if (startBonus)
            {
                Debug.Log("End bonus Trans");

                startBonus = false;
                midiManager.TransposeClear();

                if (bonusInProgress > 0f)
                {
                    gameManager.scoreManager.ScoreBonus += Mathf.RoundToInt(bonusInProgress);
                    bonusInProgress = 0f;
                }
            }
        }

    }
}