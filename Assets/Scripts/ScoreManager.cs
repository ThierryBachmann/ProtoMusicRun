using UnityEngine;

namespace MusicRun
{
    /// <summary>
    /// on calcule à chaque frame la composante du déplacement qui rapproche vraiment le joueur du but.
    /// Principe en une ligne
    ///     Score += projection du déplacement sur la direction “Start → Goal” × multiplicateur
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public PlayerController player;

        public long score;

        void Update()
        {
            // Idée pour le calcul du score:
            //  bonus si arrivée a la fin du MIDI avec tolerance
            //  Si arrivée apres la fin du MIDI : level failed
            //  score plus elevé si temps de trajet plus court (utilisation de speedMultiplier qui agit aussi sur la vitesse MIDI?)
            //      cela favorise l'évitement des colisions et le chemin le plus direct
            //      Example calcul: temps théorique du MIDI (donc avec speed=1) / temps dans le level
            //          < 1 : failed
            //          =1  : bonus 100
            //          >1  : bonus ratio * 100
            //      score de base si goal atteind : 100, variable par level ?
            //  

            //if (!player.goalHandler.goalReached)
            //{
            //    float bonusDirection = player.goalHandler.goalAngle >= -15f && player.goalHandler.goalAngle <= 15f ? 1f : -1f;
            //    score += (long)(bonusDirection * player.speedMultiplier * coefficient);
            //    if (score < 0) score = 0;
            //}
        }

        public int GetDisplayedScore() => Mathf.FloorToInt(score);
    }
}