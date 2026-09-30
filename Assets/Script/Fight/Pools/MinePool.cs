using System.Collections.Generic;
using UnityEngine;

public class MinePool : MonoBehaviour
{
    public static MinePool Instance;

    [Header("Pool")]
    public GameObject minePrefab;
    public int initialSize = 20;
    public Transform poolRoot;

    private Queue<Mine> poolQueue = new Queue<Mine>();

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
            CreateMine();
        }
    }

    Mine CreateMine()
    {
        if (minePrefab == null)
        {
            Debug.LogError("MinePool: minePrefab 没有指定。", this);
            return null;
        }

        GameObject obj = Instantiate(minePrefab, poolRoot);
        obj.SetActive(false);

        Mine mine = obj.GetComponent<Mine>();
        if (mine == null)
        {
            Debug.LogError("MinePool: minePrefab 上没有 Mine 组件。", obj);
            return null;
        }

        mine.pool = this;
        poolQueue.Enqueue(mine);
        return mine;
    }

    public Mine GetMine(Vector3 position, Quaternion rotation)
    {
        Mine mine = null;

        while (poolQueue.Count > 0 && mine == null)
        {
            mine = poolQueue.Dequeue();
        }

        if (mine == null)
        {
            mine = CreateMine();

            if (mine == null)
                return null;

            mine = poolQueue.Dequeue();
        }

        mine.transform.SetParent(null);
        mine.transform.position = position;
        mine.transform.rotation = rotation;
        mine.gameObject.SetActive(true);

        return mine;
    }

    public void ReturnMine(Mine mine)
    {
        if (mine == null) return;

        mine.transform.SetParent(poolRoot);
        mine.gameObject.SetActive(false);
        poolQueue.Enqueue(mine);
    }
}