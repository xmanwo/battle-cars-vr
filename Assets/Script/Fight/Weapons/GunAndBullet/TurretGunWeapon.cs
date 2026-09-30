using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

public class TurretGunWeapon : MonoBehaviour
{
    [Header("Refs")]
    public Camera aimCamera;
    public Transform yawPivot;
    public Transform pitchPivot;
    public Transform[] firePoints;

    [Header("Aim")]
    public float maxAimDistance = 200f;
    public float yawSpeed = 180f;
    public float pitchSpeed = 180f;
    public float minPitch = -25f;
    public float maxPitch = 25f;

    [Header("Fire")]
    public float fireRate = 10f;
    public float damage = 5f;
    public float fireRange = 120f;
    public float hitForce = 2f;
    public LayerMask hitMask;

    [Header("Fire Audio")]
    public AudioSource fireAudioSource;
    public AudioClip fireSound;
    [Range(0f, 1f)] public float fireVolume = 1f;
    public Vector2 firePitchRange = new Vector2(0.95f, 1.05f);

    [Header("Ammo")]
    public int magazineSize = 100;
    public int totalAmmo = 500;
    public float reloadTime = 1.5f;
    public bool allowManualReload = false;
    public bool autoReloadWhenEmpty = true;

    [Header("Runtime Ammo")]
    public int currentMagazineAmmo;
    public int currentReserveAmmo;
    public bool isReloading = false;
    public float reloadProgress01 = 1f;

    [Header("Accuracy")]
    [Range(0f, 100f)] public float accuracy = 60f;
    public float maxSpreadAngle = 6f;
    public bool useSpread = true;

    [Header("Bullet Visual")]
    public GameObject bulletProjectilePrefab;
    public GameObject aiBulletProjectilePrefab;
    public float bulletSpeed = 100f;
    public float missDistance = 80f;

    [Header("Control")]
    public bool isPlayerControlled = true;

    [Header("AI Control")]
    public bool isAIControlled = false;
    public Transform aiTarget;
    public bool aiWantsToFire = false;
    public float aiTargetHeight = 0.8f;
    public bool aiUsePitch = false;
    public float aiFireDotThreshold = 0.92f;

    [Header("Quest Control")]
    public float aimDeadZone = 0.2f;
    public bool invertX = false;
    public bool invertY = true;
    public float aimDistance = 80f;
    public float downAngle = 3.5f;
    public float minWorldDownAngle = 2.5f;
    public float maxWorldUpAngle = 8f;
    public float triggerPressThreshold = 0.2f;

    [Header("Quest Aim Basis")]
    public Transform aimReference;
    public bool useAimReference = true;
    public string aimReferenceName = "ArenaAimReference";

    [Header("Reload Button")]
    public bool useLeftXToReload = true;

    [Header("Recoil")]
    public bool useRecoil = true;
    public Rigidbody recoilRb;
    public float recoilForce = 2.5f;
    public ForceMode recoilForceMode = ForceMode.Impulse;
    public bool autoFindRecoilRb = true;
    public bool recoilUseFirePointForward = true;
    public float recoilPerExtraBarrel = 0.35f;

    private float nextFireTime = 0f;
    private Vector3 lastFlatAimDir = Vector3.forward;
    private CarHealth myHealth;
    private bool lastLeftPrimaryPressed = false;

    void Awake()
    {
        myHealth = GetComponentInParent<CarHealth>();

        TryFindAimReference();

        if (fireAudioSource == null)
            fireAudioSource = GetComponent<AudioSource>();

        if (fireAudioSource != null)
        {
            fireAudioSource.playOnAwake = false;
            fireAudioSource.spatialBlend = 1f;
        }

        TryAutoBindRecoilRb();

        magazineSize = Mathf.Max(1, magazineSize);
        totalAmmo = Mathf.Max(0, totalAmmo);
        reloadTime = Mathf.Max(0.01f, reloadTime);

        currentMagazineAmmo = magazineSize;
        currentReserveAmmo = totalAmmo;
    }

    void Update()
    {
        if (IsOwnerDead())
        {
            aiWantsToFire = false;
            return;
        }

        if (!isAIControlled && isPlayerControlled && allowManualReload && useLeftXToReload && IsReloadPressedThisFrame())
        {
            TryStartReload();
        }

        if (isAIControlled)
        {
            UpdateAI();
            return;
        }

        if (!isPlayerControlled) return;
        if (aimCamera == null || yawPivot == null) return;

        Transform[] points = GetValidFirePoints();
        if (points == null) return;

        Vector2 stick = GetQuestRightStick();
        bool hasAim = stick.magnitude >= aimDeadZone;
        bool fireHeld = IsQuestRightTriggerHeld();

        if (hasAim)
        {
            Vector3 flatDir = GetQuestFlatDirection(stick);
            Vector3 realAimPoint = yawPivot.position + flatDir * aimDistance;
            Vector3 yawAimPoint = GetYawOnlyAimPoint(realAimPoint);

            RotateYaw(yawAimPoint);

            Vector3 finalShootDir = GetFinalQuestShootDirection(flatDir);
            RotatePitchToDirection(finalShootDir);
        }

        if (fireHeld)
        {
            TryFireQuest();
        }
    }

    bool IsOwnerDead()
    {
        return myHealth != null && myHealth.IsDead;
    }

    bool IsTargetDead()
    {
        if (aiTarget == null) return true;

        CarHealth targetHealth = aiTarget.GetComponentInParent<CarHealth>();
        if (targetHealth == null)
            targetHealth = aiTarget.GetComponent<CarHealth>();

        return targetHealth != null && targetHealth.IsDead;
    }

    void ClearAITargetAndStopFire()
    {
        aiTarget = null;
        aiWantsToFire = false;
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

        int need = magazineSize - currentMagazineAmmo;
        int load = Mathf.Min(need, currentReserveAmmo);

        currentMagazineAmmo += load;
        currentReserveAmmo -= load;

        isReloading = false;
    }

    void TryAutoReloadIfNeeded()
    {
        if (autoReloadWhenEmpty && !isReloading)
        {
            TryStartReload();
        }
    }

    void UpdateAI()
    {
        if (IsOwnerDead())
        {
            ClearAITargetAndStopFire();
            return;
        }

        if (IsTargetDead())
        {
            ClearAITargetAndStopFire();
            return;
        }

        if (aiTarget == null || yawPivot == null) return;

        Transform[] points = GetValidFirePoints();
        if (points == null) return;

        Vector3 targetAimPos = aiTarget.position + Vector3.up * aiTargetHeight;

        Vector3 flatDir = targetAimPos - yawPivot.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.0001f) return;
        flatDir.Normalize();

        Vector3 yawAimPoint = yawPivot.position + flatDir * 10f;
        RotateYaw(yawAimPoint);

        if (aiUsePitch)
        {
            RotatePitch(targetAimPos);
        }

        if (!aiWantsToFire) return;

        Vector3 fireDir = targetAimPos - points[0].position;
        if (fireDir.sqrMagnitude < 0.0001f) return;
        fireDir.Normalize();

        Vector3 fireFlat = fireDir;
        fireFlat.y = 0f;

        if (fireFlat.sqrMagnitude < 0.0001f) return;
        fireFlat.Normalize();

        Vector3 gunFlat = points[0].forward;
        gunFlat.y = 0f;

        if (gunFlat.sqrMagnitude < 0.0001f) return;
        gunFlat.Normalize();

        float flatDot = Vector3.Dot(gunFlat, fireFlat);

        if (flatDot >= aiFireDotThreshold)
        {
            TryFireAI(fireDir);
        }
    }

    public void SetAIControl(bool enabled)
    {
        isAIControlled = enabled;
        if (enabled)
            isPlayerControlled = false;
    }

    public void SetAITarget(Transform target)
    {
        aiTarget = target;
    }

    public void SetAIFire(bool firing)
    {
        if (IsOwnerDead())
        {
            aiWantsToFire = false;
            return;
        }

        if (IsTargetDead())
        {
            aiWantsToFire = false;
            return;
        }

        aiWantsToFire = firing;
    }

    void TryFindAimReference()
    {
        if (!useAimReference)
            return;

        if (aimReference != null)
            return;

        GameObject found = GameObject.Find(aimReferenceName);

        if (found != null)
        {
            aimReference = found.transform;
        }
        else
        {
            Debug.LogWarning(
                "TurretGunWeapon: Cannot find aim reference named " + aimReferenceName +
                ". Falling back to turret transform direction.",
                this
            );
        }
    }

    bool IsReloadPressedThisFrame()
    {
        var leftDevice = GetXRDevice(XRNode.LeftHand);
        if (!leftDevice.HasValue)
        {
            lastLeftPrimaryPressed = false;
            return false;
        }

        bool pressed;
        if (leftDevice.Value.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out pressed))
        {
            bool pressedThisFrame = pressed && !lastLeftPrimaryPressed;
            lastLeftPrimaryPressed = pressed;
            return pressedThisFrame;
        }

        lastLeftPrimaryPressed = false;
        return false;
    }

    UnityEngine.XR.InputDevice? GetXRDevice(XRNode node)
    {
        List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);

        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].isValid)
                return devices[i];
        }

        return null;
    }

    Vector2 GetQuestRightStick()
    {
        var rightDevice = GetXRDevice(XRNode.RightHand);
        if (!rightDevice.HasValue) return Vector2.zero;

        Vector2 stick;
        if (rightDevice.Value.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out stick))
        {
            float x = stick.x;
            float y = stick.y;

            if (invertX) x = -x;
            if (invertY) y = -y;

            return new Vector2(x, y);
        }

        return Vector2.zero;
    }

    bool IsQuestRightTriggerHeld()
    {
        var rightDevice = GetXRDevice(XRNode.RightHand);
        if (!rightDevice.HasValue) return false;

        float trigger;
        if (rightDevice.Value.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out trigger))
            return trigger > triggerPressThreshold;

        return false;
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

    enum FireMode
    {
        Quest,
        AI
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    Vector3 GetQuestFlatDirection(Vector2 stick)
    {
        if (useAimReference && aimReference == null)
            TryFindAimReference();

        Transform basis = null;

        if (useAimReference && aimReference != null)
        {
            basis = aimReference;
        }
        else
        {
            basis = transform;
        }

        Vector3 basisForward = basis.forward;
        Vector3 basisRight = basis.right;

        basisForward.y = 0f;
        basisRight.y = 0f;

        if (basisForward.sqrMagnitude < 0.0001f)
            basisForward = Vector3.forward;

        if (basisRight.sqrMagnitude < 0.0001f)
            basisRight = Vector3.right;

        basisForward.Normalize();
        basisRight.Normalize();

        Vector3 flatDir = basisRight * stick.x + basisForward * stick.y;

        if (flatDir.sqrMagnitude < 0.0001f)
            return lastFlatAimDir;

        flatDir.Normalize();
        lastFlatAimDir = flatDir;
        return flatDir;
    }

    void TryFireQuest()
    {
        TryFireCommon(FireMode.Quest, Vector3.zero);
    }

    void FireQuestFromPoint(Transform fp)
    {
        if (fp == null) return;

        Vector3 flatDir = yawPivot != null ? yawPivot.forward : transform.forward;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = transform.forward;

        flatDir.y = 0f;
        flatDir.Normalize();

        Vector3 shootDir = GetFinalQuestShootDirection(flatDir);
        shootDir = ApplyRandomHorizontalSpread(shootDir);

        Vector3 bulletEndPoint = fp.position + shootDir * missDistance;
        CarHealth targetHealth = null;
        Rigidbody targetRb = null;

        Transform hitTarget = null;
        Vector3 localHitOffset = Vector3.zero;
        Mine targetMine = null;

        Ray ray = new Ray(fp.position, shootDir);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            fireRange,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                if (hit.collider.transform.root == transform.root)
                    continue;

                bulletEndPoint = hit.point;

                Mine hitMine = hit.collider.GetComponentInParent<Mine>();
                if (hitMine != null)
                {
                    targetMine = hitMine;
                    targetHealth = null;
                    targetRb = null;

                    hitTarget = hit.collider.transform.root;
                    localHitOffset = hitTarget.InverseTransformPoint(hit.point);
                    break;
                }

                targetHealth = hit.collider.GetComponentInParent<CarHealth>();
                targetRb = hit.collider.GetComponentInParent<Rigidbody>();

                hitTarget = hit.collider.transform.root;
                localHitOffset = hitTarget.InverseTransformPoint(hit.point);

                break;
            }
        }

        SpawnBulletVisual(
            fp.position,
            bulletEndPoint,
            targetHealth,
            targetRb,
            shootDir,
            hitTarget,
            localHitOffset,
            targetMine
        );
    }

    void TryFireAI(Vector3 aimDir)
    {
        TryFireCommon(FireMode.AI, aimDir);
    }

    void PlayFireSound()
    {
        if (fireAudioSource == null || fireSound == null)
            return;

        fireAudioSource.pitch = Random.Range(firePitchRange.x, firePitchRange.y);
        fireAudioSource.PlayOneShot(fireSound, fireVolume);
    }

    void TryFireCommon(FireMode mode, Vector3 aiAimDir)
    {
        if (IsOwnerDead()) return;
        if (isReloading) return;
        if (Time.time < nextFireTime) return;

        if (mode == FireMode.AI)
        {
            if (IsTargetDead()) return;

            if (aiAimDir.sqrMagnitude < 0.0001f) return;
            aiAimDir.Normalize();
        }

        Transform[] points = GetValidFirePoints();
        if (points == null) return;

        int burstCost = points.Length;
        if (currentMagazineAmmo < burstCost)
        {
            TryAutoReloadIfNeeded();
            return;
        }

        nextFireTime = Time.time + 1f / fireRate;
        currentMagazineAmmo = Mathf.Max(0, currentMagazineAmmo - burstCost);
        PlayFireSound();

        Vector3 recoilForward = transform.forward;
        if (recoilUseFirePointForward && points.Length > 0 && points[0] != null)
            recoilForward = points[0].forward;

        ApplyRecoil(recoilForward, points.Length);

        for (int i = 0; i < points.Length; i++)
        {
            Transform fp = points[i];
            if (fp == null) continue;

            switch (mode)
            {
                case FireMode.Quest:
                    FireQuestFromPoint(fp);
                    break;

                case FireMode.AI:
                    FireAIFromPoint(fp, aiAimDir);
                    break;
            }
        }

        if (currentMagazineAmmo < burstCost)
        {
            TryAutoReloadIfNeeded();
        }
    }

    void FireAIFromPoint(Transform fp, Vector3 aimDir)
    {
        if (fp == null) return;

        Vector3 shootDir = aimDir.normalized;
        shootDir = ApplyRandomHorizontalSpread(shootDir);

        if (aiBulletProjectilePrefab == null)
        {
            Debug.LogWarning("TurretGunWeapon: aiBulletProjectilePrefab Ã»ÓÐÖ¸¶¨¡£", this);
            return;
        }

        AIBulletProjectile bullet = null;

        if (AIBulletPool.Instance != null)
        {
            bullet = AIBulletPool.Instance.GetBullet(
                fp.position,
                Quaternion.LookRotation(shootDir, Vector3.up)
            );
        }
        else
        {
            GameObject bulletObj = Instantiate(
                aiBulletProjectilePrefab,
                fp.position,
                Quaternion.LookRotation(shootDir, Vector3.up)
            );

            bullet = bulletObj.GetComponent<AIBulletProjectile>();
        }

        if (bullet != null)
        {
            bullet.Init(
                shootDir,
                bulletSpeed,
                damage,
                hitForce,
                hitMask,
                transform.root
            );
        }
    }

    Transform[] GetValidFirePoints()
    {
        if (firePoints == null || firePoints.Length == 0)
            return null;

        int count = 0;
        for (int i = 0; i < firePoints.Length; i++)
        {
            if (firePoints[i] != null)
                count++;
        }

        if (count == 0) return null;

        Transform[] result = new Transform[count];
        int idx = 0;

        for (int i = 0; i < firePoints.Length; i++)
        {
            if (firePoints[i] != null)
                result[idx++] = firePoints[i];
        }

        return result;
    }

    Vector3 GetFinalQuestShootDirection(Vector3 flatDir)
    {
        if (flatDir.sqrMagnitude < 0.0001f)
            return yawPivot != null ? yawPivot.forward : transform.forward;

        flatDir.y = 0f;
        flatDir.Normalize();

        Vector3 pitchAxis = Vector3.Cross(flatDir, Vector3.up).normalized;
        if (pitchAxis.sqrMagnitude < 0.0001f)
            pitchAxis = transform.right;

        Vector3 shootDir = Quaternion.AngleAxis(-downAngle, pitchAxis) * flatDir;
        shootDir.Normalize();

        float worldPitch = Mathf.Asin(shootDir.y) * Mathf.Rad2Deg;
        float minAllowedWorldPitch = -minWorldDownAngle;
        float maxAllowedWorldPitch = maxWorldUpAngle;

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

    float GetCurrentSpreadAngle()
    {
        float spreadFactor = 1f - Mathf.Clamp01(accuracy / 100f);
        return maxSpreadAngle * spreadFactor;
    }

    Vector3 ApplyRandomHorizontalSpread(Vector3 baseDir)
    {
        if (!useSpread) return baseDir.normalized;

        baseDir = baseDir.normalized;

        float spreadAngle = GetCurrentSpreadAngle();
        if (spreadAngle <= 0.001f) return baseDir;

        float randomAngle = Random.Range(-spreadAngle, spreadAngle);

        Vector3 finalDir = Quaternion.AngleAxis(randomAngle, Vector3.up) * baseDir;
        return finalDir.normalized;
    }

    void SpawnBulletVisual(
        Vector3 startPoint,
        Vector3 endPoint,
        CarHealth targetHealth,
        Rigidbody targetRb,
        Vector3 shootDir,
        Transform hitTarget,
        Vector3 localHitOffset,
        Mine targetMine)
    {
        BulletVisualProjectile bullet = null;

        if (BulletVisualPool.Instance != null)
        {
            bullet = BulletVisualPool.Instance.GetBullet(
                startPoint,
                Quaternion.LookRotation(shootDir, Vector3.up)
            );
        }
        else
        {
            if (bulletProjectilePrefab == null) return;

            GameObject bulletObj = Instantiate(
                bulletProjectilePrefab,
                startPoint,
                Quaternion.LookRotation(shootDir, Vector3.up)
            );

            bullet = bulletObj.GetComponent<BulletVisualProjectile>();
        }

        if (bullet != null)
        {
            bullet.Init(
                startPoint,
                endPoint,
                bulletSpeed,
                targetHealth,
                damage,
                targetRb,
                shootDir,
                hitForce,
                hitTarget,
                localHitOffset,
                targetMine,
                targetMine != null ? targetMine.SpawnVersion : -1
            );
        }
    }

    void TryAutoBindRecoilRb()
    {
        if (!autoFindRecoilRb) return;
        if (recoilRb != null) return;

        recoilRb = GetComponentInParent<Rigidbody>();
    }

    void ApplyRecoil(Vector3 fireForward, int barrelCount)
    {
        if (!useRecoil) return;
        if (recoilRb == null) return;

        Vector3 recoilDir = -fireForward.normalized;

        float finalForce = recoilForce;

        if (barrelCount > 1)
            finalForce *= 1f + (barrelCount - 1) * recoilPerExtraBarrel;

        recoilRb.AddForce(recoilDir * finalForce, recoilForceMode);
    }
}