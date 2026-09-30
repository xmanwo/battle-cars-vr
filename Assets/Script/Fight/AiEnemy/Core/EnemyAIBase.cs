using UnityEngine;

public abstract class EnemyAIBase : MonoBehaviour
{
    [Header("Refs")]
    public RaycastCarController car;
    public Transform target;

    [Header("Auto Target")]
    public bool autoFindPlayerTarget = true;
    public bool refreshTargetAtRuntime = true;

    [Header("Detection")]
    public bool useVision = true;
    public float detectRadius = 120f;
    public float eyeHeight = 0.8f;
    public float targetHeight = 0.6f;
    public LayerMask blockMask;

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        if (car == null)
            car = GetComponent<RaycastCarController>();

        rb = car != null && car.rb != null ? car.rb : GetComponent<Rigidbody>();

        AutoAssignTarget();
    }

    protected virtual void Start()
    {
    }

    protected virtual void OnEnable()
    {
    }

    protected void StopCar()
    {
        if (car != null)
            car.SetInput(0f, 0f);
    }

    protected float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    protected virtual void RefreshTargetIfNeeded()
    {
        if (!refreshTargetAtRuntime || !autoFindPlayerTarget)
            return;

        if (!HasValidLivingTarget())
            AutoAssignTarget();
    }

    protected virtual void AutoAssignTarget()
    {
        if (!autoFindPlayerTarget)
            return;

        CarPlayerInput[] players = FindObjectsByType<CarPlayerInput>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            if (!players[i].isActiveAndEnabled) continue;
            if (!players[i].isPlayerCar) continue;

            CarHealth health = players[i].GetComponentInParent<CarHealth>();
            if (health != null && health.IsDead) continue;

            target = players[i].transform;
            return;
        }

        target = null;
    }

    protected virtual bool HasValidLivingTarget()
    {
        if (target == null) return false;

        CarPlayerInput input = target.GetComponent<CarPlayerInput>();
        if (input == null || !input.isActiveAndEnabled || !input.isPlayerCar)
            return false;

        CarHealth health = target.GetComponentInParent<CarHealth>();
        if (health != null && health.IsDead)
            return false;

        return true;
    }

    protected virtual bool CanSeeTarget()
    {
        if (target == null)
            return false;

        Vector3 flatToTarget = target.position - transform.position;
        flatToTarget.y = 0f;

        if (flatToTarget.magnitude > detectRadius)
            return false;

        Vector3 eyePos = transform.position + transform.up * eyeHeight;
        Vector3 targetPos = target.position + Vector3.up * targetHeight;
        Vector3 toTarget = targetPos - eyePos;

        if (toTarget.sqrMagnitude <= 0.000001f)
            return true;

        Vector3 dir = toTarget.normalized;
        float distance = toTarget.magnitude;

        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, distance, blockMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(target) && hit.transform != target)
                return false;
        }

        return true;
    }
}