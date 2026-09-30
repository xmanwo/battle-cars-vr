using System.Collections.Generic;
using UnityEngine;

public class SimpleMissionManager : MonoBehaviour
{
    [Header("Targets")]
    public int flowersNeeded = 5;

    [Header("Barrier")]
    public GameObject barrierObject;

    [Header("Win")]
    public bool missionUnlocked = false;
    public bool playerWon = false;

    [Header("Runtime")]
    public int flowersCollected = 0;
    public int totalEnemies = 0;
    public int enemiesKilled = 0;

    private List<EnemyMissionTarget> enemies = new List<EnemyMissionTarget>();

    public void RegisterEnemy(EnemyMissionTarget enemy)
    {
        if (enemy == null) return;
        if (enemies.Contains(enemy)) return;

        enemies.Add(enemy);
        totalEnemies = enemies.Count;
    }

    public void AddFlower()
    {
        flowersCollected++;
        Debug.Log("Flower collected: " + flowersCollected + "/" + flowersNeeded);
        CheckMissionComplete();
    }

    public void OnEnemyKilled(EnemyMissionTarget enemy)
    {
        enemiesKilled++;
        Debug.Log("Enemy killed: " + enemiesKilled + "/" + totalEnemies);
        CheckMissionComplete();
    }

    void CheckMissionComplete()
    {
        bool flowersDone = flowersCollected >= flowersNeeded;
        bool enemiesDone = enemiesKilled >= totalEnemies && totalEnemies > 0;

        if (missionUnlocked) return;

        if (flowersDone && enemiesDone)
        {
            missionUnlocked = true;

            if (barrierObject != null)
            {
                Destroy(barrierObject);
            }

            Debug.Log("Mission unlocked! Barrier removed. Go to the exit.");
        }
    }

    public void ReachExit()
    {
        if (playerWon) return;

        if (!missionUnlocked)
        {
            Debug.Log("Exit reached, but mission is not unlocked yet.");
            return;
        }

        playerWon = true;
        Debug.Log("YOU WIN!");
    }
}