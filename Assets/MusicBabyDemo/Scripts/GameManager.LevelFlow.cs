using UnityEngine;

namespace MusicRun
{
    /*
     * FILE ROLE
     * - Level flow part of GameManager (partial class).
     * - Contains level lifecycle orchestration: create/restart/start/complete.
     * - Keeps high-level transitions grouped in one file for easier onboarding.
     */
    public partial class GameManager
    {
        /// <summary>
        /// Create and start a new level or restart the same level (preserve generated chunks).
        /// </summary>
        /// <param name="index"></param>
        /// <param name="restartSame"></param>
        private void LevelCreate(int index, bool restartSame = false)
        {
            Debug.Log($"-level- LevelCreate index:{index}");
            HideAllPopups();

            GoalScreenHide();

            scoreManager.ScoreLevel = 0;
            levelFailed = false;
            actionGame.Hide();
            HideAllPopups();

            // Need to get the level description before to get information about instrument in CreateLevel.
            midiManager.LoadMIDI(terrainGenerator.levels[index]);
            midiManager.BuildMidiChannel(terrainGenerator.levels[index]);

            if (restartSame)
            {
                // No terrain created, reuse same but teleport player to the start
                playerController.TeleportToStart();
            }
            else
            {
                // Player continue at the same place,
                // but new terrain is created. Keep previous terrain at a distance to 1.5 chunk
                terrainGenerator.ClearChunks(1.5f);
                terrainGenerator.CreateLevel(index);
            }

            // Player move to start terrain facing to goal terrain
            //playerController.ResetPosition();

            //goalHandler.gameObject.SetActive(true);
            goalHandler.NewLevel();
            gameRunning = true;
            OnSwitchPause(true);
            playerController.LevelStarted();
            sceneGoal.SetInfo(terrainGenerator.CurrentLevel.name, terrainGenerator.CurrentLevel.description);
            awaitingPlayerStart = true;
            sceneGoal.Show(LevelStart);

            // The scene must be loaded before playing the MIDI.
            // PlayMIDI will also trigger midiPlayer.OnEventStartPlayMidi
            // which will set midiPlayer.MPTK_MaxDistance (apply volume attenuation with distance)
            midiManager.PlayMIDI();
        }

        private void LevelStart(bool unsused)
        {
            Debug.Log($"-level- StartLevel awaitingPlayerStart:{awaitingPlayerStart}");
            if (awaitingPlayerStart)
            {
                actionLevel.Show();
                OnSwitchPause(false);
                awaitingPlayerStart = false;
                levelRunning = true;
            }
        }

        /// <summary>
        /// Handles the completion of a level, updating game state and UI elements accordingly.
        /// Triggered from GoalHandler when player is close to the goal.
        /// </summary>
        /// <param name="reason">Reason why the level flow reached completion.</param>
        private void OnLevelCompleted(LevelEndedReason reason)
        {
            Debug.Log($"GameManager - OnLevelCompleted - {reason} alwaysSucceed:{alwaysSucceed}");

            // Check level failed: Music ended without reaching the goal.
            playerController.enableMovement = false;
            levelRunning = false;
            if (MusicPercentage >= 100f && GoalPercentage <= 98f && !alwaysSucceed)
            {
                Debug.Log("GameManager - OnLevelCompleted - Music ended without reaching the goal.");
                levelFailed = true;

                // Instantiate prefab in front of the player.
                Transform p = playerController.transform;
                Vector3 spawnPos = p.position + p.forward * VideoScreenDistance + p.up * 10f;
                Quaternion rot = Quaternion.LookRotation(p.forward, Vector3.up);
                GameObject go = Instantiate(VideoScreenPrefab, spawnPos, rot);
                goalReachedClone = go.GetComponent<GoalReachedDisplay>();
                goalReachedClone.FallingVideo(0);
                goalReachedClone.UpdateText("Level Failed - Music Ended");
            }
            else if (midiManager.InstrumentRestored < midiManager.InstrumentFound && !alwaysSucceed)
            {
                Debug.Log("GameManager - OnLevelCompleted - Music ended without reaching the goal.");
                levelFailed = true;
                goalReachedDisplay.RiseVideo(0);
                goalReachedDisplay.UpdateText("Level Failed - Instruments Missing");
            }
            else
            {
                levelFailed = false;
                goalReachedDisplay.RiseVideo(1);
                goalReachedDisplay.UpdateText();
            }

            bonusManager.EndBonus();
            actionLevel.Hide();
            actionGame.Show();
            actionGame.SelectActionsToShow();

            scoreManager.CalculateScoreLevel(MusicPercentage, GoalPercentage);
            LeaderboardPlayerScore playerScore = new LeaderboardPlayerScore(
                        leaderboard.firebaseAuth.GetUserId(),
                        playerController.playerName,
                        scoreManager.ScoreLevel,
                        999,
                        999,
                        999,
                        1
                        );
            leaderboard.SubmitScore(playerScore);
            playerController.LevelEnded();
            StartCoroutine(Utilities.WaitAndCall(2000f, () => { playerController.enableMovement = true; }));
        }
    }
}
