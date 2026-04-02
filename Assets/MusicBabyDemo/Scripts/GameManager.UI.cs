namespace MusicRun
{
    /*
     * FILE ROLE
     * - UI popup utilities part of GameManager (partial class).
     * - Centralizes generic show/hide behavior for overlay screens.
     * - Keeps popup control logic separated from core level flow.
     */
    public partial class GameManager
    {
        public void HideAllPopups()
        {
            splashScreen.Hide();
            helperScreen.Hide();
            settingScreen.Hide();
            levelFailedScreen.Hide();
            leaderboardDisplay.Hide();
            sceneGoal.Hide();
        }

        public void LeaderboardSwitchDisplay()
        {
            HideAllPopups();
            leaderboardDisplay.SwitchVisible(playerController);
        }

        public void HelperScreenDisplay()
        {
            HideAllPopups();
            helperScreen.SwitchVisible();
        }

        public void SplashScreenDisplay()
        {
            HideAllPopups();
            splashScreen.SwitchVisible();
        }
    }
}
