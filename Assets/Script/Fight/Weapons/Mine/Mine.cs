using UnityEngine;
using System.Collections.Generic;

public class Mine : MonoBehaviour
{
    [Header("Life")]
    public float armDelay = 0.25f;
    public float lifeTime = 8f;

    [Header("Explosion")]
    public float damage = 30f;
    public float explosionRadius = 4f;
    public float explosionForce = 10f;
    public float upwardForce = 2f;

    [Header("Target Filter")]
    public LayerMask hitMask;

    [Header("Effects")]
    public GameObject explosionEffectPrefab;
    public float explosionEffectLifeTime = 1.5f;

    [Header("Explosion Audio")]
    public AudioClip explosionSound;
    [Range(0f, 1f)] public float explosionVolume = 1f;
    public Vector2 explosionPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Chain Explosion")]
    public bool allowChainExplosion = true;
    public float chainTriggerRadius = 2.5f;
    public float chainExplosionDelay = 0.12f;
    public LayerMask mineMask;

    [Header("Runtime")]
    public Transform ownerRoot;
    public float ownerIgnoreTriggerTime = 0.5f;
    public float ownerDamageProtectionTime = 0.5f;
    [Range(0f, 100f)] public float ownerDamageReductionPercent = 70f;

    [HideInInspector] public MinePool pool;

    public int SpawnVersion { get; private set; } = 0;

    private bool isArmed = false;
    private bool hasExploded = false;
    private float spawnTime;

    private Rigidbody rb;
    private Collider[] allColliders;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        allColliders = GetComponentsInChildren<Collider>(true);
    }

    public void InitFromPool(Transform newOwnerRoot, Vector3 startVelocity)
    {
        SpawnVersion++;

        ownerRoot = newOwnerRoot;
        spawnTime = Time.time;
        isArmed = false;
        hasExploded = false;

        CancelInvoke();
        EnableAllColliders(true);

        if (rb != null)
        {
            rb.linearVelocity = startVelocity;
            rb.angularVelocity = Vector3.zero;
        }

        Invoke(nameof(ArmMine), armDelay);
        Invoke(nameof(Explode), lifeTime);
    }

    void ArmMine()
    {
        if (hasExploded) return;
        isArmed = true;
        CheckNearbyVehiclesAfterArming();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isArmed) return;
        if (hasExploded) return;

        CarHealth otherHealth = collision.collider.GetComponentInParent<CarHealth>();
        if (otherHealth == null) return;
        if (otherHealth.IsDead) return;

        if (ShouldIgnoreOwnerTrigger(otherHealth)) return;

        Explode();
    }

    public bool TryExplodeFromBullet()
    {
        if (!gameObject.activeInHierarchy) return false;
        if (hasExploded) return false;
        if (ownerRoot == null) return false;

        Explode();
        return true;
    }

    public void TryExplodeFromVehicle(Transform target)
    {
        if (!isArmed) return;
        if (hasExploded) return;
        if (target == null) return;

        CarHealth targetHealth = target.GetComponentInParent<CarHealth>();
        if (targetHealth == null) return;
        if (targetHealth.IsDead) return;

        if (ShouldIgnoreOwnerTrigger(targetHealth)) return;

        Explode();
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        SpawnExplosionEffect();
        PlayExplosionSound();

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        HashSet<CarHealth> damagedCars = new HashSet<CarHealth>();

        for (int i = 0; i < hits.Length; i++)
        {
            CarHealth health = hits[i].GetComponentInParent<CarHealth>();
            if (health == null) continue;
            if (damagedCars.Contains(health)) continue;
            damagedCars.Add(health);

            Rigidbody hitRb = health.GetComponent<Rigidbody>();
            if (hitRb == null)
                hitRb = health.GetComponentInParent<Rigidbody>();

            if (!health.IsDead)
            {
                float finalDamage = GetDamageToTarget(health);
                if (finalDamage > 0f)
                    health.TakeDamage(finalDamage);
            }

            if (hitRb != null)
            {
                Vector3 dir = (hitRb.worldCenterOfMass - transform.position).normalized;
                Vector3 force = dir * explosionForce + Vector3.up * upwardForce;
                hitRb.AddForce(force, ForceMode.Impulse);
            }
        }

        TryChainExplodeNearbyMines();
        ReturnToPool();
    }

    void SpawnExplosionEffect()
    {
        if (explosionEffectPrefab == null) return;

        if (EffectPool.Instance != null)
        {
            EffectPool.Instance.PlayEffect(
                explosionEffectPrefab,
                transform.position,
                Quaternion.identity,
                explosionEffectLifeTime,
                null
            );
        }
        else
        {
            GameObject fx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, explosionEffectLifeTime);
        }
    }

    void PlayExplosionSound()
    {
        if (explosionSound == null)
            return;

        GameObject audioObj = new GameObject("Mine Explosion Audio");
        audioObj.transform.position = transform.position;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = explosionSound;
        source.volume = explosionVolume;
        source.pitch = Random.Range(explosionPitchRange.x, explosionPitchRange.y);
        source.spatialBlend = 1f;
        source.playOnAwake = false;

        source.Play();

        Destroy(audioObj, explosionSound.length + 0.2f);
    }

    void ReturnToPool()
    {
        CancelInvoke();

        isArmed = false;
        hasExploded = false;
        spawnTime = 0f;
        ownerRoot = null;

        EnableAllColliders(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (pool != null)
        {
            pool.ReturnMine(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void EnableAllColliders(bool enabledState)
    {
        if (allColliders == null) return;

        for (int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i] != null)
                allColliders[i].enabled = enabledState;
        }
    }

    bool ShouldIgnoreOwnerTrigger(CarHealth targetHealth)
    {
        if (targetHealth == null) return false;
        if (ownerRoot == null) return false;
        if (targetHealth.transform.root != ownerRoot) return false;

        return Time.time - spawnTime <= ownerIgnoreTriggerTime;
    }

    float GetDamageToTarget(CarHealth targetHealth)
    {
        if (targetHealth == null) return 0f;

        float finalDamage = damage;

        if (ownerRoot != null &&
            targetHealth.transform.root == ownerRoot &&
            Time.time - spawnTime <= ownerDamageProtectionTime)
        {
            finalDamage *= 1f - ownerDamageReductionPercent / 100f;
        }

        return finalDamage;
    }

    public void TriggerChainExplosion(float delay)
    {
        if (hasExploded) return;
        Invoke(nameof(Explode), delay);
    }

    void CheckNearbyVehiclesAfterArming()
    {
        if (!isArmed) return;
        if (hasExploded) return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            CarHealth health = hits[i].GetComponentInParent<CarHealth>();
            if (health == null) continue;
            if (health.IsDead) continue;

            if (ShouldIgnoreOwnerTrigger(health)) continue;

            Explode();
            return;
        }
    }

    void TryChainExplodeNearbyMines()
    {
        if (!allowChainExplosion) return;

        float searchRadius = explosionRadius + chainTriggerRadius;

        Collider[] nearby = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            mineMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < nearby.Length; i++)
        {
            Mine otherMine = nearby[i].GetComponentInParent<Mine>();
            if (otherMine == null) continue;
            if (otherMine == this) continue;
            if (!otherMine.allowChainExplosion) continue;

            float requiredDistance = explosionRadius + otherMine.chainTriggerRadius;
            float realDistance = Vector3.Distance(transform.position, otherMine.transform.position);

            if (realDistance <= requiredDistance)
            {
                otherMine.TriggerChainExplosion(chainExplosionDelay);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}