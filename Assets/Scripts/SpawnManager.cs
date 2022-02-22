using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform[] allSpawnPoints, fFASpawnPoints, team1SpawnPoints, team2SpawnPoints, team3SpawnPoints;

    public static SpawnManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        foreach (Transform spawn in allSpawnPoints)
        {
            spawn.gameObject.SetActive(false);
        }
    }
    
    public Transform GetFFASpawnPoint()
    {
        return fFASpawnPoints[Random.Range(0, fFASpawnPoints.Length)];
    }
}
