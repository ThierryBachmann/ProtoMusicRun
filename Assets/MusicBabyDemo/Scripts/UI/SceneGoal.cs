// Display a popup screen before the player starts to run.
// Some information about the level is provided from the TerrainGenerator.
using TMPro;

namespace MusicRun
{

    public class SceneGoal : PanelDisplay
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI infoText;


        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            base.Start();
        }

        /// <summary>
        /// The gameManager is setting information when CreateAndStartLevel() is run.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="info"></param>
        public void SetInfo(string title, string info)
        {
            titleText.text=title;
            infoText.text=info;
        }
    }
}