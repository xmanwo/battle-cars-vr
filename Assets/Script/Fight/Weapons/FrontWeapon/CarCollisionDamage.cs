using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarCollisionDamage : MonoBehaviour
{
    public enum DamageMode
    {
        SingleHitPerContact,
        RepeatedWhileContact
    }

    [Header("Refs")]
    public Rigidbody rb;
    public CarHealth myHealth;

    [Header("Team")]
    public bool isEnemy = false;

    [Header("Mode")]
    public DamageMode damageMode = DamageMode.SingleHitPerContact;

    [Header("Repeated Damage (持续)")]
    public int repeatedDamage = 5;
    public float damageCooldown = 0.3f;

    [Header("Repeated Force (持续)")]
    public float repeatedForwardForce = 2f;
    public float repeatedUpForce = 0.3f;

    [Header("Hit Damage By My Forward Speed (撞击)")]
    public float minDamageSpeed = 3f;
    public float maxDamageSpeed = 20f;
    public int minDamage = 4;
    public int maxDamage = 25;

    [Header("Approach Check (撞击)")]
    [Range(-1f, 1f)]
    public float minApproachDot = 0.5f;

    [Header("Front Weapon Bonus")]
    public float frontDamageMultiplier = 1f;

    [Header("Extra Impact Force (撞击)")]
    public float extraImpactUpForce = 0f;
    public float extraImpactForwardForce = 0f;

    [Header("Weapon Force Speed Scale")]
    public float weaponForceMinMultiplier = 0f;
    public float weaponForceMaxMultiplier = 1f;
    public float weaponForceMinSpeed = 3f;
    public float weaponForceMaxSpeed = 20f;

    [Header("Debug")]
    public bool debugLog = false;

    private HashSet<CarHealth> frontTargets = new HashSet<CarHealth>();
    private HashSet<CarHealth> damagedThisContact = new HashSet<CarHealth>();
    private Dictionary<CarHealth, float> lastDamageTimeByTarget = new Dictionary<CarHealth, float>();

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (myHealth == null)
            myHealth = GetComponent<CarHealth>();
    }

    public bool CanDealCollisionDamage()
    {
        return myHealth != null && !myHealth.IsDead;
    }

    public void RegisterFrontTarget(Collider other)
    {
        if (!CanDealCollisionDamage()) return;

        CarHealth otherHealth = other.GetComponentInParent<CarHealth>();
        if (otherHealth == null) return;
        if (otherHealth == myHealth) return;
        if (otherHealth.IsDead) return;

        frontTargets.Add(otherHealth);

        if (debugLog)
            Debug.Log(name + " register front target: " + otherHealth.name);
    }

    public void UnregisterFrontTarget(Collider other)
    {
        CarHealth otherHealth = other.GetComponentInParent<CarHealth>();
        if (otherHealth == null) return;

        frontTargets.Remove(otherHealth);
        damagedThisContact.Remove(otherHealth);
        lastDamageTimeByTarget.Remove(otherHealth);

        if (debugLog)
            Debug.Log(name + " unregister front target: " + otherHealth.name);
    }

    void OnCollisionStay(Collision collision)
    {
        if (!CanDealCollisionDamage())
            return;

        TryDealCollisionDamage(collision);
    }

    void TryDealCollisionDamage(Collision collision)
    {
        if (!CanDealCollisionDamage())
            return;

        CarHealth otherHealth = collision.collider.GetComponentInParent<CarHealth>();
        CarCollisionDamage otherDamage = collision.collider.GetComponentInParent<CarCollisionDamage>();

        if (otherHealth == null || otherDamage == null)
            return;

        if (otherHealth == myHealth)
            return;

        if (otherHealth.IsDead)
            return;

        if (otherDamage.myHealth != null && otherDamage.myHealth.IsDead)
            return;

        if (isEnemy && otherDamage.isEnemy)
            return;

        if (!frontTargets.Contains(otherHealth))
        {
            if (debugLog)
                Debug.Log(name + " skip: target not in front trigger");
            return;
        }

        if (damageMode == DamageMode.SingleHitPerContact)
            TrySingleHitDamage(otherHealth);
        else if (damageMode == DamageMode.RepeatedWhileContact)
            TryRepeatedDamage(otherHealth);
    }

    void TrySingleHitDamage(CarHealth otherHealth)
    {
        if (!CanDealCollisionDamage()) return;
        if (otherHealth == null || otherHealth.IsDead) return;

        if (damagedThisContact.Contains(otherHealth))
        {
            if (debugLog)
                Debug.Log(name + " skip single-hit: already damaged this contact -> " + otherHealth.name);
            return;
        }

        Vector3 flatVelocity = rb.linearVelocity;
        flatVelocity.y = 0f;

        float speedMagnitude = flatVelocity.magnitude;
        if (speedMagnitude < 0.01f)
        {
            if (debugLog)
                Debug.Log(name + " skip single-hit: speed magnitude too low");
            return;
        }

        float forwardSpeed = Vector3.Dot(flatVelocity, transform.forward);
        if (forwardSpeed < minDamageSpeed)
        {
            if (debugLog)
                Debug.Log(name + " skip single-hit: forwardSpeed too low = " + forwardSpeed);
            return;
        }

        Vector3 toOther = otherHealth.transform.position - transform.position;
        toOther.y = 0f;

        if (toOther.sqrMagnitude < 0.0001f)
            return;

        toOther.Normalize();

        float approachDot = Vector3.Dot(flatVelocity.normalized, toOther);
        if (approachDot < minApproachDot)
        {
            if (debugLog)
                Debug.Log(name + " skip single-hit: approachDot too low = " + approachDot);
            return;
        }

        float speed01 = Mathf.InverseLerp(minDamageSpeed, maxDamageSpeed, forwardSpeed);
        float baseDamage = Mathf.Lerp(minDamage, maxDamage, speed01);
        float finalDamage = baseDamage * frontDamageMultiplier;

        int damage = Mathf.RoundToInt(finalDamage);
        damage = Mathf.Max(1, damage);

        otherHealth.TakeDamage(damage);

        Rigidbody otherRb = otherHealth.GetComponent<Rigidbody>();
        if (otherRb != null)
        {
            float weaponSpeed01 = Mathf.InverseLerp(
                weaponForceMinSpeed,
                weaponForceMaxSpeed,
                forwardSpeed
            );

            float forceScale = Mathf.Lerp(
                weaponForceMinMultiplier,
                weaponForceMaxMultiplier,
                weaponSpeed01
            );

            Vector3 extraForce =
                transform.forward * extraImpactForwardForce * forceScale +
                Vector3.up * extraImpactUpForce * forceScale;

            otherRb.AddForce(extraForce, ForceMode.Impulse);
        }

        damagedThisContact.Add(otherHealth);

        if (debugLog)
        {
            Debug.Log(
                name + " single-hit " + otherHealth.name +
                " | forwardSpeed=" + forwardSpeed +
                " | damage=" + damage
            );
        }
    }

    void TryRepeatedDamage(CarHealth otherHealth)
    {
        if (!CanDealCollisionDamage()) return;
        if (otherHealth == null || otherHealth.IsDead) return;

        float lastTime;
        if (lastDamageTimeByTarget.TryGetValue(otherHealth, out lastTime))
        {
            if (Time.time - lastTime < damageCooldown)
            {
                if (debugLog)
                    Debug.Log(name + " skip repeated: cooldown -> " + otherHealth.name);
                return;
            }
        }

        int damage = Mathf.RoundToInt(repeatedDamage * frontDamageMultiplier);
        damage = Mathf.Max(1, damage);

        otherHealth.TakeDamage(damage);

        Rigidbody otherRb = otherHealth.GetComponent<Rigidbody>();
        if (otherRb != null)
        {
            Vector3 repeatedForce =
                transform.forward * repeatedForwardForce +
                Vector3.up * repeatedUpForce;

            otherRb.AddForce(repeatedForce, ForceMode.Impulse);
        }

        lastDamageTimeByTarget[otherHealth] = Time.time;

        if (debugLog)
        {
            Debug.Log(
                name + " repeated-hit " + otherHealth.name +
                " | damage=" + damage
            );
        }
    }

    void OnDisable()
    {
        frontTargets.Clear();
        damagedThisContact.Clear();
        lastDamageTimeByTarget.Clear();
    }
}