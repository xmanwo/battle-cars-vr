using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

public class FlamethrowerTurretWeapon : MonoBehaviour
{
    [Header("Refs")]
    public Camera aimCamera;
    public Transform yawPivot;
    public Transform pitchPivot;
    public Transform firePoint;
    public FlamethrowerTriggerZone flameTriggerZone;

    [Header("Aim")]
    public LayerMask groundMask = ~0;
    public float maxAimDistance = 120f;
    public float yawSpeed = 180f;
    public float pitchSpeed = 180f;
    public float minPitch = -20f;
    public float maxPitch = 20f;

    [Header("Flame Damage")]
    public LayerMask damageMask;
    public LayerMask blockMask;
    public float damageTickInterval = 0.15f;
    public float damagePerTick = 4f;

    [Header("Enemy Push")]
    public float enemyPushBackwardForce = 2.5f;
    public float enemyPushUpForce = 1.5f;
    public ForceMode enemyPushForceMode = ForceMode.Force;

    [Header("Ammo")]
    public int magazineSize = 30;
    public int totalAmmo = 300;
    public float ammoConsumePerSecond = 10f;
    public float reloadTime = 2f;
    public bool allowManualReload = true;
    public Key reloadKey = Key.R;
    public bool autoReloadWhenEmpty = true;

    [Header("Runtime Ammo")]
    public float currentMagazineAmmo;
    public int currentReserveAmmo;
    public bool isReloading = false;
    public float reloadProgress01 = 1f;

    [Header("Visual")]
    public ParticleSystem flameLoopParticles;

    [Header("Gamepad Control")]
    public bool allowGamepadInput = true;
    public float gamepadAimDeadZone = 0.2f;
    public bool invertGamepadX = false;
    public bool invertGamepadY = false;
    public float gamepadAimDistance = 18f;
    public float gamepadDownAngle = 4f;
    public float gamepadMinWorldDownAngle = 2.5f;
    public float gamepadMaxWorldUpAngle = 8f;
    public float triggerPressThreshold = 0.2f;

    [Header("Recoil")]
    public bool useRecoil = true;
    public Rigidbody recoilRb;
    public float recoilForcePerSecond = 5f;
    public ForceMode recoilForceMode = ForceMode.Force;
    public bool autoFindRecoilRb = true;

    [Header("Input Lock")]
    public float inputSwitchCooldown = 0.25f;

    private float nextDamageTickTime = 0f;
    private Vector3 lastGamepadFlatDir = Vector3.forward;
    private Vector3 lastMousePosition;
    private Vector3 currentAimPoint;

    private CarHealth myHealth;

    private enum AimInputDevice
    {
        None,
        Mouse,
        Gamepad
    }

    private AimInputDevice currentInputDevice = AimInputDevice.None;
    private float lastInputSwitchTime = -999f;
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

        if (flameTriggerZone == null)
            flameTriggerZone = GetComponentInChildren<FlamethrowerTriggerZone>();

        magazineSize = Mathf.Max(1, magazineSize);
        totalAmmo = Mathf.Max(0, totalAmmo);
        ammoConsumePerSecond = Mathf.Max(0.01f, ammoConsumePerSecond);
        reloadTime = Mathf.Max(0.01f, reloadTime);
        damageTickInterval = Mathf.Max(0.02f, damageTickInterval);
        damagePerTick = Mathf.Max(0f, damagePerTick);

        currentMagazineAmmo = magazineSize;
        currentReserveAmmo = totalAmmo;

        StopFlameLoopImmediate();
    }

    void OnEnable()
    {
        suppressQuestFireUntilRelease = IsQuestRightTriggerHeld();
    }

    void Update()
    {
        if (IsOwnerDead())
        {
            StopFlame();
            return;
        }

        if (aimCamera == null || yawPivot == null)
        {
            StopFlame();
            return;
        }

        if (allowManualReload && IsReloadPressedThisFrame())
        {
            TryStartReload();
        }

        UpdatePlayerAimAndFire();
    }

    void UpdatePlayerAimAndFire()
    {
        Vector2 stick = Vector2.zero;
        bool hasGamepadAim = false;

        if (allowGamepadInput)
        {
            stick = GetGamepadAimInput();
            hasGamepadAim = stick.magnitude >= gamepadAimDeadZone;
        }

        Vector3 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : lastMousePosition;
        bool hasMouseAim = (mousePosition - lastMousePosition).sqrMagnitude > 0.01f;
        bool mouseFire = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool questFireHeld = IsQuestRightTriggerHeld();

        if (suppressQuestFireUntilRelease)
        {
            if (questFireHeld)
                questFireHeld = false;
            else
                suppressQuestFireUntilRelease = false;
        }

        bool gamepadFire = questFireHeld;

        if (hasGamepadAim || questFireHeld)
        {
            if (currentInputDevice != AimInputDevice.Gamepad)
            {
                currentInputDevice = AimInputDevice.Gamepad;
                lastInputSwitchTime = Time.time;
            }
        }
        else if (hasMouseAim || mouseFire)
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
            }

            else if (questFireHeld)
            {
                Vector3 flatDir = yawPivot != null ? yawPivot.forward : transform.forward;
                flatDir.y = 0f;

                if (flatDir.sqrMagnitude < 0.0001f)
                    flatDir = transform.forward;

                flatDir.Normalize();
                RotatePitchToDirection(GetFinalGamepadShootDirection(flatDir));
            }

            if (gamepadFire)
            {
                TryFireGamepad();
            }
            else if (!inputLocked && (hasMouseAim || mouseFire))
            {
                currentInputDevice = AimInputDevice.Mouse;
                lastInputSwitchTime = Time.time;
            }
            else
            {
                StopFlame();
            }
        }
        else
        {
            Vector3 realAimPoint = GetMouseWorldAimPoint();
            Vector3 yawAimPoint = GetYawOnlyAimPoint(realAimPoint);

            RotateYaw(yawAimPoint);
            RotatePitch(realAimPoint);

            if (mouseFire)
            {
                TryFireMouse();
            }
            else
            {
                StopFlame();
            }

            if (!inputLocked && (hasGamepadAim || questFireHeld))
            {
                currentInputDevice = AimInputDevice.Gamepad;
                lastInputSwitchTime = Time.time;
            }
        }

        lastMousePosition = mousePosition;
    }

    void TryFireMouse()
    {
        TryFireCommon(FireMode.Mouse);
    }

    void TryFireGamepad()
    {
        TryFireCommon(FireMode.Gamepad);
    }

    enum FireMode
    {
        Mouse,
        Gamepad
    }

    void TryFireCommon(FireMode mode)
    {
        if (IsOwnerDead()) return;
        if (isReloading)
        {
            StopFlame();
            return;
        }

        if (currentMagazineAmmo <= 0f)
        {
            StopFlame();
            TryAutoReloadIfNeeded();
            return;
        }

        StartFlame();

        currentMagazineAmmo -= ammoConsumePerSecond * Time.deltaTime;

        if (currentMagazineAmmo <= 0f)
        {
            currentMagazineAmmo = 0f;
            StopFlame();
            TryAutoReloadIfNeeded();
            return;
        }

        if (Time.time >= nextDamageTickTime)
        {
            nextDamageTickTime = Time.time + damageTickInterval;
            ApplyFlameDamageTick();
        }

        Vector3 recoilForward = firePoint != null ? firePoint.forward : transform.forward;
        ApplyRecoil(recoilForward);
    }

    void StartFlame()
    {
        if (flameLoopParticles != null && !flameLoopParticles.isPlaying)
            flameLoopParticles.Play();
    }

    void StopFlame()
    {
        if (flameLoopParticles != null && flameLoopParticles.isPlaying)
            flameLoopParticles.Stop();
    }

    void StopFlameLoopImmediate()
    {
        if (flameLoopParticles != null)
            flameLoopParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void ApplyFlameDamageTick()
    {
        if (flameTriggerZone == null) return;

        flameTriggerZone.ClearNulls();

        List<Collider> hits = flameTriggerZone.GetValidTargets();
        HashSet<CarHealth> damagedTargets = new HashSet<CarHealth>();

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 attackDir = firePoint != null ? firePoint.forward : transform.forward;
        attackDir.Normalize();

        for (int i = 0; i < hits.Count; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;
            if (((1 << col.gameObject.layer) & damageMask) == 0) continue;
            if (col.transform.root == transform.root) continue;

            Vector3 hitPoint = GetClosestPointSafe(col, origin);

            if (IsBlocked(origin, hitPoint, col))
                continue;

            CarHealth health = col.GetComponentInParent<CarHealth>();
            if (health != null && !damagedTargets.Contains(health))
            {
                health.TakeDamage(damagePerTick);
                damagedTargets.Add(health);
            }

            Rigidbody rb = col.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                Vector3 backwardDir = -attackDir;
                Vector3 pushDir = backwardDir * enemyPushBackwardForce + Vector3.up * enemyPushUpForce;

                if (pushDir.sqrMagnitude > 0.0001f)
                    rb.AddForce(pushDir, enemyPushForceMode);
            }
        }
    }

    bool CanReload()
    {
        if (isReloading) return false;
        if (currentMagazineAmmo >= magazineSize) return false;
        if (currentReserveAmmo <= 0) return false;
        return true;
    }

    public void TryStartReload()
    {
        if (!CanReload()) return;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        reloadProgress01 = 0f;
        StopFlame();

        float reloadStartTime = Time.time;
        float reloadEndTime = reloadStartTime + reloadTime;

        while (Time.time < reloadEndTime)
        {
            reloadProgress01 = Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime);
            yield return null;
        }

        reloadProgress01 = 1f;

        if (IsOwnerDead())
        {
            isReloading = false;
            yield break;
        }

        int need = Mathf.CeilToInt(magazineSize - currentMagazineAmmo);
        int load = Mathf.Min(need, currentReserveAmmo);

        currentMagazineAmmo += load;
        currentReserveAmmo -= load;

        currentMagazineAmmo = Mathf.Clamp(currentMagazineAmmo, 0f, magazineSize);
        currentReserveAmmo = Mathf.Max(0, currentReserveAmmo);

        isReloading = false;
    }

    void TryAutoReloadIfNeeded()
    {
        if (autoReloadWhenEmpty && !isReloading)
        {
            TryStartReload();
        }
    }

    bool IsReloadPressedThisFrame()
    {
        return Keyboard.current != null &&
               Keyboard.current[reloadKey].wasPressedThisFrame;
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

    Vector3 GetYawOnlyAimPoint(Vector3 realAimPoint)
    {
        Vector3 yawAimPoint = realAimPoint;
        yawAimPoint.y = yawPivot.position.y;
        return yawAimPoint;
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
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
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
        recoilRb.AddForce(recoilDir * recoilForcePerSecond * Time.deltaTime, recoilForceMode);
    }
}


