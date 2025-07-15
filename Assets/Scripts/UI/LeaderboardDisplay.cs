using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

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
            //texts[2].text = DateTimeOffset.FromUnixTimeMilliseconds(entry.timestamp).ToString();
            //texts[3].text = entry.maxLevel.ToString();
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
