using MusicRun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleItem : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI valueText;

    private Image imageBackground;
    private Color backgroundColor;

    void Awake()
    {
        imageBackground = GetComponent<Image>();
        backgroundColor = imageBackground.color;
    }

    public void SetColor(Color color)
    {
        imageBackground.color = color;
    }
    public void ResetColor()
    {
        imageBackground.color = backgroundColor;
    }

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetValue(string value)
    {
        valueText.text = value;
    }
}
