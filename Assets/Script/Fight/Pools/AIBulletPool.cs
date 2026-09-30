using System.Collections.Generic;
using UnityEngine;

public class AIBulletPool : MonoBehaviour
{
    public static AIBulletPool Instance;

    [Header("Pool")]
    public GameObject bulletPrefab;
    public int initialSize = 40;
    public Transform poolRoot;

    private Queue<AIBulletProjectile> poolQueue = new Queue<AIBulletProjectile>();

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

        Prewarm();
    }

    void Prewarm()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateBullet();
        }
    }

    AIBulletProjectile CreateBullet()
    {
        GameObject obj = Instantiate(bulletPrefab, poolRoot);
        obj.SetActive(false);

        AIBulletProjectile bullet = obj.GetComponent<AIBulletProjectile>();
        if (bullet == null)
        {
            Debug.LogError("AIBulletPool: bulletPrefab 上没有 AIBulletProjectile 组件。", obj);
            return null;
        }

        bullet.pool = this;
        poolQueue.Enqueue(bullet);
        return bullet;
    }

    public AIBulletProjectile GetBullet(Vector3 position, Quaternion rotation)
    {
        AIBulletProjectile bullet = null;

        while (poolQueue.Count > 0 && bullet == null)
        {
            bullet = poolQueue.Dequeue();
        }

        if (bullet == null)
        {
            bullet = CreateBullet();

            if (bullet == null)
                return null;

            bullet = poolQueue.Dequeue();
        }

        bullet.transform.SetParent(null);
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.gameObject.SetActive(true);

        return bullet;
    }

    public void ReturnBullet(AIBulletProjectile bullet)
    {
        if (bullet == null) return;

        bullet.transform.SetParent(poolRoot);
        bullet.gameObject.SetActive(false);
        poolQueue.Enqueue(bullet);
    }
}