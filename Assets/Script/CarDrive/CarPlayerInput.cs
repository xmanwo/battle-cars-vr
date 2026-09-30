using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CarPlayerInput : MonoBehaviour
{
    public RaycastCarController car;

    [Header("Role")]
    public bool isPlayerCar = true;

    [Header("Quest Controller")]
    public float stickDeadZone = 0.35f;
    public float triggerDeadZone = 0.05f;

    [Header("Camera")]
    public Transform cameraTransform;
    public bool autoBindMainCamera = true;

    [Header("Self Righting")]
    public bool allowSelfRightingInput = false;
    private bool lastLeftSecondaryPressed = false;

    void Awake()
    {
        if (car == null)
            car = GetComponent<RaycastCarController>();

        ApplyRoleSettings();
        TryBindCamera();
    }

    void OnEnable()
    {
        ApplyRoleSettings();
        TryBindCamera();
    }

    void OnDisable()
    {
        ApplyRoleSettings();
    }

    void OnValidate()
    {
        ApplyRoleSettingsInEditor();
    }

    void Update()
    {
        if (car == null) return;
        if (!IsActuallyPlayerCar()) return;

        if (cameraTransform == null && autoBindMainCamera)
        {
            TryBindCamera();
        }

        Vector2 moveInput = GetXRMoveStick();
        float brakeAxis = GetXRBrake();

        if (IsXRButtonPressedThisFrame(XRNode.LeftHand, CommonUsages.secondaryButton, ref lastLeftSecondaryPressed))
        {
            car.RequestSelfRight();
        }

        HandleQuestControllerInput(moveInput, brakeAxis);
    }

    bool IsActuallyPlayerCar()
    {
        return isPlayerCar && isActiveAndEnabled;
    }

    void TryBindCamera()
    {
        if (!IsActuallyPlayerCar()) return;
        if (!autoBindMainCamera) return;
        if (cameraTransform != null) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
        }
    }

    void ApplyRoleSettings()
    {
        bool actuallyPlayer = IsActuallyPlayerCar();

        CarWeaponSystem weaponSystem = GetComponent<CarWeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.isPlayerControlled = actuallyPlayer;

            if (weaponSystem.CurrentTopWeapon != null)
            {
                TurretGunWeapon turret = weaponSystem.CurrentTopWeapon.GetComponent<TurretGunWeapon>();
                if (turret != null)
                {
                    turret.isPlayerControlled = actuallyPlayer;
                }
            }
        }

        CarCollisionDamage collisionDamage = GetComponent<CarCollisionDamage>();
        if (collisionDamage != null)
        {
            collisionDamage.isEnemy = !actuallyPlayer;
        }

        if (car != null)
        {
            car.allowSelfRighting = actuallyPlayer && allowSelfRightingInput;
        }
    }

    void ApplyRoleSettingsInEditor()
    {
#if UNITY_EDITOR
        bool actuallyPlayer = isPlayerCar && enabled && gameObject.activeInHierarchy;

        CarWeaponSystem weaponSystem = GetComponent<CarWeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.isPlayerControlled = actuallyPlayer;
        }

        CarCollisionDamage collisionDamage = GetComponent<CarCollisionDamage>();
        if (collisionDamage != null)
        {
            collisionDamage.isEnemy = !actuallyPlayer;
        }

        if (car == null)
            car = GetComponent<RaycastCarController>();

        if (car != null)
        {
            car.allowSelfRighting = actuallyPlayer && allowSelfRightingInput;
        }
#endif
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

    Vector2 GetXRMoveStick()
    {
        var leftDevice = GetXRDevice(XRNode.LeftHand);
        if (!leftDevice.HasValue) return Vector2.zero;

        Vector2 axis;
        if (leftDevice.Value.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out axis))
            return axis;

        return Vector2.zero;
    }

    float GetXRBrake()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];

            if (!device.isValid)
                continue;

            // 必须是左手控制器
            if ((device.characteristics & InputDeviceCharacteristics.Left) == 0)
                continue;

            if ((device.characteristics & InputDeviceCharacteristics.Controller) == 0)
                continue;

            float triggerValue;
            if (device.TryGetFeatureValue(CommonUsages.trigger, out triggerValue))
            {
                return triggerValue;
            }
        }

        return 0f;
    }

    void HandleQuestControllerInput(Vector2 rawAxisInput, float brake)
    {
        Vector2 stickInput;

        if (rawAxisInput.magnitude < stickDeadZone)
        {
            stickInput = Vector2.zero;
        }
        else
        {
            float mappedMag = Mathf.InverseLerp(stickDeadZone, 1f, rawAxisInput.magnitude);
            stickInput = rawAxisInput.normalized * mappedMag;
        }

        float throttle = stickInput.magnitude;

        brake = Mathf.Clamp01(brake);

        if (brake > triggerDeadZone)
        {
            throttle = 0f;
        }

        Vector2 localOmniInput = ConvertWorldStickInputToCarLocal(stickInput);

        car.omniDriveMode = true;
        car.SetOmniInput(localOmniInput);
        car.SetInput(throttle, 0f);
        car.SetBrakeInput(brake);
    }

    bool IsXRButtonPressedThisFrame(XRNode node, InputFeatureUsage<bool> usage, ref bool lastPressed)
    {
        var device = GetXRDevice(node);

        if (!device.HasValue)
        {
            lastPressed = false;
            return false;
        }

        bool pressed;
        if (device.Value.TryGetFeatureValue(usage, out pressed))
        {
            bool pressedThisFrame = pressed && !lastPressed;
            lastPressed = pressed;
            return pressedThisFrame;
        }

        lastPressed = false;
        return false;
    }

    Vector2 ConvertWorldStickInputToCarLocal(Vector2 stickInput)
    {
        if (stickInput.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        Vector3 worldMoveDir = new Vector3(stickInput.x, 0f, stickInput.y);

        if (worldMoveDir.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        worldMoveDir.Normalize();

        Vector3 localDir = car.transform.InverseTransformDirection(worldMoveDir);

        return new Vector2(localDir.x, localDir.z).normalized * stickInput.magnitude;
    }

    float GetXRThrottle()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];

            if (!device.isValid)
                continue;

            if ((device.characteristics & InputDeviceCharacteristics.Right) == 0)
                continue;

            if ((device.characteristics & InputDeviceCharacteristics.Controller) == 0)
                continue;

            float triggerValue;
            if (device.TryGetFeatureValue(CommonUsages.trigger, out triggerValue))
            {
                return triggerValue;
            }
        }

        return 0f;
    }
}