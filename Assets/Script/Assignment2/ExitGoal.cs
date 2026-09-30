using UnityEngine;

public class ExitGoal : MonoBehaviour
{
    [Header("Who Can Win")]
    public GameObject playerObject;   // 直接拖玩家车根物体

    private void OnTriggerEnter(Collider other)
    {
        if (playerObject == null) return;

        if (other.transform.root.gameObject != playerObject)
            return;

        SimpleMissionManager mission = FindFirstObjectByType<SimpleMissionManager>();
        if (mission != null)
        {
            mission.ReachExit();
        }
    }
}