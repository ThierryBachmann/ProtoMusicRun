using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

[System.Serializable]
public class PlayerScore
{
    public string playerName;
    public long score;
    public float completionTime;
    public float pathEfficiency;
    public float maxSpeed;
    public int maxLevel;
    public long timestamp;
    //public List<Vector3> keyWaypoints; // Simplified path data

    public PlayerScore(string name, long score, float time, float efficiency, float speed,int level)
    {
        playerName = name;
        this.score = score;
        completionTime = time;
        pathEfficiency = efficiency;
        maxSpeed = speed;
        maxLevel=level;
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //keyWaypoints = waypoints;
    }
}

[System.Serializable]
public class LeaderboardEntry
{
    public PlayerScore data;
    public string key;
}
