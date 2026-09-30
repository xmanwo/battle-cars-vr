using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections.Generic;
using MoreMountains.Feedbacks;

public class ShotgunTurretWeapon : MonoBehaviour
{
    [Header("Refs")]
    public Camera aimCamera;
    public Transform yawPivot;
    public Transform pitchPivot;
    public Transform firePoint;
    public ShotgunAimSectorVisual sectorVisual;

    [Header("Aim")]
    public LayerMask groundMask = ~0;
    public float maxAimDistance = 120f;
    public float yawSpeed = 180f;
    public float pitchSpeed = 180f;
    public float minPitch = -20f;
    public float maxPitch = 20f;

    [Header("Shotgun Area")]
    public float attackRadius = 14f;
    [Range(5f, 180f)] public float attackAngle = 65f;
    public LayerMask damageMask;
    public int pellets = 8;
    public float damagePerPellet = 8f;
    public float hitForcePerPellet = 2f;
    public float maxTargetDistanceTolerance = 0.75f;

    [Header("Area Origin")]
    public Transform areaOrigin;
    public Vector3 areaOriginOffset = new Vector3(0f, -1f, 0f);

    [Header("Sector Visual")]
    public bool hideSectorWhenAirborne = false;
    public float sectorGroundCheckDistance = 5f;
    public float maxSectorShowHeightFromGround = 1.5f;
    public LayerMask sectorGroundMask = ~0;

    [Header("Shotgun Height Volume")]
    public bool useHeightLimit = true;
    public float attackHeight = 2.5f;

    [Header("Surface / Block")]
    public LayerMask blockMask;     // »áµ²×¡¹¥»÷µÄ²ã

    [Header("Fire")]
    public float fireRate = 1f;
    public bool requireAimToFire = true;

    [Header("Ammo")]
    public int totalAmmo = 1000;
    public bool consumeAmmo = true;

    [Header("Runtime Ammo")]
    public int currentAmmo;

    [Header("Special Hit")]
    public bool shotgunCanDetonateMines = true;

    [Header("Gamepad")]
    public bool allowGamepadInput = true;
    public float gamepadAimDeadZone = 0.2f;
    public bool invertGamepadX = false;
    public bool invertGamepadY = true;
    public float gamepadAimDistance = 18f;
    public float gamepadDownAngle = 4f;
    public float gamepadMinWorldDownAngle = 2.5f;
    public float gamepadMaxWorldUpAngle = 8f;
    public float triggerPressThreshold = 0.2f;

    [Header("Recoil")]
    public bool useRecoil = true;
    public Rigidbody recoilRb;
    public float recoilForce = 5f;
    public ForceMode recoilForceMode = ForceMode.Impulse;
    public bool autoFindRecoilRb = true;

    [Header("Visual")]
    public bool drawDebug = false;
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    public float impactEffectLifeTime = 0.35f;

    public GameObject shotgunBlastEffectPrefab;
    public float shotgunBlastEffectLifeTime = 0.15f;
    public Vector3 shotgunBlastEffectOffset = Vector3.zero;

    private float nextFireTime;
    private Vector3 currentAimPoint;
    private Vector3 currentFlatAimDir = Vector3.forward;
    private bool isAiming;
    private CarHealth myHealth;
    private Vector3 lastGamepadFlatDir = Vector3.forward;
    private Coroutine cooldownReadyCoroutine;

    [Header("Feel Feedbacks")]
    public MMF_Player fireFeedbacks;
    public MMF_Player cooldownReadyFeedbacks;

    [Header("Cooldown Ready")]
    public bool playCooldownReadyFeedback = true;
    public float readySoundLeadTime = 0.2f;

    private enum AimInputDevice
    {
        None,
        Mouse,
        Gamepad
    }

    [Header("Input Lock")]
    public float inputSwitchCooldown = 0.25f;

    private AimInputDevice currentInputDevice = AimInputDevice.None;
    private float lastInputSwitchTime = -999f;
    private Vector3 lastMousePosition;
    private bool lastQuestRightTriggerHeld;
    private bool suppressQuestFireUntilRelease;

    void Awake()
    {
        myHealth = GetComponentInParent<CarHealth>();
        lastMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera == null)
            aimCamera = FindFirstObjectByType<Camera>();

        if (autoFindRecoilRb && recoilRb == null)
            recoilRb = GetComponentInParent<Rigidbody>();

        totalAmmo = Mathf.Max(0, totalAmmo);
        pellets = Mathf.Max(1, pellets);
        attackRadius = Mathf.Max(0.1f, attackRadius);
        attackAngle = Mathf.Clamp(attackAngle, 1f, 180f);
        fireRate = Mathf.Max(0.01f, fireRate);
        readySoundLeadTime = Mathf.Max(0f, readySoundLeadTime);

        currentAmmo = totalAmmo;

        if (sectorVisual != null)
            sectorVisual.Hide();
    }

    void OnEnable()
    {
        lastQuestRightTriggerHeld = IsQuestRightTriggerHeld();
        suppressQuestFireUntilRelease = lastQuestRightTriggerHeld;
    }

    void Update()
    {

        if (IsOwnerDead())
        {
            isAiming = false;
            RefreshSectorVisual(false);
            return;
        }

        if (aimCamera == null || yawPivot == null)
        {
            RefreshSectorVisual(false);
            return;
        }

        Vector2 stick = Vector2.zero;
        bool hasGamepadAim = false;

        if (allowGamepadInput)
        {
            stick = GetGamepadAimInput();
            hasGamepadAim = stick.magnitude >= gamepadAimDeadZone;
        }

        Vector3 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : lastMousePosition;
        bool hasMouseAim = (mousePosition - lastMousePosition).sqrMagnitude > 0.01f;

        bool mouseAimHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool mouseFirePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        bool gamepadFirePressed = Gamepad.current != null && Gamepad.current.rightStickButton.wasPressedThisFrame;
        bool questFireHeld = IsQuestRightTriggerHeld();

        if (suppressQuestFireUntilRelease)
        {
            if (questFireHeld)
                questFireHeld = false;
            else
                suppressQuestFireUntilRelease = false;
        }

        bool questFirePressed = questFireHeld && !lastQuestRightTriggerHeld;

        if (hasGamepadAim || questFireHeld)
        {
            if (currentInputDevice != AimInputDevice.Gamepad)
            {
                currentInputDevice = AimInputDevice.Gamepad;
                lastInputSwitchTime = Time.time;
            }
        }
        else if (hasMouseAim || mouseAimHeld || mouseFirePressed)
        {
            if (currentInputDevice != AimInputDevice.Mouse)
            {
                currentInputDevice = AimInputDevice.Mouse;
                lastInputSwitchTime = Time.time;
            }
        }

        bool inputLocked = Time.time - lastInputSwitchTime < inputSwitchCooldown;

        if (currentInputDevice == AimInputDevice.Gamepad)
        {
            if (hasGamepadAim)
            {
                Vector3 flatDir = GetGamepadFlatDirection(stick);
                Vector3 realAimPoint = yawPivot.position + flatDir * gamepadAimDistance;
                Vector3 yawAimPoint = GetYawOnlyAimPoint(realAimPoint);

                RotateYaw(yawAimPoint);

                Vector3 finalShootDir = GetFinalGamepadShootDirection(flatDir);
                RotatePitchToDirection(finalShootDir);

                currentFlatAimDir = flatDir;
                isAiming = true;
            }
            else
            {
                isAiming = questFireHeld;

                if (questFireHeld)
                {
                    currentFlatAimDir = yawPivot != null ? yawPivot.forward : transform.forward;
                    currentFlatAimDir.y = 0f;

                    if (currentFlatAimDir.sqrMagnitude > 0.0001f)
                        currentFlatAimDir.Normalize();
                    else
                        currentFlatAimDir = transform.forward;

                    RotatePitchToDirection(GetFinalGamepadShootDirection(currentFlatAimDir));
                }

                if (!inputLocked && (hasMouseAim || mouseAimHeld || mouseFirePressed))
                {
                    currentInputDevice = AimInputDevice.Mouse;
                    lastInputSwitchTime = Time.time;
                }
            }

            if ((gamepadFirePressed || questFirePressed) && currentInputDevice == AimInputDevice.Gamepad)
            {
                TryFire();
            }
        }
        else
        {
            currentAimPoint = GetMouseWorldAimPoint();

            Vector3 yawAimPoint = GetYawOnlyAimPoint(currentAimPoint);

            RotateYaw(yawAimPoint);
            RotatePitch(currentAimPoint);

            // ²»ÔÙÓÃ¡°Ä¿±êµã·½Ïò¡±×öÉÈÐÎ·½Ïò
            // ¸Ä³ÉÓÃÅÚËþµ±Ç°ÕæÊµ³¯Ïò£¬ºÍÆÕÍ¨Ç¹±íÏÖ±£³ÖÒ»ÖÂ
            currentFlatAimDir = yawPivot != null ? yawPivot.forward : transform.forward;
            currentFlatAimDir.y = 0f;

            if (currentFlatAimDir.sqrMagnitude > 0.0001f)
                currentFlatAimDir.Normalize();
            else
                currentFlatAimDir = transform.forward;

            isAiming = mouseAimHeld;

            if (mouseFirePressed && (!requireAimToFire || isAiming))
                TryFire();

            if (!inputLocked && (hasGamepadAim || questFireHeld))
            {
                currentInputDevice = AimInputDevice.Gamepad;
                lastInputSwitchTime = Time.time;
            }
        }

        RefreshSectorVisual(isAiming);
        lastMousePosition = mousePosition;
        lastQuestRightTriggerHeld = IsQuestRightTriggerHeld();
    }

    bool IsOwnerDead()
    {
        return myHealth != null && myHealth.IsDead;
    }

    Vector2 GetGamepadAimInput()
    {
        UnityEngine.XR.InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightDevice.isValid &&
            rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 xrStick))
        {
            float xrX = xrStick.x;
            float xrY = xrStick.y;

            if (invertGamepadX) xrX = -xrX;
            if (invertGamepadY) xrY = -xrY;

            return new Vector2(xrX, xrY);
        }

        if (Gamepad.current == null) return Vector2.zero;

        Vector2 stick = Gamepad.current.rightStick.ReadValue();
        float x = stick.x;
        float y = stick.y;

        if (invertGamepadX) x = -x;
        if (invertGamepadY) y = -y;

        return new Vector2(x, y);
    }

    bool IsQuestRightTriggerHeld()
    {
        UnityEngine.XR.InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightDevice.isValid &&
            rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float trigger))
        {
            return trigger > triggerPressThreshold;
        }

        if (Gamepad.current != null)
            return Gamepad.current.rightTrigger.ReadValue() > triggerPressThreshold;

        return false;
    }

    Vector3 GetMouseWorldAimPoint()
    {
        Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = aimCamera.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root != transform.root)
                return hit.point;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return ray.origin + ray.direction * maxAimDistance;
    }

    Vector3 GetGamepadFlatDirection(Vector2 stick)
    {
        Transform basis = aimCamera != null ? aimCamera.transform : transform;

        Vector3 camForward = basis.forward;
        Vector3 camRight = basis.right;

        camForward.y = 0f;
        camRight.y = 0f;

        if (camForward.sqrMagnitude < 0.0001f) camForward = Vector3.forward;
        if (camRight.sqrMagnitude < 0.0001f) camRight = Vector3.right;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 flatDir = camRight * stick.x + camForward * stick.y;

        if (flatDir.sqrMagnitude < 0.0001f)
            return lastGamepadFlatDir;

        flatDir.Normalize();
        lastGamepadFlatDir = flatDir;
        return flatDir;
    }

    Vector3 GetFinalGamepadShootDirection(Vector3 flatDir)
    {
        if (flatDir.sqrMagnitude < 0.0001f)
            return yawPivot != null ? yawPivot.forward : transform.forward;

        flatDir.y = 0f;
        flatDir.Normalize();

        Vector3 pitchAxis = Vector3.Cross(flatDir, Vector3.up).normalized;
        if (pitchAxis.sqrMagnitude < 0.0001f)
            pitchAxis = transform.right;

        Vector3 shootDir = Quaternion.AngleAxis(-gamepadDownAngle, pitchAxis) * flatDir;
        shootDir.Normalize();

        float worldPitch = Mathf.Asin(shootDir.y) * Mathf.Rad2Deg;
        float minAllowedWorldPitch = -gamepadMinWorldDownAngle;
        float maxAllowedWorldPitch = gamepadMaxWorldUpAngle;

        float clampedWorldPitch = Mathf.Clamp(worldPitch, minAllowedWorldPitch, maxAllowedWorldPitch);

        if (!Mathf.Approximately(worldPitch, clampedWorldPitch))
        {
            Vector3 newFlatDir = new Vector3(shootDir.x, 0f, shootDir.z);

            if (newFlatDir.sqrMagnitude > 0.0001f)
            {
                newFlatDir.Normalize();

                float angleRad = clampedWorldPitch * Mathf.Deg2Rad;
                float y = Mathf.Sin(angleRad);
                float flatScale = Mathf.Cos(angleRad);

                shootDir = newFlatDir * flatScale + Vector3.up * y;
                shootDir.Normalize();
            }
        }

        return shootDir;
    }

    void RotateYaw(Vector3 aimPoint)
    {
        if (yawPivot == null || yawPivot.parent == null) return;

        Vector3 localAim = yawPivot.parent.InverseTransformPoint(aimPoint);
        localAim.y = 0f;
        if (localAim.sqrMagnitude < 0.0001f) return;

        float targetYaw = Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg;
        float currentYaw = NormalizeAngle(yawPivot.localEulerAngles.y);

        float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, yawSpeed * Time.deltaTime);
        yawPivot.localRotation = Quaternion.Euler(0f, newYaw, 0f);
    }

    void RotatePitch(Vector3 aimPoint)
    {
        if (pitchPivot == null || pitchPivot.parent == null) return;

        Vector3 localAim = pitchPivot.parent.InverseTransformPoint(aimPoint);
        float horizontalDist = new Vector2(localAim.x, localAim.z).magnitude;
        if (horizontalDist < 0.0001f) return;

        float targetPitch = -Mathf.Atan2(localAim.y, horizontalDist) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        float currentPitch = NormalizeAngle(pitchPivot.localEulerAngles.x);
        float newPitch = Mathf.MoveTowardsAngle(currentPitch, targetPitch, pitchSpeed * Time.deltaTime);
        pitchPivot.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
    }

    void RotatePitchToDirection(Vector3 worldDir)
    {
        if (pitchPivot == null || yawPivot == null) return;
        if (worldDir.sqrMagnitude < 0.0001f) return;

        Vector3 localDir = yawPivot.InverseTransformDirection(worldDir.normalized);
        float horizontalDist = new Vector2(localDir.x, localDir.z).magnitude;
        if (horizontalDist < 0.0001f) return;

        float targetPitch = -Mathf.Atan2(localDir.y, horizontalDist) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        float currentPitch = NormalizeAngle(pitchPivot.localEulerAngles.x);
        float newPitch = Mathf.MoveTowardsAngle(currentPitch, targetPitch, pitchSpeed * Time.deltaTime);
        pitchPivot.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
    }

    float NormalizeAngle(float angle)
    {
        return Mathf.DeltaAngle(0f, angle);
    }

    void RefreshSectorVisual(bool show)
    {
        if (sectorVisual == null) return;

        if (!show)
        {
            sectorVisual.Hide();
            return;
        }

        Vector3 origin = GetAreaOrigin();

        if (!CanShowSectorOnGround(origin))
        {
            sectorVisual.Hide();
            return;
        }

        Vector3 forward = yawPivot != null ? yawPivot.forward : currentFlatAimDir;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        if (CanFireNow())
            sectorVisual.SetState(ShotgunAimSectorVisual.SectorState.Ready);
        else
            sectorVisual.SetState(ShotgunAimSectorVisual.SectorState.Reloading);

        sectorVisual.Show(origin, forward, attackRadius, attackAngle);
    }

    public void TryFire()
    {
        if (IsOwnerDead()) return;
        if (Time.time < nextFireTime) return;
        if (requireAimToFire && !isAiming) return;

        if (consumeAmmo && currentAmmo <= 0)
            return;

        nextFireTime = Time.time + 1f / fireRate;

        if (consumeAmmo)
            currentAmmo--;

        Vector3 areaOriginPos = GetAreaOrigin();
        Vector3 muzzlePos = firePoint != null ? firePoint.position : areaOriginPos;

        // ÅÐ¶¨·½Ïò£ºÖ»¿´Ë®Æ½Ãé×¼
        Vector3 attackDir = currentFlatAimDir;
        if (attackDir.sqrMagnitude < 0.0001f)
            attackDir = yawPivot != null ? yawPivot.forward : transform.forward;

        attackDir.y = 0f;
        if (attackDir.sqrMagnitude < 0.0001f)
            attackDir = transform.forward;

        attackDir.Normalize();

        // ±íÏÖ·½Ïò£ºÇ¹¿ÚÕæÊµ³¯Ïò
        Vector3 visualDir = firePoint != null ? firePoint.forward : attackDir;
        if (visualDir.sqrMagnitude < 0.0001f)
            visualDir = attackDir;

        visualDir.Normalize();

        ApplyRecoil(visualDir);
        SpawnMuzzleFlash(muzzlePos, visualDir);
        // SpawnShotgunBlastEffect(muzzlePos, visualDir);
        FireShotgunArea(areaOriginPos, attackDir);

        fireFeedbacks?.PlayFeedbacks();
        ScheduleCooldownReadyFeedback();
    }

    void FireShotgunArea(Vector3 origin, Vector3 attackDir)
    {
        Collider[] hits = Physics.OverlapSphere(origin, attackRadius, damageMask, QueryTriggerInteraction.Collide);
        HashSet<CarHealth> damagedTargets = new HashSet<CarHealth>();
        Dictionary<Rigidbody, float> rigidbodyForceMap = new Dictionary<Rigidbody, float>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;
            if (col.transform.root == transform.root) continue;

            Vector3 hitPoint = GetClosestPointSafe(col, origin);
            Vector3 toTarget = hitPoint - origin;

            float dist = toTarget.magnitude;
            if (dist > attackRadius + maxTargetDistanceTolerance) continue;
            if (dist < 0.001f) continue;

            float verticalOffset = hitPoint.y - origin.y;
            if (useHeightLimit && Mathf.Abs(verticalOffset) > attackHeight * 0.5f)
                continue;

            Vector3 flatToTarget = toTarget;
            flatToTarget.y = 0f;

            Vector3 flatBaseDir = attackDir;
            flatBaseDir.y = 0f;

            if (flatToTarget.sqrMagnitude < 0.0001f || flatBaseDir.sqrMagnitude < 0.0001f)
                continue;

            flatToTarget.Normalize();
            flatBaseDir.Normalize();

            float angle = Vector3.Angle(flatBaseDir, flatToTarget);
            if (angle > attackAngle * 0.5f) continue;

            if (IsBlocked(origin, hitPoint, col)) continue;

            int pelletHits = EstimatePelletHits(angle, dist);
            if (pelletHits <= 0) continue;

            SpawnImpact(hitPoint);

            if (TryDetonateMine(col))
            {
                continue;
            }

            CarHealth health = col.GetComponentInParent<CarHealth>();
            if (health != null && !damagedTargets.Contains(health))
            {
                float totalDamage = pelletHits * damagePerPellet;
                health.TakeDamage(totalDamage);
                damagedTargets.Add(health);
            }

            Rigidbody rb = col.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                float totalForce = pelletHits * hitForcePerPellet;
                if (rigidbodyForceMap.ContainsKey(rb))
                    rigidbodyForceMap[rb] += totalForce;
                else
                    rigidbodyForceMap.Add(rb, totalForce);
            }
        }

        foreach (var pair in rigidbodyForceMap)
        {
            if (pair.Key == null) continue;

            Vector3 forceDir = (pair.Key.worldCenterOfMass - origin).normalized;
            if (forceDir.sqrMagnitude < 0.0001f)
                forceDir = attackDir;

            pair.Key.AddForce(forceDir * pair.Value, ForceMode.Impulse);
        }
    }

    int EstimatePelletHits(float angleFromCenter, float distance)
    {
        float angle01 = 1f - Mathf.Clamp01(angleFromCenter / Mathf.Max(0.01f, attackAngle * 0.5f));
        float distance01 = 1f - Mathf.Clamp01(distance / attackRadius);

        float weight = 0.65f * angle01 + 0.35f * distance01;
        int result = Mathf.RoundToInt(Mathf.Lerp(1f, pellets, weight));
        return Mathf.Clamp(result, 1, pellets);
    }

    bool TryDetonateMine(Collider col)
    {
        if (!shotgunCanDetonateMines) return false;
        if (col == null) return false;

        Mine mine = col.GetComponentInParent<Mine>();
        if (mine == null) return false;

        if (mine.transform.root == transform.root)
            return false;

        mine.Explode();
        return true;
    }

    Vector3 GetClosestPointSafe(Collider col, Vector3 from)
    {
        if (col == null) return from;

        if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider)
            return col.ClosestPoint(from);

        MeshCollider meshCol = col as MeshCollider;
        if (meshCol != null)
        {
            if (meshCol.convex)
                return col.ClosestPoint(from);

            return meshCol.bounds.ClosestPoint(from);
        }

        return col.bounds.ClosestPoint(from);
    }

    void ApplyRecoil(Vector3 fireForward)
    {
        if (!useRecoil) return;
        if (recoilRb == null) return;

        Vector3 recoilDir = -fireForward.normalized;
        recoilRb.AddForce(recoilDir * recoilForce, recoilForceMode);
    }

    void SpawnMuzzleFlash(Vector3 pos, Vector3 dir)
    {
        if (muzzleFlashPrefab == null) return;

        if (EffectPool.Instance != null)
        {
            EffectPool.Instance.PlayEffect(
                muzzleFlashPrefab,
                pos,
                Quaternion.LookRotation(dir, Vector3.up),
                0.08f,
                firePoint
            );
        }
        else
        {
            GameObject fx = Instantiate(muzzleFlashPrefab, pos, Quaternion.LookRotation(dir, Vector3.up));
            if (firePoint != null)
                fx.transform.SetParent(firePoint, true);
            Destroy(fx, 0.08f);
        }
    }

    void SpawnImpact(Vector3 pos)
    {
        if (impactEffectPrefab == null) return;

        if (EffectPool.Instance != null)
        {
            EffectPool.Instance.PlayEffect(
                impactEffectPrefab,
                pos,
                Quaternion.identity,
                impactEffectLifeTime,
                null
            );
        }
        else
        {
            GameObject fx = Instantiate(impactEffectPrefab, pos, Quaternion.identity);
            Destroy(fx, impactEffectLifeTime);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;

        Vector3 origin = GetAreaOrigin();
        Vector3 forward = Application.isPlaying ? currentFlatAimDir : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;
        forward.Normalize();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, attackRadius);

        Vector3 left = Quaternion.AngleAxis(-attackAngle * 0.5f, Vector3.up) * forward;
        Vector3 right = Quaternion.AngleAxis(attackAngle * 0.5f, Vector3.up) * forward;
        Gizmos.DrawLine(origin, origin + left * attackRadius);
        Gizmos.DrawLine(origin, origin + right * attackRadius);
    }

    Vector3 GetYawOnlyAimPoint(Vector3 realAimPoint)
    {
        Vector3 yawAimPoint = realAimPoint;
        yawAimPoint.y = yawPivot.position.y;
        return yawAimPoint;
    }

    Vector3 GetAreaOrigin()
    {
        Vector3 origin;

        if (areaOrigin != null)
            origin = areaOrigin.position + areaOriginOffset;
        else
            origin = transform.position + areaOriginOffset;

        // ´ÓºòÑ¡µãÉÏ·½ÍùÏÂÕÒµØÃæ
        Vector3 rayStart = origin + Vector3.up * 2f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f, sectorGroundMask, QueryTriggerInteraction.Ignore))
        {
            float minHeightAboveGround = 0.08f; // ÇøÓòÖÁÉÙÀëµØÒ»µãµã£¬±ÜÃâ´©µØÉÁË¸
            float minY = hit.point.y + minHeightAboveGround;

            if (origin.y < minY)
                origin.y = minY;
        }

        return origin;
    }

    bool IsBlocked(Vector3 origin, Vector3 targetPoint, Collider targetCol)
    {
        Vector3 dir = targetPoint - origin;
        float dist = dir.magnitude;
        if (dist < 0.001f) return false;

        dir /= dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, blockMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.transform.root != targetCol.transform.root)
                return true;
        }

        return false;
    }

    bool CanShowSectorOnGround(Vector3 origin)
    {
        if (!hideSectorWhenAirborne)
            return true;

        if (Physics.Raycast(origin + Vector3.up * 0.1f,
                            Vector3.down,
                            out RaycastHit hit,
                            sectorGroundCheckDistance,
                            sectorGroundMask,
                            QueryTriggerInteraction.Ignore))
        {
            return hit.distance <= maxSectorShowHeightFromGround;
        }

        return false;
    }

    bool CanFireNow()
    {
        if (IsOwnerDead()) return false;
        if (Time.time < nextFireTime) return false;
        if (consumeAmmo && currentAmmo <= 0) return false;
        return true;
    }

    public float GetCooldownProgress01()
    {
        if (fireRate <= 0f)
            return 1f;

        float cooldownDuration = 1f / fireRate;
        float remaining = Mathf.Max(0f, nextFireTime - Time.time);
        return Mathf.Clamp01(1f - remaining / cooldownDuration);
    }

    void SpawnShotgunBlastEffect(Vector3 pos, Vector3 dir)
    {
        if (shotgunBlastEffectPrefab == null) return;

        Vector3 spawnPos = pos;
        if (firePoint != null)
            spawnPos = firePoint.position + firePoint.TransformDirection(shotgunBlastEffectOffset);

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        if (EffectPool.Instance != null)
        {
            EffectPool.Instance.PlayEffect(
                shotgunBlastEffectPrefab,
                spawnPos,
                rot,
                shotgunBlastEffectLifeTime,
                firePoint
            );
        }
        else
        {
            GameObject fx = Instantiate(shotgunBlastEffectPrefab, spawnPos, rot);

            if (firePoint != null)
                fx.transform.SetParent(firePoint, true);

            Destroy(fx, shotgunBlastEffectLifeTime);
        }
    }

    void ScheduleCooldownReadyFeedback()
    {
        if (!playCooldownReadyFeedback) return;
        if (cooldownReadyFeedbacks == null) return;
        if (fireRate <= 0f) return;

        if (cooldownReadyCoroutine != null)
            StopCoroutine(cooldownReadyCoroutine);

        cooldownReadyCoroutine = StartCoroutine(CooldownReadyFeedbackRoutine());
    }

    System.Collections.IEnumerator CooldownReadyFeedbackRoutine()
    {
        float cooldownDuration = 1f / fireRate;
        float waitTime = Mathf.Max(0f, cooldownDuration - readySoundLeadTime);

        yield return new WaitForSeconds(waitTime);

        if (IsOwnerDead())
        {
            cooldownReadyCoroutine = null;
            yield break;
        }

        if (Time.time < nextFireTime)
        {
            cooldownReadyFeedbacks?.PlayFeedbacks();
        }

        cooldownReadyCoroutine = null;
    }
}



