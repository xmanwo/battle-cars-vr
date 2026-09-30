using UnityEngine;

public class BulletVisualProjectile : MonoBehaviour
{
    [Header("Move")]
    private float speed = 80f;
    public float maxLifeTime = 3f;

    private Vector3 targetPoint;
    private bool hasTarget = false;

    private CarHealth pendingHealth;
    private float pendingDamage;
    private Rigidbody pendingRb;
    private Vector3 pendingHitForceDir;
    private float pendingHitForce;

    [Header("Hit Feedback")]
    public GameObject hitEffectPrefab;
    public float hitEffectLifeTime = 0.5f;

    private Transform pendingHitTarget;
    private Vector3 pendingLocalHitOffset;

    // 新增：等子弹飞到点后再引爆地雷
    private Mine pendingMine;

    private bool initialized = false;
    private bool hasHitApplied = false;
    private float lifeTimer = 0f;

    [HideInInspector] public BulletVisualPool pool;

    private int pendingMineSpawnVersion = -1;

    public void Init(
        Vector3 startPoint,
        Vector3 endPoint,
        float bulletSpeed,
        CarHealth targetHealth,
        float damage,
        Rigidbody targetRb,
        Vector3 hitForceDir,
        float hitForce,
        Transform hitTarget = null,
        Vector3 localHitOffset = default,
        Mine targetMine = null,
        int targetMineSpawnVersion = -1)
    {
        transform.position = startPoint;
        targetPoint = endPoint;
        speed = bulletSpeed;
        hasTarget = true;

        pendingHealth = targetHealth;
        pendingDamage = damage;
        pendingRb = targetRb;
        pendingHitForceDir = hitForceDir;
        pendingHitForce = hitForce;

        pendingHitTarget = hitTarget;
        pendingLocalHitOffset = localHitOffset;
        pendingMine = targetMine;
        pendingMineSpawnVersion = targetMineSpawnVersion;

        initialized = true;
        hasHitApplied = false;
        lifeTimer = 0f;

        Vector3 dir = targetPoint - startPoint;
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        ClearTrailIfNeeded();
    }

    void OnEnable()
    {
        lifeTimer = 0f;
        initialized = false;
        hasTarget = false;
        hasHitApplied = false;
    }

    void Update()
    {
        if (!initialized || !hasTarget) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifeTime)
        {
            ReturnToPool();
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint,
            speed * Time.deltaTime
        );

        Vector3 toTarget = targetPoint - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        if ((transform.position - targetPoint).sqrMagnitude < 0.01f)
        {
            ApplyHitIfNeeded();
            SpawnHitEffect();
            ReturnToPool();
        }
    }

    void ApplyHitIfNeeded()
    {
        if (hasHitApplied) return;
        hasHitApplied = true;

        if (pendingMine != null)
        {
            if (pendingMine.gameObject.activeInHierarchy &&
                pendingMine.ownerRoot != null &&
                pendingMine.SpawnVersion == pendingMineSpawnVersion)
            {
                pendingMine.TryExplodeFromBullet();
            }
            return;
        }

        if (pendingHealth != null)
        {
            pendingHealth.TakeDamage(pendingDamage);
        }

        if (pendingRb != null)
        {
            pendingRb.AddForce(pendingHitForceDir * pendingHitForce, ForceMode.Impulse);
        }
    }

    void SpawnHitEffect()
    {
        if (hitEffectPrefab == null) return;

        Vector3 spawnPos = targetPoint;
        Transform parent = null;

        if (pendingHitTarget != null)
        {
            spawnPos = pendingHitTarget.TransformPoint(pendingLocalHitOffset);

            if (pendingHitTarget.gameObject.layer != LayerMask.NameToLayer("Ground"))
            {
                parent = pendingHitTarget;
            }
        }

        if (EffectPool.Instance != null)
        {
            EffectPool.Instance.PlayEffect(
                hitEffectPrefab,
                spawnPos,
                Quaternion.identity,
                hitEffectLifeTime,
                parent
            );
        }
        else
        {
            GameObject fx = Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);

            if (parent != null)
            {
                fx.transform.SetParent(parent, true);
            }

            Destroy(fx, hitEffectLifeTime);
        }
    }

    void ReturnToPool()
    {
        initialized = false;
        hasTarget = false;
        hasHitApplied = false;
        lifeTimer = 0f;

        pendingHealth = null;
        pendingRb = null;
        pendingHitTarget = null;
        pendingDamage = 0f;
        pendingHitForce = 0f;
        pendingHitForceDir = Vector3.zero;
        pendingLocalHitOffset = Vector3.zero;
        pendingMine = null;
        pendingMineSpawnVersion = -1;

        ClearTrailIfNeeded();

        if (pool != null)
        {
            pool.ReturnBullet(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void ClearTrailIfNeeded()
    {
        TrailRenderer tr = GetComponent<TrailRenderer>();
        if (tr != null)
        {
            tr.Clear();
        }
    }
}