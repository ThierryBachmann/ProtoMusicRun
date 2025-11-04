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
        public void SetInfo(string title, string info)
        {
            titleText.text=title;
            infoText.text=info;
        }
    }
}