using UnityEngine;

public class ShovelWeapon : MonoBehaviour
{
    [Header("Refs")]
    public CarCollisionDamage collisionDamage;

    [Header("Damage")]
    public float damageMultiplier = 1.1f;

    [Header("Force Direction")]
    public float impactUpForce = 7f;
    public float impactForwardForce = 6f;

    [Header("Force By Speed")]
    public float minForceMultiplier = 0.3f;  // 最低有效速度时，也至少有 30% 力
    public float maxForceMultiplier = 1f;    // 高速时达到 100% 力
    public float minForceSpeed = 3f;         // 从这个速度开始增长
    public float maxForceSpeed = 18f;        // 到这个速度达到最大值

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