using UnityEngine;
using UnityEngine.UI;

public class CarHealthBar : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;
    public CarHealth carHealth;
    public Transform target;

    public Vector3 offset = new Vector3(0, 2f, 0);
    public bool showPlayerWeaponBar = true;
    public Vector2 weaponBarOffset = new Vector2(0f, 10f);
    public float weaponBarHeight = 5f;
    public Color initialGunColor = new Color(0.35f, 0.75f, 1f, 1f);
    public Color shotgunColor = new Color(0.2f, 0.95f, 0.35f, 1f);
    public Color flamethrowerColor = new Color(1f, 0.78f, 0.2f, 1f);

    private Image weaponBarBackground;
    private Image weaponFillImage;
    private RectTransform weaponFillRect;
    private RectTransform weaponBarRect;
    private float weaponBarFullWidth = 1f;
    private CarWeaponSystem weaponSystem;

    void Start()
    {
        initialGunColor = new Color(0.35f, 0.75f, 1f, 1f);
        shotgunColor = new Color(0.2f, 0.95f, 0.35f, 1f);
        flamethrowerColor = new Color(1f, 0.78f, 0.2f, 1f);
        RefreshSliderLimits();
    }

    void Update()
    {
        if (slider == null || carHealth == null || target == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(carHealth.currentHealth > 0);
        if (!gameObject.activeSelf)
            return;

        RefreshSliderLimits();
        slider.value = carHealth.currentHealth;
        UpdateWeaponBar();

        transform.position = target.position + offset;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0.001f)
            transform.forward = directionToCamera.normalized;
    }

    void RefreshSliderLimits()
    {
        if (slider == null || carHealth == null)
            return;

        slider.maxValue = carHealth.maxHealth;
        slider.value = carHealth.currentHealth;
    }

    public void SetFillColor(Color color)
    {
        if (fillImage != null)
            fillImage.color = color;
    }

    void UpdateWeaponBar()
    {
        if (!showPlayerWeaponBar || !IsPlayerHealthBar())
        {
            SetWeaponBarVisible(false);
            return;
        }

        EnsureWeaponBar();

        if (weaponFillImage == null)
            return;

        if (weaponSystem == null && carHealth != null)
            weaponSystem = carHealth.GetComponent<CarWeaponSystem>();

        GameObject topWeapon = weaponSystem != null ? weaponSystem.CurrentTopWeapon : null;
        if (topWeapon == null)
        {
            SetWeaponBarVisible(false);
            return;
        }

        if (TryReadTurretGun(topWeapon, out float initialValue))
        {
            SetWeaponBarVisible(true);
            weaponFillImage.color = initialGunColor;
            SetWeaponBarFill(initialValue);
            return;
        }

        if (TryReadShotgun(topWeapon, out float shotgunValue))
        {
            SetWeaponBarVisible(true);
            weaponFillImage.color = shotgunColor;
            SetWeaponBarFill(shotgunValue);
            return;
        }

        if (TryReadFlamethrower(topWeapon, out float flamethrowerValue))
        {
            SetWeaponBarVisible(true);
            weaponFillImage.color = flamethrowerColor;
            SetWeaponBarFill(flamethrowerValue);
            return;
        }

        SetWeaponBarVisible(false);
    }

    bool IsPlayerHealthBar()
    {
        if (carHealth == null)
            return false;

        CarPlayerInput input = carHealth.GetComponent<CarPlayerInput>();
        if (input != null && input.isPlayerCar)
            return true;

        if (weaponSystem == null)
            weaponSystem = carHealth.GetComponent<CarWeaponSystem>();

        if (weaponSystem != null && weaponSystem.isPlayerControlled)
            return true;

        CarPlayerInput[] players = FindObjectsByType<CarPlayerInput>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || !players[i].isPlayerCar)
                continue;

            CarHealth activePlayerHealth = players[i].GetComponent<CarHealth>();
            if (activePlayerHealth == carHealth)
                return true;
        }

        return false;
    }

    bool TryReadTurretGun(GameObject topWeapon, out float value)
    {
        TurretGunWeapon gun = topWeapon.GetComponent<TurretGunWeapon>();
        if (gun == null)
        {
            value = 0f;
            return false;
        }

        value = gun.isReloading
            ? gun.reloadProgress01
            : SafeRatio(gun.currentMagazineAmmo, gun.magazineSize);

        return true;
    }

    bool TryReadShotgun(GameObject topWeapon, out float value)
    {
        ShotgunTurretWeapon shotgun = topWeapon.GetComponent<ShotgunTurretWeapon>();
        if (shotgun == null)
        {
            value = 0f;
            return false;
        }

        value = shotgun.consumeAmmo && shotgun.currentAmmo <= 0
            ? 0f
            : shotgun.GetCooldownProgress01();

        return true;
    }

    bool TryReadFlamethrower(GameObject topWeapon, out float value)
    {
        FlamethrowerTurretWeapon flamethrower = topWeapon.GetComponent<FlamethrowerTurretWeapon>();
        if (flamethrower == null)
        {
            value = 0f;
            return false;
        }

        value = flamethrower.isReloading
            ? flamethrower.reloadProgress01
            : SafeRatio(flamethrower.currentMagazineAmmo, flamethrower.magazineSize);

        return true;
    }

    float SafeRatio(float current, float max)
    {
        if (max <= 0f)
            return 0f;

        return Mathf.Clamp01(current / max);
    }

    void EnsureWeaponBar()
    {
        if (weaponFillImage != null || slider == null)
            return;

        RectTransform sourceRect = GetHealthBarReferenceRect();
        if (sourceRect == null)
            return;

        GameObject backgroundObj = new GameObject("WeaponBarBackground", typeof(RectTransform), typeof(Image));
        backgroundObj.transform.SetParent(transform, false);

        weaponBarRect = backgroundObj.GetComponent<RectTransform>();
        CopyRectTransform(sourceRect, weaponBarRect);
        weaponBarRect.anchoredPosition += weaponBarOffset;
        weaponBarRect.sizeDelta = new Vector2(weaponBarRect.sizeDelta.x, weaponBarHeight);

        weaponBarFullWidth = Mathf.Max(1f, sourceRect.rect.width);

        weaponBarBackground = backgroundObj.GetComponent<Image>();
        weaponBarBackground.type = Image.Type.Simple;
        weaponBarBackground.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject fillObj = new GameObject("WeaponBarFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(backgroundObj.transform, false);

        weaponFillRect = fillObj.GetComponent<RectTransform>();
        weaponFillRect.anchorMin = new Vector2(1f, 0f);
        weaponFillRect.anchorMax = new Vector2(1f, 1f);
        weaponFillRect.pivot = new Vector2(1f, 0.5f);
        weaponFillRect.anchoredPosition = Vector2.zero;
        weaponFillRect.sizeDelta = new Vector2(weaponBarFullWidth, 0f);

        weaponFillImage = fillObj.GetComponent<Image>();
        weaponFillImage.type = Image.Type.Simple;
        weaponFillImage.color = initialGunColor;
        SetWeaponBarFill(1f);
    }

    RectTransform GetHealthBarReferenceRect()
    {
        if (fillImage != null && fillImage.transform.parent is RectTransform fillAreaRect)
            return fillAreaRect;

        if (slider != null)
        {
            Transform background = slider.transform.Find("Background");
            if (background is RectTransform backgroundRect)
                return backgroundRect;
        }

        return slider != null ? slider.transform as RectTransform : null;
    }

    void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    void SetWeaponBarVisible(bool visible)
    {
        if (weaponFillImage != null)
            weaponFillImage.gameObject.SetActive(visible);

        if (weaponBarBackground != null)
            weaponBarBackground.gameObject.SetActive(visible);
    }

    void SetWeaponBarFill(float value)
    {
        if (weaponFillRect == null)
            return;

        value = Mathf.Clamp01(value);
        weaponFillRect.sizeDelta = new Vector2(weaponBarFullWidth * value, 0f);
    }
}
