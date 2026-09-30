using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections.Generic;

public class MineLayerWeapon : MonoBehaviour
{
    [Header("Refs")]
    public Transform dropPoint;
    public GameObject minePrefab;
    public Rigidbody ownerRb;
    public CarHealth ownerHealth;

    [Header("Drop")]
    public float dropCooldown = 2f;
    public float backwardThrowForce = 4f;
    public float upwardThrowForce = 0.8f;
    public bool inheritOwnerVelocity = true;

    [Header("Keyboard")]
    public Key keyboardDropKey = Key.E;

    private float nextDropTime = 0f;
    private bool lastRightSecondaryPressed = false;

    void Awake()
    {
        if (ownerHealth == null)
            ownerHealth = GetComponentInParent<CarHealth>();

        if (ownerRb == null)
            ownerRb = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (dropPoint == null || minePrefab == null) return;
        if (ownerHealth != null && ownerHealth.IsDead) return;

        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current[keyboardDropKey].wasPressedThisFrame;

        bool questBPressed = IsXRButtonPressedThisFrame(
            XRNode.RightHand,
            UnityEngine.XR.CommonUsages.secondaryButton,
            ref lastRightSecondaryPressed
        );

        if (keyboardPressed || questBPressed)
        {
            TryDropMine();
        }
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

    bool IsXRButtonPressedThisFrame(
        XRNode node,
        UnityEngine.XR.InputFeatureUsage<bool> usage,
        ref bool lastPressed)
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

    void TryDropMine()
    {
        if (ownerHealth != null && ownerHealth.IsDead) return;
        if (Time.time < nextDropTime) return;

        nextDropTime = Time.time + dropCooldown;

        Vector3 startVelocity = Vector3.zero;

        if (inheritOwnerVelocity && ownerRb != null)
            startVelocity += ownerRb.linearVelocity;

        startVelocity += -transform.root.forward * backwardThrowForce;
        startVelocity += transform.root.up * upwardThrowForce;

        Mine mine = null;

        if (MinePool.Instance != null)
        {
            mine = MinePool.Instance.GetMine(dropPoint.position, dropPoint.rotation);
        }
        else
        {
            GameObject mineObj = Instantiate(minePrefab, dropPoint.position, dropPoint.rotation);
            mine = mineObj.GetComponent<Mine>();
        }

        if (mine != null)
        {
            mine.InitFromPool(transform.root, startVelocity);
        }
    }
}