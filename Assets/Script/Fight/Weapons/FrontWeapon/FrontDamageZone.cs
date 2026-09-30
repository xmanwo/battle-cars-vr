using UnityEngine;

public class FrontDamageZone : MonoBehaviour
{
    public CarCollisionDamage owner;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<CarCollisionDamage>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null)
            return;

        owner.RegisterFrontTarget(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner == null)
            return;

        owner.UnregisterFrontTarget(other);
    }
}