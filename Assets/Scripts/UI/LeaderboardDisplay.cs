using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject leaderboardPanel;
    public TMP_Text bestScoreText;
    public Button rerunButton, continueButton, stopButton;

    [Header("Leaderboard")]
    public Transform leaderboardContentParent;
    public GameObject leaderboardEntryPrefab;
    public FirebaseLeaderboard leaderboard;

    void Awake()
    {
        // Subscribe to events
        leaderboard.OnLeaderboardLoaded += DisplayLeaderboard;
    }

    public void Start()
    {
        //rerunButton.onClick.AddListener(OnRerun);
        //continueButton.onClick.AddListener(OnContinue);
        //stopButton.onClick.AddListener(OnStop);

        Hide(); // hide initially
    }

    public void Show(long bestScore)
    {
        Debug.Log("Showing panel: " + leaderboardPanel.name);
        CanvasGroup group = leaderboardPanel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        leaderboardPanel.SetActive(true);
        StartCoroutine(leaderboard.LoadLeaderboard());
        bestScoreText.text = $"Your Best Score: {bestScore}";
    }

    private void DisplayLeaderboard(List<PlayerScore> scores)
    {
        Debug.Log($"=== LEADERBOARD {scores.Count} entries ===");
        ClearLeaderboard();

        foreach (var entry in scores)
        {
            var row = Instantiate(leaderboardEntryPrefab, leaderboardContentParent);
            var texts = row.GetComponentsInChildren<TMP_Text>();
            texts[0].text = entry.playerName;
            texts[1].text = entry.score.ToString();
            texts[2].text = entry.maxLevel.ToString();
            // Convert timestamp to DateTime in local time
            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(entry.timestamp).LocalDateTime;
            // Get current culture (or specify one)
            CultureInfo culture = CultureInfo.CurrentCulture;
            // Get short date pattern and adjust to 2-digit year
            string shortDate = culture.DateTimeFormat.ShortDatePattern
                .Replace("yyyy", "yy")      // Replace long year with short
                .Replace("MMMM", "MM")      // Replace full month name with numeric
                .Replace("MMM", "MM");      // Replace short month name with numeric if needed

            // Format: short date with numeric month and 2-digit year, and hour:minute
            texts[3].text = dateTime.ToString($"{shortDate} HH:mm", culture);
        }
    }

    void ClearLeaderboard()
    {
        foreach (Transform child in leaderboardContentParent)
            Destroy(child.gameObject);
    }

    public void Hide() => leaderboardPanel.SetActive(false);

    void OnRerun()
    {
        Debug.Log("Re-run game");
        Hide();
        // Your game restart logic
    }

    void OnContinue()
    {
        Debug.Log("Continue game");
        Hide();
        // Resume gameplay logic
    }

    void OnStop()
    {
        Debug.Log("Stop game");
        Hide();
        // Exit or return to main menu
    }
}
