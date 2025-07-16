using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Collections;

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

    public float animationDuration = 0.4f;
    public Vector3 startScale = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 endScale = Vector3.one;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Awake()
    {
        // Subscribe to events
        leaderboard.OnLeaderboardLoaded += DisplayLeaderboard;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Start hidden and small
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = startScale;
    }

    public void Start()
    {
        Hide();
    }

    public void Show(PlayerController player)
    {
        if (Visible < 0.5f)
        {
            Debug.Log("Showing panel: " + leaderboardPanel.name);
            //CanvasGroup group = leaderboardPanel.GetComponent<CanvasGroup>();
            //if (group != null)
            //{
            //    group.alpha = 1;
            //    group.interactable = true;
            //    group.blocksRaycasts = true;
            //}
            leaderboardPanel.SetActive(true);
            StartCoroutine(leaderboard.LoadLeaderboard());
            StartCoroutine(AnimateIn());
            RefreshPlayerScore(player);
        }
    }
    public void RefreshPlayerScore(PlayerController player)
    {
        bestScoreText.text = $"Your Score {player.playerLastScore} - Your Best Score: {player.playerBestScore} - Your Position: {player.playerPosition}";
    }

    public void Hide()
    {
        if (Visible >= 0.5f)
            StartCoroutine(AnimateOut());
    }

    public float Visible => canvasGroup.alpha;

    private IEnumerator AnimateIn()
    {
        float timer = 0f;

        // Initial state
        rectTransform.localScale = startScale;
        canvasGroup.alpha = 0;

        while (timer < animationDuration)
        {
            float t = timer / animationDuration;
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            canvasGroup.alpha = t;
            timer += Time.deltaTime;
            yield return null;
        }

        // Final state
        rectTransform.localScale = endScale;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator AnimateOut()
    {
        float timer = 0f;

        // Initial state
        rectTransform.localScale = endScale;
        float animateOut = animationDuration / 3f;
        while (timer < animateOut)
        {
            float t = 1f - timer / animateOut;
            rectTransform.localScale = Vector3.Lerp(startScale,endScale , t);
            canvasGroup.alpha = t;
            timer += Time.deltaTime;
            yield return null;
        }

        // Final state
        rectTransform.localScale = startScale;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
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
