using UnityEngine;

public class PooledEffect : MonoBehaviour
{
    [HideInInspector] public EffectPool ownerPool;

    private float lifeTimer = 0f;
    private float targetLifeTime = 1f;
    private Transform followTarget;
    private bool initialized = false;

    public void Play(float lifeTime, Transform parent = null)
    {
        targetLifeTime = Mathf.Max(0.01f, lifeTime);
        followTarget = parent;
        lifeTimer = 0f;
        initialized = true;

        if (followTarget != null)
        {
            transform.SetParent(followTarget, true);
        }
        else
        {
            transform.SetParent(null, true);
        }

        ClearParticleSystems();
        ClearTrailRenderers();
    }

    void Update()
    {
        if (!initialized) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= targetLifeTime)
        {
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        initialized = false;
        lifeTimer = 0f;
        followTarget = null;

        transform.SetParent(ownerPool != null ? ownerPool.poolRoot : null);

        if (ownerPool != null)
        {
            ownerPool.ReturnEffect(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void ClearParticleSystems()
    {
        ParticleSystem[] psList = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < psList.Length; i++)
        {
            psList[i].Clear(true);
            psList[i].Play(true);
        }
    }

    void ClearTrailRenderers()
    {
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            trails[i].Clear();
        }
    }
}