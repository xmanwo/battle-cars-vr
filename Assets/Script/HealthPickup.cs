using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Heal")]
    public int healAmount = 30;

    [Header("Target")]
    public LayerMask playerLayer;

    private bool used = false;

    void OnEnable()
    {
        used = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (used)
            return;

        if (((1 << other.gameObject.layer) & playerLayer) == 0)
            return;

        CarHealth health = other.GetComponentInParent<CarHealth>();
        if (health == null)
            return;

        if (health.IsDead)
            return;

        used = true;

        health.Heal(healAmount);

        Destroy(gameObject);
    }
}