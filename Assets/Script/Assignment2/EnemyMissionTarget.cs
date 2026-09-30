using UnityEngine;

public class EnemyMissionTarget : MonoBehaviour
{
    [HideInInspector] public bool counted = false;

    private CarHealth health;
    private SimpleMissionManager mission;

    void Awake()
    {
        health = GetComponent<CarHealth>();
    }

    void Start()
    {
        mission = FindFirstObjectByType<SimpleMissionManager>();

        if (mission != null)
        {
            mission.RegisterEnemy(this);
        }

        if (health != null)
        {
            health.OnDied += HandleDied;
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= HandleDied;
        }
    }

    void HandleDied(CarHealth deadHealth)
    {
        if (counted) return;
        counted = true;

        if (mission != null)
        {
            mission.OnEnemyKilled(this);
        }
    }
}