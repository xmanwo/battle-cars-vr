using System.Collections.Generic;
using UnityEngine;

public class BulletVisualPool : MonoBehaviour
{
    public static BulletVisualPool Instance;

    [Header("Pool")]
    public GameObject bulletPrefab;
    public int initialSize = 30;
    public Transform poolRoot;

    private Queue<BulletVisualProjectile> poolQueue = new Queue<BulletVisualProjectile>();

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

    BulletVisualProjectile CreateBullet()
    {
        GameObject obj = Instantiate(bulletPrefab, poolRoot);
        obj.SetActive(false);

        BulletVisualProjectile bullet = obj.GetComponent<BulletVisualProjectile>();
        if (bullet == null)
        {
            Debug.LogError("BulletVisualPool: bulletPrefab 上没有 BulletVisualProjectile 组件。", obj);
            return null;
        }

        bullet.pool = this;
        poolQueue.Enqueue(bullet);
        return bullet;
    }

    public BulletVisualProjectile GetBullet(Vector3 position, Quaternion rotation)
    {
        BulletVisualProjectile bullet = null;

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

    public void ReturnBullet(BulletVisualProjectile bullet)
    {
        if (bullet == null) return;

        bullet.transform.SetParent(poolRoot);
        bullet.gameObject.SetActive(false);
        poolQueue.Enqueue(bullet);
    }
}