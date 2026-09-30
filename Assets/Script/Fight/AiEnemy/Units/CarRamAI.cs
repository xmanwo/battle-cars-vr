using UnityEngine;

[RequireComponent(typeof(RaycastCarController))]
public class CarRamAI : EnemyAIBase
{
    public enum RamState
    {
        Chase,
        Charge,
        Reposition
    }

    [Header("Move")]
    public float stopDistance = 3f;
    public float slowDownDistance = 8f;
    public float ramDistance = 10f;
    public float ramAngle = 25f;
    public float ramThrottleMultiplier = 1f;
    public float turnAroundThrottle = 0.35f;

    [Header("Charge State")]
    public float chargeDuration = 1.2f;
    public float chargeSteerStrength = 0.7f;
    public float tooCloseDistance = 2.2f;

    [Header("Reposition State")]
    public float repositionDuration = 0.9f;
    public float repositionThrottle = 0.55f;
    public float repositionSteerStrength = 0.8f;
    public float repositionMinDistance = 5f;

    [Header("Avoid")]
    public bool enableAvoid = true;
    public float avoidCheckDistance = 6f;
    public float sideCheckDistance = 4f;
    public float avoidSteerStrength = 1f;
    public float avoidThrottle = 0.4f;
    public float sideRayAngle = 30f;
    public float rayStartHeight = 0.5f;
    public float carWidthOffset = 0.8f;
    public float avoidHoldTime = 0.12f;

    [Header("Friendly Avoid")]
    public bool avoidFriendlies = true;
    public float friendlyDetectRadius = 3.2f;
    public float friendlyDetectForward = 4.5f;
    public float friendlyAvoidSteer = 0.85f;
    public float friendlyAvoidThrottle = 0.2f;
    public LayerMask friendlyBlockMask = ~0;

    [Header("Drive Fail Recover")]
    public float throttleStuckThreshold = 0.7f;
    public float minMoveSpeed = 1.3f;
    public float driveFailTime = 0.3f;

    [Header("Recover")]
    public float stuckCheckSpeed = 0.8f;
    public float stuckTime = 0.6f;
    public float reverseTime = 0.85f;
    public bool reverseStraightWhenBlocked = true;
    public float reverseBlockedCheckDistance = 1.8f;
    public float reverseStraightTime = 0.25f;

    [Header("Command")]
    public bool useCommandPoint = false;
    public Vector3 commandPoint;

    [Header("Attack Permission")]
    public bool allowCharge = true;
    public bool forceHoldChase = false;

    [Header("Squad Chase")]
    public bool blendCommandAndTarget = true;
    [Range(0f, 1f)] public float commandPointWeight = 0.65f;
    public float targetPullWeight = 0.35f;
    public float commandSlowRadius = 5f;

    [Header("Debug")]
    public RamState currentState = RamState.Chase;

    bool canSeeTarget;
    float stuckTimer;
    float reverseTimer;
    float avoidTimer;
    float driveFailTimer;
    float currentAvoidSteer;
    float stateTimer;
    int repositionTurnSign = 1;
    float lockedChargeSteer;
    float reverseSteerLock;

    public void SetCommandPoint(Vector3 point)
    {
        commandPoint = point;
        useCommandPoint = true;
    }

    public void ClearCommandPoint()
    {
        useCommandPoint = false;
    }

    public void SetChargePermission(bool canCharge)
    {
        allowCharge = canCharge;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        ResetAIState();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetAIState();
    }

    void ResetAIState()
    {
        AutoAssignTarget();
        currentState = RamState.Chase;
        stateTimer = 0f;
        stuckTimer = 0f;
        reverseTimer = 0f;
        avoidTimer = 0f;
        driveFailTimer = 0f;
        lockedChargeSteer = 0f;
        reverseSteerLock = 0f;
    }

    void Update()
    {
        RefreshTargetIfNeeded();

        if (car == null || target == null)
        {
            StopCar();
            ResetMotionTimers();
            return;
        }

        canSeeTarget = !useVision || CanSeeTarget();

        if (reverseTimer > 0f)
        {
            reverseTimer -= Time.deltaTime;
            ReverseRecover();
            return;
        }

        if (TryFriendlyAvoid(out float friendlySteer, out float friendlyThrottle))
        {
            car.SetInput(friendlyThrottle, friendlySteer);
            CheckIfStuck();
            return;
        }

        if (enableAvoid)
            CheckAvoid();

        if (avoidTimer > 0f)
        {
            avoidTimer -= Time.deltaTime;
            AvoidMove();
            CheckIfStuck();
            return;
        }

        UpdateStateMachine();
        CheckIfStuck();
    }

    void ResetMotionTimers()
    {
        stuckTimer = 0f;
        stateTimer = 0f;
        avoidTimer = 0f;
        driveFailTimer = 0f;
    }

    void UpdateStateMachine()
    {
        switch (currentState)
        {
            case RamState.Chase:
                UpdateChase();
                break;
            case RamState.Charge:
                UpdateCharge();
                break;
            case RamState.Reposition:
                UpdateReposition();
                break;
        }
    }

    void UpdateChase()
    {
        Vector3 aimPoint = GetCurrentAimPoint();
        Vector3 toTarget = aimPoint - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance < 0.01f)
        {
            StopCar();
            return;
        }

        Vector3 dir = toTarget.normalized;
        Vector3 localTarget = transform.InverseTransformPoint(aimPoint);
        float angle = Vector3.Angle(transform.forward, dir);

        float steer;
        float throttle;

        if (localTarget.z < 0f)
        {
            steer = Mathf.Abs(localTarget.x) < 0.5f ? 1f : Mathf.Sign(localTarget.x);
            throttle = turnAroundThrottle;
        }
        else
        {
            steer = Mathf.Clamp((localTarget.x / Mathf.Max(localTarget.magnitude, 0.001f)) * 1.2f, -1f, 1f);
            throttle = 1f;

            if (distance < slowDownDistance)
                throttle = Mathf.Lerp(0.35f, 1f, distance / slowDownDistance);

            if (distance < stopDistance)
                throttle = 0.15f;
        }

        if (canSeeTarget && !forceHoldChase && allowCharge && distance < ramDistance && angle < ramAngle)
        {
            EnterCharge();
            return;
        }

        car.SetInput(throttle, steer);
        CheckDriveFailRecover(throttle);
    }

    void UpdateCharge()
    {
        stateTimer -= Time.deltaTime;

        Vector3 toTarget = GetCurrentAimPoint() - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < 0.01f)
        {
            EnterReposition();
            return;
        }

        float throttle = Mathf.Clamp(ramThrottleMultiplier, -1f, 1f);
        car.SetInput(throttle, lockedChargeSteer);
        CheckDriveFailRecover(throttle);

        if (toTarget.magnitude < tooCloseDistance || stateTimer <= 0f)
            EnterReposition();
    }

    protected override void RefreshTargetIfNeeded()
    {
        if (!refreshTargetAtRuntime || !autoFindPlayerTarget)
            return;

        if (target == null || !IsTargetStillPlayer(target))
            AutoAssignTarget();
    }

    void UpdateReposition()
    {
        stateTimer -= Time.deltaTime;

        Vector3 aimPoint = GetCurrentAimPoint();
        Vector3 toTarget = aimPoint - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        Vector3 localTarget = transform.InverseTransformPoint(aimPoint);

        float steer = -repositionTurnSign * repositionSteerStrength;

        if (Mathf.Abs(localTarget.x) > 1f)
            steer = Mathf.Clamp(-Mathf.Sign(localTarget.x) * repositionSteerStrength, -1f, 1f);

        car.SetInput(repositionThrottle, steer);

        if (stateTimer <= 0f && distance > repositionMinDistance)
            currentState = RamState.Chase;
    }

    void EnterCharge()
    {
        currentState = RamState.Charge;
        stateTimer = chargeDuration;

        Vector3 localTarget = transform.InverseTransformPoint(GetCurrentAimPoint());
        lockedChargeSteer = Mathf.Clamp(
            (localTarget.x / Mathf.Max(localTarget.magnitude, 0.001f)) * chargeSteerStrength,
            -1f,
            1f
        );
    }

    void EnterReposition()
    {
        currentState = RamState.Reposition;
        stateTimer = repositionDuration;

        Vector3 localTarget = transform.InverseTransformPoint(GetCurrentAimPoint());
        repositionTurnSign = Mathf.Abs(localTarget.x) > 0.1f
            ? (localTarget.x > 0f ? 1 : -1)
            : (Random.value > 0.5f ? 1 : -1);

        driveFailTimer = 0f;
    }

    void StartReverseRecover()
    {
        reverseTimer = reverseTime;
        stuckTimer = 0f;
        driveFailTimer = 0f;
        avoidTimer = 0f;
        currentState = RamState.Chase;
        stateTimer = 0f;

        Vector3 origin = transform.position + transform.up * rayStartHeight;
        bool frontBlocked = Physics.Raycast(
            origin,
            transform.forward,
            reverseBlockedCheckDistance,
            blockMask,
            QueryTriggerInteraction.Ignore
        );

        if (reverseStraightWhenBlocked && frontBlocked)
        {
            reverseSteerLock = 0f;
        }
        else
        {
            Vector3 localTarget = transform.InverseTransformPoint(GetCurrentAimPoint());
            reverseSteerLock = Mathf.Clamp(-Mathf.Sign(localTarget.x), -1f, 1f);

            if (Mathf.Abs(localTarget.x) < 0.3f)
                reverseSteerLock = Random.value > 0.5f ? -1f : 1f;
        }
    }

    bool IsTargetStillPlayer(Transform t)
    {
        if (t == null) return false;

        CarPlayerInput input = t.GetComponent<CarPlayerInput>();
        return input != null && input.isActiveAndEnabled;
    }

    Vector3 GetCurrentAimPoint()
    {
        if (useCommandPoint && target != null && blendCommandAndTarget)
        {
            Vector3 flatMe = transform.position; flatMe.y = 0f;
            Vector3 flatCmd = commandPoint; flatCmd.y = 0f;
            Vector3 flatTarget = target.position; flatTarget.y = 0f;

            float distToCmd = Vector3.Distance(flatMe, flatCmd);
            float t = Mathf.InverseLerp(commandSlowRadius, 0f, distToCmd);
            float cmdW = Mathf.Lerp(commandPointWeight, 0.15f, t);
            float tarW = Mathf.Lerp(targetPullWeight, 0.85f, t);

            return (flatCmd * cmdW + flatTarget * tarW) / Mathf.Max(cmdW + tarW, 0.001f);
        }

        if (useCommandPoint)
            return commandPoint;

        if (target != null)
            return target.position;

        return transform.position;
    }

    float GetLocalForwardSpeed()
    {
        return EnemyAIDriveHelper.GetLocalForwardSpeed(transform, rb);
    }

    bool TryFriendlyAvoid(out float steer, out float throttle)
    {
        steer = 0f;
        throttle = 0f;

        if (!avoidFriendlies) return false;

        Vector3 center = transform.position + Vector3.up * rayStartHeight;
        Collider[] hits = Physics.OverlapSphere(center, friendlyDetectRadius, friendlyBlockMask, QueryTriggerInteraction.Ignore);

        Transform best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            Transform other = hits[i].transform.root;
            if (other == transform.root) continue;

            bool isFriendly =
                other.GetComponent<CarRamAI>() != null ||
                other.GetComponent<CarGunAI>() != null;

            if (!isFriendly) continue;

            CarHealth otherHealth = other.GetComponent<CarHealth>();
            if (otherHealth != null && otherHealth.IsDead)
                continue;

            Vector3 toOther = other.position - transform.position;
            toOther.y = 0f;

            float dist = toOther.magnitude;
            if (dist < 0.01f || dist > friendlyDetectForward) continue;

            float forwardDot = Vector3.Dot(transform.forward, toOther.normalized);
            if (forwardDot < 0.2f) continue;

            float score = dist - forwardDot * 0.5f;
            if (score < bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        if (best == null) return false;

        Vector3 local = transform.InverseTransformPoint(best.position);
        steer = local.x >= 0f ? -friendlyAvoidSteer : friendlyAvoidSteer;

        float distToFriendly = Vector3.Distance(transform.position, best.position);
        throttle = distToFriendly < 1.8f ? -0.25f : friendlyAvoidThrottle;

        return true;
    }

    void CheckAvoid()
    {
        Vector3 center = transform.position + transform.up * rayStartHeight;
        Vector3 left = center - transform.right * carWidthOffset;
        Vector3 right = center + transform.right * carWidthOffset;

        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.AngleAxis(-sideRayAngle, transform.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(sideRayAngle, transform.up) * forward;

        bool hitForward = Physics.Raycast(center, forward, avoidCheckDistance, blockMask, QueryTriggerInteraction.Ignore);
        bool hitLeft = Physics.Raycast(left, leftDir, sideCheckDistance, blockMask, QueryTriggerInteraction.Ignore);
        bool hitRight = Physics.Raycast(right, rightDir, sideCheckDistance, blockMask, QueryTriggerInteraction.Ignore);

        if (!hitForward && !hitLeft && !hitRight)
            return;

        float avoidSteer;

        if (hitLeft && !hitRight) avoidSteer = 1f;
        else if (hitRight && !hitLeft) avoidSteer = -1f;
        else
        {
            Vector3 localTarget = transform.InverseTransformPoint(GetCurrentAimPoint());
            avoidSteer = localTarget.x >= 0f ? 1f : -1f;
        }

        currentAvoidSteer = Mathf.Clamp(avoidSteer * avoidSteerStrength, -1f, 1f);
        avoidTimer = avoidHoldTime;
    }

    void AvoidMove()
    {
        float throttle = avoidThrottle;

        Vector3 flatToTarget = GetCurrentAimPoint() - transform.position;
        flatToTarget.y = 0f;

        if (flatToTarget.magnitude < stopDistance)
            throttle = 0.2f;

        car.SetInput(throttle, currentAvoidSteer);
    }

    void ReverseRecover()
    {
        float steer = reverseSteerLock;

        if (reverseTimer > reverseTime - reverseStraightTime)
            steer = 0f;

        car.SetInput(-0.7f, steer);
    }

    void CheckDriveFailRecover(float throttleInput)
    {
        if (rb == null) return;

        bool allowDriveFailCheck =
            currentState == RamState.Chase ||
            currentState == RamState.Charge;

        if (!allowDriveFailCheck)
        {
            driveFailTimer = 0f;
            return;
        }

        float localForwardSpeed = GetLocalForwardSpeed();

        bool tryingForwardHard = throttleInput >= throttleStuckThreshold;
        bool notMovingForwardEnough = localForwardSpeed < minMoveSpeed;

        if (tryingForwardHard && notMovingForwardEnough)
        {
            driveFailTimer += Time.deltaTime;

            if (driveFailTimer >= driveFailTime)
            {
                StartReverseRecover();
            }
        }
        else
        {
            driveFailTimer = 0f;
        }
    }

    void CheckIfStuck()
    {
        if (rb == null) return;

        float localForwardSpeedAbs = Mathf.Abs(GetLocalForwardSpeed());

        Vector3 origin = transform.position + transform.up * rayStartHeight;
        bool frontBlocked = Physics.Raycast(
            origin,
            transform.forward,
            avoidCheckDistance * 0.85f,
            blockMask,
            QueryTriggerInteraction.Ignore
        );

        bool lowForwardSpeed = localForwardSpeedAbs < stuckCheckSpeed;

        if (lowForwardSpeed || (frontBlocked && currentState != RamState.Reposition))
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckTime)
            {
                StartReverseRecover();
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ramDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, slowDownDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
        }

        if (useVision)
        {
            Gizmos.color = canSeeTarget ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, detectRadius);

            Vector3 eyePos = transform.position + transform.up * eyeHeight;
            if (target != null)
            {
                Vector3 targetPos = target.position + Vector3.up * targetHeight;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(eyePos, targetPos);
            }
        }

        if (enableAvoid)
        {
            Vector3 center = transform.position + transform.up * rayStartHeight;
            Vector3 left = center - transform.right * carWidthOffset;
            Vector3 right = center + transform.right * carWidthOffset;

            Vector3 forward = transform.forward;
            Vector3 leftDir = Quaternion.AngleAxis(-sideRayAngle, transform.up) * forward;
            Vector3 rightDir = Quaternion.AngleAxis(sideRayAngle, transform.up) * forward;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(center, center + forward * avoidCheckDistance);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(left, left + leftDir * sideCheckDistance);
            Gizmos.DrawLine(right, right + rightDir * sideCheckDistance);
        }
    }
}