using System.Collections.Generic;
using UnityEngine;

public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance;

    [Header("Pool Root")]
    public Transform poolRoot;

    private Dictionary<GameObject, Queue<PooledEffect>> poolMap =
        new Dictionary<GameObject, Queue<PooledEffect>>();

    [Header("Optional Prewarm")]
    public GameObject[] prewarmPrefabs;
    public int[] prewarmCounts;



    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (poolRoot == null)
            poolRoot = transform;
    }

    void Start()
    {
        if (prewarmPrefabs == null || prewarmCounts == null) return;

        int count = Mathf.Min(prewarmPrefabs.Length, prewarmCounts.Length);
        for (int i = 0; i < count; i++)
        {
            Prewarm(prewarmPrefabs[i], prewarmCounts[i]);
        }
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        Queue<PooledEffect> queue = GetOrCreateQueue(prefab);

        for (int i = 0; i < count; i++)
        {
            PooledEffect fx = CreateEffect(prefab);
            if (fx != null)
                queue.Enqueue(fx);
        }
    }

    public void PlayEffect(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        float lifeTime,
        Transform parent = null)
    {
        if (prefab == null) return;

        Queue<PooledEffect> queue = GetOrCreateQueue(prefab);

        PooledEffect fx = null;

        while (queue.Count > 0 && fx == null)
        {
            fx = queue.Dequeue();
        }

        if (fx == null)
        {
            fx = CreateEffect(prefab);
        }

        if (fx == null) return;

        fx.transform.SetParent(null);
        fx.transform.position = position;
        fx.transform.rotation = rotation;
        fx.gameObject.SetActive(true);
        fx.Play(lifeTime, parent);
    }

    public void ReturnEffect(PooledEffect effect)
    {
        if (effect == null) return;

        GameObject prefabKey = effect.gameObject.GetComponent<EffectPoolKey>()?.sourcePrefab;
        if (prefabKey == null)
        {
            effect.gameObject.SetActive(false);
            return;
        }

        Queue<PooledEffect> queue = GetOrCreateQueue(prefabKey);

        effect.transform.SetParent(poolRoot);
        effect.gameObject.SetActive(false);
        queue.Enqueue(effect);
    }

    Queue<PooledEffect> GetOrCreateQueue(GameObject prefab)
    {
        if (!poolMap.TryGetValue(prefab, out Queue<PooledEffect> queue))
        {
            queue = new Queue<PooledEffect>();
            poolMap.Add(prefab, queue);
        }

        return queue;
    }

    PooledEffect CreateEffect(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, poolRoot);
        obj.SetActive(false);

        PooledEffect fx = obj.GetComponent<PooledEffect>();
        if (fx == null)
        {
            fx = obj.AddComponent<PooledEffect>();
        }

        EffectPoolKey key = obj.GetComponent<EffectPoolKey>();
        if (key == null)
        {
            key = obj.AddComponent<EffectPoolKey>();
        }

        key.sourcePrefab = prefab;
        fx.ownerPool = this;

        return fx;
    }
}