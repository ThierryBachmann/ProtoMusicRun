using UnityEngine;

namespace MusicRun
{
    /*
     * FILE ROLE
     * - Runtime helper part of GameManager (partial class).
     * - Contains debug shortcut handling, in-level progression/score refresh,
     *   and goal screen instance/show-hide utility logic.
     * - Keeps the main GameManager file focused on high-level flow.
     */
    public partial class GameManager
    {
        private void GoalScreenHide()
        {
            if (goalReachedClone != null)
            {
                Debug.Log("-level- Destroy(goalReachedDisplayClone)");
                Destroy(goalReachedClone.gameObject);
                goalReachedClone = null;
                return;
            }

            Debug.Log("-level- goalReachedDisplay.HideVideo");
            goalReachedDisplay.ScreenVideo.StopVideo();
            goalReachedDisplay.HideVideo();
        }

        private void HandleDebugShortcuts()
        {
            if (!enableShortcutKeys || !Input.anyKeyDown)
                return;

            foreach (char key in Input.inputString)
                HandleDebugShortcutKey(key);
        }

        private void HandleDebugShortcutKey(char key)
        {
            if (key == 'h')
                HelperScreenDisplay();
            else if (key == 'n')
            {
                midiManager.InstrumentRestored = midiManager.InstrumentFound;
                playerController.TeleportToGoal();
            }
            else if (key == 's')
                GameStop();
            else if (key == 'r')
                DebugSpawnGoalReachedClone();
            else if (key == 'a')
                actionGame.SwitchVisible();
            else if (key == 'l')
                LeaderboardSwitchDisplay();
            else if (key == 'm')
                midiManager.SoundOnOff();
            else if (key == 'g')
            {
                terrainGenerator.ClearChunks(0);
                terrainGenerator.UpdateChunks(playerController.CurrentPlayerChunk);
            }
            else if (key == 'x')
                DebugDestroyGoalReachedClone();
            else if (key == 'p')
                playerController.TeleportToStart();
        }

        private void DebugSpawnGoalReachedClone()
        {
            Debug.Log("-debug- Instantiate(goalReachedDisplayClone); ");
            playerController.enableMovement = false;
            Transform player = playerController.transform;
            Vector3 spawnPos = player.position + player.forward * VideoScreenDistance + player.up * 1f;
            Quaternion rot = Quaternion.LookRotation(player.forward, Vector3.up);
            GameObject go = Instantiate(VideoScreenPrefab, spawnPos, rot);
            goalReachedClone = go.GetComponent<GoalReachedDisplay>();
            goalReachedClone.FallingVideo(0);
        }

        private void DebugDestroyGoalReachedClone()
        {
            if (goalReachedClone == null)
                return;

            Debug.Log("-debug- Destroy(goalReachedDisplayClone); ");
            Destroy(goalReachedClone.gameObject);
        }

        private void UpdateLevelProgressAndScore()
        {
            if (!gameRunning || !levelRunning || levelPaused)
                return;

            if (goalHandler.distanceAtStart > 0)
                GoalPercentage = 100f - (goalHandler.distancePlayerGoal / goalHandler.distanceAtStart * 100f);
            else
                GoalPercentage = 0f;

            scoreManager.ScoreGoal = scoreManager.CalculateScoreGoal(midiManager.Progress, GoalPercentage);
        }
    }
}
