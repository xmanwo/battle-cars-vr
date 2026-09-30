using UnityEngine;
using UnityEngine.InputSystem;

public class CarWeaponSystem : MonoBehaviour
{
    [Header("Slots")]
    public Transform frontSlot;
    public Transform rearSlot;
    public Transform topSlot;
    public Transform leftSlot;
    public Transform rightSlot;
    public Transform rearDropPoint;

    [Header("Front Weapon Prefabs")]
    public GameObject spikeWeaponPrefab;
    public GameObject shovelWeaponPrefab;

    [Header("Rear Weapon Prefabs")]
    public GameObject mineLayerWeaponPrefab;

    [Header("Top Weapon Prefabs")]
    public GameObject topGunWeaponPrefab;

    [Header("Armor Prefabs")]
    public GameObject leftArmorPrefab;
    public GameObject rightArmorPrefab;

    [Header("Default Loadout")]
    public GameObject defaultFrontWeaponPrefab;
    public GameObject defaultRearWeaponPrefab;
    public GameObject defaultTopWeaponPrefab;
    public bool equipArmorOnStart = false;

    [Header("Runtime")]
    private GameObject currentFrontWeapon;
    private GameObject currentRearWeapon;
    private GameObject currentTopWeapon;
    private GameObject currentLeftArmor;
    private GameObject currentRightArmor;

    public GameObject CurrentFrontWeapon => currentFrontWeapon;
    public GameObject CurrentRearWeapon => currentRearWeapon;
    public GameObject CurrentTopWeapon => currentTopWeapon;
    public GameObject CurrentLeftArmor => currentLeftArmor;
    public GameObject CurrentRightArmor => currentRightArmor;

    [Header("Control")]
    public bool isPlayerControlled = false;

    void Start()
    {
        EquipDefaultLoadout();
    }

    void Update()
    {
        if (!isPlayerControlled) return;
        HandleWeaponSwitchInput();
    }

    void HandleWeaponSwitchInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            EquipFrontWeapon(spikeWeaponPrefab);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            EquipFrontWeapon(shovelWeaponPrefab);
        }

        if (Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            UnequipFrontWeapon();
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            EquipTopWeapon(topGunWeaponPrefab);
        }

        if (Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            UnequipTopWeapon();
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            EquipArmorPair();
        }

        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            UnequipArmorPair();
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            EquipRearWeapon(mineLayerWeaponPrefab);
        }

        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            UnequipRearWeapon();
        }
    }

    public void EquipDefaultLoadout()
    {
        if (defaultFrontWeaponPrefab != null)
            EquipFrontWeapon(defaultFrontWeaponPrefab);

        if (defaultRearWeaponPrefab != null)
            EquipRearWeapon(defaultRearWeaponPrefab);

        if (defaultTopWeaponPrefab != null)
            EquipTopWeapon(defaultTopWeaponPrefab);

        if (equipArmorOnStart)
            EquipArmorPair();
    }

    public void EquipFrontWeapon(GameObject weaponPrefab)
    {
        if (frontSlot == null || weaponPrefab == null)
            return;

        UnequipFrontWeapon();

        currentFrontWeapon = Instantiate(weaponPrefab, frontSlot);
        currentFrontWeapon.transform.localPosition = Vector3.zero;
        currentFrontWeapon.transform.localRotation = Quaternion.identity;
        currentFrontWeapon.transform.localScale = Vector3.one;

        AssignCollisionReference(currentFrontWeapon);
    }

    public void UnequipFrontWeapon()
    {
        if (currentFrontWeapon != null)
        {
            if (currentFrontWeapon.scene.IsValid())
            {
                Destroy(currentFrontWeapon);
            }

            currentFrontWeapon = null;
        }

        CarCollisionDamage collisionDamage = GetComponent<CarCollisionDamage>();
        if (collisionDamage != null)
        {
            collisionDamage.frontDamageMultiplier = 1f;
            collisionDamage.extraImpactUpForce = 0f;
            collisionDamage.extraImpactForwardForce = 0f;
            collisionDamage.weaponForceMinMultiplier = 0f;
            collisionDamage.weaponForceMaxMultiplier = 1f;
            collisionDamage.weaponForceMinSpeed = 3f;
            collisionDamage.weaponForceMaxSpeed = 20f;
        }
    }

    public void EquipRearWeapon(GameObject weaponPrefab)
    {
        if (rearSlot == null || weaponPrefab == null)
            return;

        UnequipRearWeapon();

        currentRearWeapon = Instantiate(weaponPrefab, rearSlot);
        currentRearWeapon.transform.localPosition = Vector3.zero;
        currentRearWeapon.transform.localRotation = Quaternion.identity;
        currentRearWeapon.transform.localScale = Vector3.one;

        AssignRearWeaponReferences(currentRearWeapon);
    }

    public void UnequipRearWeapon()
    {
        if (currentRearWeapon != null)
        {
            if (currentRearWeapon.scene.IsValid())
            {
                Destroy(currentRearWeapon);
            }

            currentRearWeapon = null;
        }
    }

    public void EquipTopWeapon(GameObject weaponPrefab)
    {
        if (topSlot == null || weaponPrefab == null)
            return;

        UnequipTopWeapon();

        currentTopWeapon = Instantiate(weaponPrefab, topSlot);
        currentTopWeapon.transform.localPosition = Vector3.zero;
        currentTopWeapon.transform.localRotation = Quaternion.identity;
        currentTopWeapon.transform.localScale = Vector3.one;

        AssignTopWeaponReferences(currentTopWeapon);
    }

    public void UnequipTopWeapon()
    {
        if (currentTopWeapon != null)
        {
            if (currentTopWeapon.scene.IsValid())
            {
                Destroy(currentTopWeapon);
            }

            currentTopWeapon = null;
        }
    }

    public void EquipArmorPair()
    {
        UnequipArmorPair();

        if (leftSlot != null && leftArmorPrefab != null)
        {
            currentLeftArmor = Instantiate(leftArmorPrefab, leftSlot);
            currentLeftArmor.transform.localPosition = Vector3.zero;
            currentLeftArmor.transform.localRotation = Quaternion.identity;
            currentLeftArmor.transform.localScale = Vector3.one;

            AssignArmorReference(currentLeftArmor);
        }

        if (rightSlot != null && rightArmorPrefab != null)
        {
            currentRightArmor = Instantiate(rightArmorPrefab, rightSlot);
            currentRightArmor.transform.localPosition = Vector3.zero;
            currentRightArmor.transform.localRotation = Quaternion.identity;
            currentRightArmor.transform.localScale = Vector3.one;
        }
    }

    public void UnequipArmorPair()
    {
        if (currentLeftArmor != null)
        {
            if (currentLeftArmor.scene.IsValid())
            {
                Destroy(currentLeftArmor);
            }

            currentLeftArmor = null;
        }

        if (currentRightArmor != null)
        {
            if (currentRightArmor.scene.IsValid())
            {
                Destroy(currentRightArmor);
            }

            currentRightArmor = null;
        }
    }

    void AssignCollisionReference(GameObject weaponObj)
    {
        CarCollisionDamage collisionDamage = GetComponent<CarCollisionDamage>();
        if (collisionDamage == null) return;

        SpikeWeapon spike = weaponObj.GetComponent<SpikeWeapon>();
        if (spike != null)
        {
            spike.collisionDamage = collisionDamage;
        }

        ShovelWeapon shovel = weaponObj.GetComponent<ShovelWeapon>();
        if (shovel != null)
        {
            shovel.collisionDamage = collisionDamage;
        }
    }

    void AssignRearWeaponReferences(GameObject weaponObj)
    {
        MineLayerWeapon mineLayer = weaponObj.GetComponent<MineLayerWeapon>();
        if (mineLayer != null)
        {
            mineLayer.dropPoint = rearDropPoint;

            if (mineLayer.ownerRb == null)
            {
                mineLayer.ownerRb = GetComponent<Rigidbody>();
            }

            if (mineLayer.ownerHealth == null)
            {
                mineLayer.ownerHealth = GetComponent<CarHealth>();
            }
        }
    }

    void AssignArmorReference(GameObject armorObj)
    {
        ArmorModule armor = armorObj.GetComponent<ArmorModule>();
        if (armor != null)
        {
            armor.carHealth = GetComponent<CarHealth>();
        }
    }

    void AssignTopWeaponReferences(GameObject weaponObj)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            mainCam = FindFirstObjectByType<Camera>();

        Rigidbody carRb = GetComponent<Rigidbody>();

        TurretGunWeapon turret = weaponObj.GetComponent<TurretGunWeapon>();
        if (turret != null)
        {
            turret.isPlayerControlled = isPlayerControlled;

            if (turret.aimCamera == null)
            {
                if (mainCam != null)
                    turret.aimCamera = mainCam;
            }
        }

        ShotgunTurretWeapon shotgun = weaponObj.GetComponent<ShotgunTurretWeapon>();
        if (shotgun != null)
        {
            if (shotgun.aimCamera == null && mainCam != null)
                shotgun.aimCamera = mainCam;

            if (shotgun.recoilRb == null && carRb != null)
                shotgun.recoilRb = carRb;
        }

        FlamethrowerTurretWeapon flamethrower = weaponObj.GetComponent<FlamethrowerTurretWeapon>();
        if (flamethrower != null)
        {
            if (flamethrower.aimCamera == null && mainCam != null)
                flamethrower.aimCamera = mainCam;

            if (flamethrower.recoilRb == null && carRb != null)
                flamethrower.recoilRb = carRb;
        }
    }
}
