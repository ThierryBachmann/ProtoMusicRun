using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicRun
{
    /// <summary>
    /// on calcule à chaque frame la composante du déplacement qui rapproche vraiment le joueur du but.
    /// Principe en une ligne
    ///     Score += projection du déplacement sur la direction “Start → Goal” × multiplicateur
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {

        public int ScoreLevel;
        public int ScoreGoal;
        public int ScoreOverall;
        public int ScoreBonus;

        private GameManager gameManager;
        private PlayerController player;

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.Player;
        }

        void Update()
        {
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
        }

        public void CalculateScoreLevel(float musicProgress, float distanceProgress)
        {
            ScoreGoal = CalculateScoreGoal(musicProgress, distanceProgress);
            ScoreLevel = ScoreGoal + ScoreBonus;
            ScoreOverall += ScoreGoal;
            Debug.Log($"CalculateLevelScore ScoreGoal: {ScoreGoal} ScoreBonus: {ScoreBonus} ScoreLevel: {ScoreLevel} ScoreOverall:{ScoreOverall}");
        }

        public int CalculateScoreGoal(float musicProgress, float distanceProgress)
        {
            int score = 0;
            if (distanceProgress <= 0)
            {
                //Debug.Log("CalculateScoreGoal - Distance progress is zero or negative, cannot calculate score.");
            }
            else if (Mathf.Abs(musicProgress - distanceProgress) < 2f)
            {
                // If the music and distance progress are very close, we assume the level is completed successfully.
                //Debug.Log($"CalculateScoreGoal Close to goal:{score} musicProgress:{musicProgress:N1} distanceProgress:{distanceProgress:N1} Delta:{Mathf.Abs(musicProgress - distanceProgress):N1}");
                score = 100;
            }
            else if (distanceProgress > musicProgress * 1.2f)
            {
                //Debug.Log($"CalculateScoreGoal far away to goal:{score} musicProgress:{musicProgress:N1} distanceProgress:{distanceProgress:N1}");
                score = 0;
            }
            else
            {
                // Calculate score based on the ratio of music progress to distance progress
                float ratio = distanceProgress/musicProgress;
                score = Mathf.RoundToInt(100f * ratio);
                score = Mathf.Clamp(score, 10, 100);
                //Debug.Log($"CalculateScoreGoal ScoreGoal:{score} musicProgress:{musicProgress:N1} distanceProgress:{distanceProgress:N1} Ratio:{ratio:N1}");
            }
            return score;
        }

        public Color CalculateColor()
        {
            Color targetColor;
            if (ScoreGoal >= 100) targetColor = Utilities.ColorGreen;
            else if (ScoreGoal >= 50) targetColor = Utilities.ColorWarning;
            else if (ScoreGoal >= 20) targetColor = Utilities.ColorWarning;
            else if (ScoreGoal >= 0) targetColor = Utilities.ColorAlert;
            else targetColor = Utilities.ColorWarning;
            return targetColor;
        }
    }
}