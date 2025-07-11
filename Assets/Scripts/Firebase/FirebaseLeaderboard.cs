
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

public class FirebaseLeaderboard : MonoBehaviour
{
    public FirebaseAuth firebaseAuth;
    public string firebaseURL = "https://protorunmusic-default-rtdb.europe-west1.firebasedatabase.app/"; 
    //"https://your-project-default-rtdb.firebaseio.com/";
    

    void Start()
    {
        firebaseAuth.OnAuthenticationComplete += OnAuthComplete;
    }

    private void OnAuthComplete(bool success)
    {
        if (success)
        {
            Debug.Log("Ready to submit scores!");
            // Now you can safely submit scores
        }
    }

    public void SubmitScore(long score, float time, float efficiency, float maxSpeed, List<Vector3> path)
    {
        if (!firebaseAuth.IsAuthenticated())
        {
            Debug.LogWarning("Not authenticated, cannot submit score");
            return;
        }

        StartCoroutine(SubmitScoreCoroutine(score, time, efficiency, maxSpeed, path));
    }

    private IEnumerator SubmitScoreCoroutine(long score, float time, float efficiency, float maxSpeed, List<Vector3> path)
    {
        // Refresh token if needed
        yield return firebaseAuth.RefreshTokenIfNeeded();

        PlayerScore playerScore = new PlayerScore(
            firebaseAuth.GetPlayerName(),
            score,
            time,
            efficiency,
            maxSpeed,
            path
        );

        // Add user ID to the score data
        string userId = firebaseAuth.GetUserId();

        string json = JsonUtility.ToJson(playerScore);
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
}