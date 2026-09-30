using UnityEngine;

[RequireComponent(typeof(RaycastCarController))]
public class CarGunAI : EnemyAIBase
{
    public enum GunState
    {
        Attack,
        Cooldown
    }

    [Header("Refs")]
    public TurretGunWeapon gun;

    [Header("Fire")]
    public float fireRange = 30f;
    public float stopFireRange = 32f;
    public float attackDuration = 3f;
    public float cooldownDuration = 5f;
    public bool requireVisionToFire = true;

    [Header("Combat Band")]
    public float preferredRange = 20f;
    public float combatBand = 5f;
    public float maxChaseRange = 45f;
    public float retreatDistance = 8f;

    [Header("Range Control")]
    public float edgeSlowBuffer = 12f;
    public float edgeReturnThrottle = 0.45f;

    [Header("Drive AI")]
    public float approachThrottle = 0.75f;
    public float orbitThrottle = 0.6f;
    public float retreatThrottle = 0.7f;
    public float reverseThrottle = 0.65f;
    public float steerSensitivity = 1.25f;

    [Header("Orbit")]
    public float orbitOffset = 6f;
    public bool alternateOrbitSide = true;
    public float sideSwapInterval = 2.5f;

    [Header("Obstacle Avoidance")]
    public bool useAvoidance = true;
    public LayerMask obstacleMask;
    public float avoidCheckDistance = 6f;
    public float avoidSideCheckDistance = 4f;
    public float avoidSteerStrength = 1f;
    public float avoidThrottleMultiplier = 0.8f;

    [Header("Recover")]
    public float stuckCheckSpeed = 1.0f;
    public float stuckTime = 1.25f;
    public float reverseTime = 1.2f;

    [Header("Debug")]
    public GunState currentState = GunState.Attack;
    public Vector3 currentMovePoint;
    public int currentSideSign = 1;
    public bool isAvoidingObstacle;
    public bool isRecovering;
    public bool inFireRange;
    public bool canSeeTarget;
    public bool inDetectRange;

    float stateTimer;
    float stuckTimer;
    float reverseTimer;
    float sideSwapTimer;

    protected override void Awake()
    {
        base.Awake();

        if (gun == null)
            gun = GetComponentInChildren<TurretGunWeapon>();
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

        currentState = GunState.Attack;
        stateTimer = attackDuration;
        stuckTimer = 0f;
        reverseTimer = 0f;
        sideSwapTimer = sideSwapInterval;

        inFireRange = false;
        inDetectRange = false;
        canSeeTarget = false;
        isRecovering = false;
        isAvoidingObstacle = false;

        if (alternateOrbitSide)
            currentSideSign = Random.value > 0.5f ? 1 : -1;

        if (gun != null)
        {
            gun.SetAIControl(true);
            gun.SetAITarget(target);
            gun.SetAIFire(false);
        }
    }

    void Update()
    {
        RefreshTargetIfNeeded();

        if (gun != null)
        {
            gun.SetAIControl(true);
            gun.SetAITarget(target);
        }

        if (car == null || !HasValidLivingTarget())
        {
            target = null;
            StopCar();
            SetGunFire(false);
            ResetMotionTimers();
            return;
        }

        float dist = FlatDistance(transform.position, target.position);

        inDetectRange = dist <= detectRadius;
        canSeeTarget = !useVision || CanSeeTarget();

        if (!inDetectRange)
        {
            StopCar();
            SetGunFire(false);
            ResetMotionTimers();
            return;
        }

        if (reverseTimer > 0f)
        {
            reverseTimer -= Time.deltaTime;
            isRecovering = true;
            SetGunFire(false);
            ReverseRecover();
            return;
        }

        isRecovering = false;

        UpdateFireState(dist);
        UpdateMove(dist);
        UpdateOrbitSideSwap();
        CheckIfStuck(dist);
    }

    void ResetMotionTimers()
    {
        stuckTimer = 0f;
        reverseTimer = 0f;
        isRecovering = false;
        isAvoidingObstacle = false;
    }

    void UpdateFireState(float dist)
    {
        if (!inFireRange && dist <= fireRange)
            inFireRange = true;
        else if (inFireRange && dist > stopFireRange)
            inFireRange = false;

        stateTimer -= Time.deltaTime;

        bool canActuallyFire = inFireRange;
        if (requireVisionToFire)
            canActuallyFire &= canSeeTarget;

        if (currentState == GunState.Attack)
        {
            SetGunFire(canActuallyFire);

            if (stateTimer <= 0f)
            {
                currentState = GunState.Cooldown;
                stateTimer = cooldownDuration;
                SetGunFire(false);
            }
        }
        else
        {
            SetGunFire(false);

            if (stateTimer <= 0f)
            {
                currentState = GunState.Attack;
                stateTimer = attackDuration;
            }
        }
    }

    void UpdateMove(float dist)
    {
        float minBand = preferredRange - combatBand;
        float maxBand = preferredRange + combatBand;
        float edgeStart = Mathf.Max(0f, detectRadius - edgeSlowBuffer);

        // 快到探测边缘时，优先回收到理想交战距离，避免继续横移冲出范围
        if (dist > edgeStart)
        {
            Vector3 returnPoint = GetApproachPoint(preferredRange);
            MoveSmart(returnPoint, edgeReturnThrottle, allowReverse: false);
            return;
        }

        if (dist > maxChaseRange)
        {
            MoveSmart(target.position, approachThrottle, allowReverse: false);
            return;
        }

        if (dist < minBand)
        {
            MoveSmart(GetRetreatPoint(), retreatThrottle, allowReverse: true);
            return;
        }

        if (dist > maxBand)
        {
            MoveSmart(GetApproachPoint(preferredRange), approachThrottle, allowReverse: false);
            return;
        }

        BuildOrbitPoint();
        MoveSmart(currentMovePoint, orbitThrottle, allowReverse: true);
    }

    Vector3 GetApproachPoint(float desiredDistance)
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f)
            return target.position;

        toTarget.Normalize();
        return target.position - toTarget * desiredDistance;
    }

    void BuildOrbitPoint()
    {
        if (target == null) return;

        Vector3 toSelf = transform.position - target.position;
        toSelf.y = 0f;

        if (toSelf.sqrMagnitude < 0.001f)
            toSelf = -target.forward;

        toSelf.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, toSelf).normalized;

        currentMovePoint =
            target.position
            + toSelf * preferredRange
            + right * (orbitOffset * currentSideSign);
    }

    Vector3 GetRetreatPoint()
    {
        Vector3 away = transform.position - target.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -target.forward;

        away.Normalize();
        return transform.position + away * retreatDistance;
    }

    void UpdateOrbitSideSwap()
    {
        if (!alternateOrbitSide)
            return;

        sideSwapTimer -= Time.deltaTime;
        if (sideSwapTimer <= 0f)
        {
            currentSideSign *= -1;
            sideSwapTimer = sideSwapInterval;
        }
    }

    void MoveSmart(Vector3 point, float throttle, bool allowReverse)
    {
        EnemyAIDriveHelper.MoveSmart(
            car,
            transform,
            point,
            throttle,
            reverseThrottle,
            steerSensitivity,
            allowReverse,
            out float steer,
            out float finalThrottle
        );

        if (useAvoidance && TryGetAvoidSteer(out float avoidSteer))
        {
            isAvoidingObstacle = true;
            steer = Mathf.Clamp(steer + avoidSteer * avoidSteerStrength, -1f, 1f);
            finalThrottle *= avoidThrottleMultiplier;
            car.SetInput(finalThrottle, steer);
        }
        else
        {
            isAvoidingObstacle = false;
        }
    }

    bool TryGetAvoidSteer(out float avoidSteer)
    {
        avoidSteer = 0f;

        Vector3 origin = transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;
        Vector3 forward = transform.forward;
        Vector3 leftDir = (forward - transform.right * 0.6f).normalized;
        Vector3 rightDir = (forward + transform.right * 0.6f).normalized;

        bool hitForward = Physics.Raycast(origin, forward, avoidCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore);
        bool hitLeft = Physics.Raycast(origin, leftDir, avoidSideCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore);
        bool hitRight = Physics.Raycast(origin, rightDir, avoidSideCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore);

        if (!hitForward && !hitLeft && !hitRight)
            return false;

        if (hitForward)
        {
            if (hitLeft && !hitRight) avoidSteer = 1f;
            else if (hitRight && !hitLeft) avoidSteer = -1f;
            else
            {
                Vector3 localTarget = target != null ? transform.InverseTransformPoint(target.position) : Vector3.right;
                avoidSteer = localTarget.x >= 0f ? 1f : -1f;
            }
            return true;
        }

        if (hitLeft)
        {
            avoidSteer = 1f;
            return true;
        }

        if (hitRight)
        {
            avoidSteer = -1f;
            return true;
        }

        return false;
    }

    void ReverseRecover()
    {
        EnemyAIDriveHelper.ReverseRecoverFromTarget(
            car,
            transform,
            target,
            reverseThrottle
        );
    }

    void CheckIfStuck(float dist)
    {
        if (rb == null) return;

        // 边界附近不触发倒车恢复，避免一脚倒出探测范围
        if (dist > detectRadius - edgeSlowBuffer)
        {
            stuckTimer = 0f;
            return;
        }

        float localForwardSpeed = Mathf.Abs(
            EnemyAIDriveHelper.GetLocalForwardSpeed(transform, rb)
        );

        if (localForwardSpeed < stuckCheckSpeed)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckTime)
            {
                reverseTimer = reverseTime;
                stuckTimer = 0f;
                SetGunFire(false);
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    void SetGunFire(bool firing)
    {
        if (gun != null)
            gun.SetAIFire(firing);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fireRange);

        Gizmos.color = new Color(1f, 0.7f, 0f);
        Gizmos.DrawWireSphere(transform.position, stopFireRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredRange);

        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, preferredRange - combatBand);
        Gizmos.DrawWireSphere(transform.position, preferredRange + combatBand);

        Gizmos.color = new Color(1f, 0f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, detectRadius - edgeSlowBuffer));

        if (useVision)
        {
            Gizmos.color = inDetectRange ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, detectRadius);

            Vector3 eyePos = transform.position + transform.up * eyeHeight;
            if (target != null)
            {
                Vector3 targetPos = target.position + Vector3.up * targetHeight;
                Gizmos.color = canSeeTarget ? Color.blue : Color.gray;
                Gizmos.DrawLine(eyePos, targetPos);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(currentMovePoint, 0.6f);
        Gizmos.DrawLine(transform.position, currentMovePoint);
    }
}