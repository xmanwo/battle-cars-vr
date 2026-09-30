using UnityEngine;

public class SpikeWeapon : MonoBehaviour
{
    [Header("Refs")]
    public CarCollisionDamage collisionDamage;

    [Header("Damage")]
    public float damageMultiplier = 1.2f;

    [Header("Knockback")]
    public float impactUpForce = 0f;
    public float impactForwardForce = 5f;

    [Header("Force By Speed")]
    public float minForceMultiplier = 0.2f;
    public float maxForceMultiplier = 1f;
    public float minForceSpeed = 3f;
    public float maxForceSpeed = 18f;

    void Awake()
    {
        if (collisionDamage == null)
            collisionDamage = GetComponentInParent<CarCollisionDamage>();
    }

    void OnEnable()
    {
        if (collisionDamage != null)
        {
            collisionDamage.frontDamageMultiplier = damageMultiplier;

            collisionDamage.extraImpactUpForce = impactUpForce;
            collisionDamage.extraImpactForwardForce = impactForwardForce;

            collisionDamage.weaponForceMinMultiplier = minForceMultiplier;
            collisionDamage.weaponForceMaxMultiplier = maxForceMultiplier;
            collisionDamage.weaponForceMinSpeed = minForceSpeed;
            collisionDamage.weaponForceMaxSpeed = maxForceSpeed;
        }
    }

    void OnDisable()
    {
        if (collisionDamage != null)
        {
            collisionDamage.frontDamageMultiplier = 1f;

            collisionDamage.extraImpactUpForce = 0f;
            collisionDamage.extraImpactForwardForce = 0f;

            collisionDamage.weaponForceMinMultiplier = 0f;
            collisionDamage.weaponForceMaxMultiplier = 1f;
            collisionDamage.weaponForceMinSpeed = 3f;
            collisionDamage.weaponForceMaxSpeed = 20f;
        }
    }
}