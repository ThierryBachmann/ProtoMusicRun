using System;

[System.Serializable]
public class LeaderboardPlayerScore
{
    public string playerName;
    public int playerPosition;
    public long score;
    public float completionTime;
    public float pathEfficiency;
    public float maxSpeed;
    public int maxLevel;
    public long timestamp;

    public LeaderboardPlayerScore(string name, long score, float time, float efficiency, float speed,int level)
    {
        playerName = name;
        this.score = score;
        completionTime = time;
        pathEfficiency = efficiency;
        maxSpeed = speed;
        maxLevel=level;
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
    public override string ToString() {return $"{playerName} {score} {playerPosition}";}
}

[System.Serializable]
public class LeaderboardEntry
{
    public LeaderboardPlayerScore data;
    public string key;
}
