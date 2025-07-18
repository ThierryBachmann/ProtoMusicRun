using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization;

public class LeaderboardDisplay : PanelDisplay
{
    [Header("Main UI")]
    public GameObject leaderboardPanel;
    public TMP_Text bestScoreText;

    [Header("Leaderboard")]
    public Transform leaderboardContentParent;
    public GameObject leaderboardEntryPrefab;
    public FirebaseLeaderboard leaderboard;


    public new void Awake()
    {
        // Subscribe to events
        leaderboard.OnLeaderboardLoaded += DisplayLeaderboard;
        base.Awake();
    }

    public new void Start()
    {
        base.Start();
    }

    public void Show(PlayerController player)
    {
        if (Visible < 0.5f)
        {
            //Debug.Log("Showing panel: " + leaderboardPanel.name);
            leaderboardPanel.SetActive(true);
            StartCoroutine(leaderboard.LoadLeaderboard());
            RefreshPlayerScore(player);
            Show();
        }
    }
    public void RefreshPlayerScore(PlayerController player)
    {
        bestScoreText.text = $"Your Score {player.playerLastScore} - Your Best Score: {player.playerBestScore} - Your Position: {player.playerPosition}";
    }

    private void DisplayLeaderboard(List<PlayerScore> scores)
    {
        Debug.Log($"=== LEADERBOARD {scores.Count} entries ===");
        ClearLeaderboard();

        foreach (var entry in scores)
        {
            var row = Instantiate(leaderboardEntryPrefab, leaderboardContentParent);
            var texts = row.GetComponentsInChildren<TMP_Text>();
            texts[0].text = entry.playerPosition.ToString();
            texts[1].text = entry.playerName;
            texts[2].text = entry.score.ToString();
            texts[3].text = entry.maxLevel.ToString();
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
            texts[4].text = dateTime.ToString($"{shortDate} HH:mm", culture);
        }
    }

    void ClearLeaderboard()
    {
        foreach (Transform child in leaderboardContentParent)
            Destroy(child.gameObject);
    }
}
