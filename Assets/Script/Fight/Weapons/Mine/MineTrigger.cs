using UnityEngine;

public class MineTrigger : MonoBehaviour
{
    public Mine mine;

    void Awake()
    {
        if (mine == null)
            mine = GetComponentInParent<Mine>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (mine == null) return;

        CarHealth otherHealth = other.GetComponentInParent<CarHealth>();
        if (otherHealth == null) return;
        if (otherHealth.IsDead) return;

        mine.TryExplodeFromVehicle(otherHealth.transform);
    }

}