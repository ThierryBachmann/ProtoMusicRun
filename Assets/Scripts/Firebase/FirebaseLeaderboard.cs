using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class FirebaseLeaderboard : MonoBehaviour
{
    public FirebaseAuth firebaseAuth;
    [Header("Firebase Configuration")]
    public string firebaseURL = "https://protorunmusic-default-rtdb.europe-west1.firebasedatabase.app/";
    public string leaderboardNode = "leaderboard";
    public int maxLeaderboardEntries = 100;

    [Header("Events")]
    public System.Action<List<PlayerScore>> OnLeaderboardLoaded;
    public System.Action<bool> OnScoreSubmitted;

    void Awake()
    {
        firebaseAuth.OnAuthenticationComplete += OnAuthComplete;
    }
    void Start()
    {
        // Load leaderboard on start
        StartCoroutine(LoadLeaderboard());
    }

    private void OnAuthComplete(bool success)
    {
        if (success)
        {
            Debug.Log("Ready to submit scores!");
            //// Load leaderboard on start
            //StartCoroutine(LoadLeaderboard());
            // Now you can safely submit scores
        }
    }
    public IEnumerator LoadLeaderboard()
    {
        // Use Firebase's built-in ordering, no auth needed, score are readable for anonymous
        string orderBy = Uri.EscapeDataString("\"score\"");
        string url = $"{firebaseURL}{leaderboardNode}.json?&orderBy={orderBy}&limitToLast={maxLeaderboardEntries}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;

            if (string.IsNullOrEmpty(jsonResponse) || jsonResponse == "null")
            {
                OnLeaderboardLoaded?.Invoke(new List<PlayerScore>());
                yield break;
            }

            // Simple approach: Parse as individual entries
            List<PlayerScore> scores = ParseFirebaseResponse(jsonResponse);

            // Since Firebase returns in ascending order, reverse for descending
            scores.Reverse();

            OnLeaderboardLoaded?.Invoke(scores);
            Debug.Log($"Loaded {scores.Count} leaderboard entries");
        }
        else
        {
            Debug.LogError($"Failed to load leaderboard: {request.error}");
            OnLeaderboardLoaded?.Invoke(new List<PlayerScore>());
        }
    }
    /// <summary>
    /// Firebase Realtime Database returns data in this format:
    /// {
    /// "user1_id": {"playerName": "Player1", "score": 1000, ...},
    /// "user2_id": {"playerName": "Player2", "score": 800, ...}
    /// }
    /// The goal is to convert this into a List<PlayerScore> so we can sort and display the leaderboard.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    private List<PlayerScore> ParseFirebaseResponse(string jsonResponse)
    {
        List<PlayerScore> scores = new List<PlayerScore>();

        try
        {
            // Remove outer braces
            string content = jsonResponse.Trim();
            if (content.StartsWith("{") && content.EndsWith("}"))
            {
                content = content.Substring(1, content.Length - 2);
            }
            if (string.IsNullOrEmpty(content))
                return scores;

            // Split by entries and parse each one  
            string[] entries = SplitJsonEntries(content);

            foreach (string entry in entries)
            {
                try
                {
                    // Find the value part (after the key and colon)
                    int colonIndex = entry.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string valueJson = entry.Substring(colonIndex + 1).Trim();
                        PlayerScore score = JsonUtility.FromJson<PlayerScore>(valueJson);
                        scores.Add(score);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse entry: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse Firebase response: {e.Message}");
        }

        return scores;
    }

    /// <summary>
    /// Helper method to split JSON entries (simplified JSON parsing)
    /// Firebase returns: {"user1": {...}, "user2": {...}}
    /// We split this into individual entries
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    private string[] SplitJsonEntries(string json)
    {
        List<string> entries = new List<string>();
        int braceCount = 0;
        int startIndex = 0;

        for (int i = 0; i < json.Length; i++)
        {
            if (json[i] == '{')
            {
                braceCount++;
            }
            else if (json[i] == '}')
            {
                braceCount--;

                if (braceCount == 0)
                {
                    // Found complete entry
                    string entry = json.Substring(startIndex, i - startIndex + 1);
                    entries.Add(entry);

                    // Skip to next entry (skip comma and whitespace)
                    i++;
                    while (i < json.Length && (json[i] == ',' || json[i] == ' ' || json[i] == '\n'))
                        i++;

                    startIndex = i;
                    i--; // Adjust for loop increment
                }
            }
        }

        return entries.ToArray();
    }
    private bool ValidateScore(PlayerScore score)
    {
        return true;
        // Basic validation - customize based on your game mechanics

        // Check reasonable time bounds
        if (score.completionTime < 5f || score.completionTime > 300f)
            return false;

        // Check path efficiency is within bounds
        if (score.pathEfficiency < 0f || score.pathEfficiency > 1f)
            return false;

        // Check max speed is reasonable
        if (score.maxSpeed < 0f || score.maxSpeed > 50f) // Adjust based on your game
            return false;

        // Check that score correlates somewhat with other metrics
        float expectedScore = (score.pathEfficiency * 1000f) + (score.maxSpeed * 10f) - (score.completionTime * 5f);
        if (Mathf.Abs(score.score - expectedScore) > expectedScore * 0.5f) // 50% tolerance
            return false;

        return true;
    }
    public void SubmitScore(PlayerScore score)
    {
        if (!firebaseAuth.IsAuthenticated())
        {
            Debug.LogWarning("Not authenticated, cannot submit score");
            return;
        }
        StartCoroutine(SubmitScoreCoroutine(score));
    }

    private IEnumerator SubmitScoreCoroutine(PlayerScore score)
    {
        // Refresh token if needed
        yield return firebaseAuth.RefreshTokenIfNeeded();

        // Validate score before submission (basic anti-cheat)
        if (!ValidateScore(score))
        {
            Debug.LogWarning("Score validation failed!");
            OnScoreSubmitted?.Invoke(false);
            yield break;
        }

        // Add user ID to the score data
        string userId = firebaseAuth.GetUserId();

        string json = JsonUtility.ToJson(score);
        string url = $"{firebaseURL}leaderboard/{userId}.json?auth={firebaseAuth.GetIdToken()}";

        UnityWebRequest request = UnityWebRequest.Put(url, json);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Score submitted successfully!");
        }
        else
        {
            Debug.LogError($"Failed to submit score: {request.error}");
        }
    }
    private IEnumerator CleanupOldEntries()
    {
        // Keep only top entries + recent entries to manage database size
        yield return new WaitForSeconds(1f); // Wait for submission to complete

        string url = $"{firebaseURL}{leaderboardNode}.json?orderBy=\"timestamp\"";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse and determine entries to remove
            // Implementation depends on your cleanup strategy
            Debug.Log("Cleanup completed");
        }
    }

    public void GetPlayerRank(string playerName, System.Action<int> onRankFound)
    {
        StartCoroutine(GetPlayerRankCoroutine(playerName, onRankFound));
    }

    private IEnumerator GetPlayerRankCoroutine(string playerName, System.Action<int> onRankFound)
    {
        yield return LoadLeaderboard();

        // Find player's best score and rank
        // This would be called after OnLeaderboardLoaded event
        int rank = -1; // -1 means not found
        onRankFound?.Invoke(rank);
    }
}