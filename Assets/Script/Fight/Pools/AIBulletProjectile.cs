using UnityEngine;

public class AIBulletProjectile : MonoBehaviour
{
    [Header("Move")]
    public float lifeTime = 3f;
    public float hitRadius = 0.15f;

    [Header("Hit Feedback")]
    public GameObject hitEffectPrefab;
    public float hitEffectLifeTime = 0.5f;

    private float speed;
    private float damage;
    private float hitForce;
    private LayerMask hitMask;
    private Vector3 moveDir;
    private Transform ownerRoot;

    private bool initialized = false;
    private float lifeTimer = 0f;
    private bool hasHitSomething = false;

    [HideInInspector] public AIBulletPool pool;

    public void Init(
        Vector3 dir,
        float bulletSpeed,
        float bulletDamage,
        float bulletHitForce,
        LayerMask bulletHitMask,
        Transform bulletOwnerRoot)
    {
        moveDir = dir.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        hitForce = bulletHitForce;
        hitMask = bulletHitMask;
        ownerRoot = bulletOwnerRoot;

        initialized = true;
        lifeTimer = 0f;
        hasHitSomething = false;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
        }

        ClearTrailIfNeeded();
    }

    void OnEnable()
    {
        initialized = false;
        lifeTimer = 0f;
        hasHitSomething = false;
    }

    void Update()
    {
        if (!initialized) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            ReturnToPool();
            return;
        }

        Vector3 move = moveDir * speed * Time.deltaTime;

        if (Physics.SphereCast(
            transform.position,
            hitRadius,
            moveDir,
            out RaycastHit hit,
            move.magnitude,
            hitMask,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root != ownerRoot)
            {
                ApplyHit(hit);
                SpawnHitEffect(hit.point);
                ReturnToPool();
                return;
            }
        }

        transform.position += move;
    }

    void ApplyHit(RaycastHit hit)
    {
        if (hasHitSomething) return;
        hasHitSomething = true;

        CarHealth health = hit.collider.GetComponentInParent<CarHealth>();
        Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();

        if (health != null)
        {
            health.TakeDamage(damage);
        }

        if (rb != null)
        {
            rb.AddForce(moveDir * hitForce, ForceMode.Impulse);
        }

        Mine mine = hit.collider.GetComponentInParent<Mine>();
        if (mine != null)
        {
            mine.Explode();
        }
    }

    void SpawnHitEffect(Vector3 hitPoint)
    {
        if (hitEffectPrefab == null) return;

        if (EffectPool.Instance != null)
        {
            EffectPool.Instance.PlayEffect(
                hitEffectPrefab,
                hitPoint,
                Quaternion.identity,
                hitEffectLifeTime,
                null
            );
        }
        else
        {
            GameObject fx = Instantiate(
                hitEffectPrefab,
                hitPoint,
                Quaternion.identity
            );

            Destroy(fx, hitEffectLifeTime);
        }
    }

    void ReturnToPool()
    {
        initialized = false;
        lifeTimer = 0f;
        hasHitSomething = false;

        speed = 0f;
        damage = 0f;
        hitForce = 0f;
        moveDir = Vector3.zero;
        ownerRoot = null;

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