using UnityEngine;

public class FlowerCollectible : MonoBehaviour
{
    [Header("Who Can Collect")]
    public GameObject playerObject;   // 直接把玩家车根物体拖进来

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (playerObject == null) return;

        // 碰到的是玩家本体，或者玩家子物体，都算
        if (other.transform.root.gameObject != playerObject)
            return;

        collected = true;

        SimpleMissionManager mission = FindFirstObjectByType<SimpleMissionManager>();
        if (mission != null)
        {
            mission.AddFlower();
        }

        Destroy(gameObject);
    }
}