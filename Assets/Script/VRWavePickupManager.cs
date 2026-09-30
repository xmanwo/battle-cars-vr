using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;

[System.Serializable]
public class VREnemySpawnInfo
{
    public GameObject enemyPrefab;
    public int enemyCount = 1;
}

[System.Serializable]
public class VRWaveInfo
{
    [Header("Enemies In This Wave")]
    public VREnemySpawnInfo[] enemies;
}

[System.Serializable]
public class VRCarSelectionOption
{
    public string displayName = "Car";
    public string trait = "Balanced";
    public GameObject carPrefab;
    public int maxHealth = 350;
    public float maxSpeed = 18f;
    public float driveForce = 60f;
    public float turnTorque = 20f;
    public float lateralGrip = 8f;
}

public class VRWavePickupManager : MonoBehaviour
{
    [Header("Waves")]
    public VRWaveInfo[] waves;

    [Header("Enemy Spawn")]
    public Transform enemySpawnPoint;
    public float enemySpawnRadius = 0f;
    public float enemySpawnSpacingTime = 0.2f;
    public float minEnemySpawnDistance = 2.5f;
    public int enemySpawnPositionAttempts = 20;

    [Header("Heal Pickup")]
    public GameObject healthPickupPrefab;
    public Transform healthPickupSpawnPoint;
    public bool spawnHealAfterEveryWave = true;
    public bool onlyOneHealAlive = true;

    [Header("Next Wave")]
    public float nextWaveDelay = 5f;

    [Header("VR World Text")]
    public TMP_Text waveText;
    public TMP_Text countdownText;
    public TMP_Text missionText;

    [Header("Text Content")]
    public string waveFormat = "Wave {0}/{1}";
    public string countdownFormat = "Next wave in {0}";
    public string missionCompleteText = "Mission Complete";

    [Header("Weapon Selection")]
    public bool showWeaponSelectionBetweenWaves = true;
    public CarWeaponSystem playerWeaponSystem;
    public GameObject initialTopWeaponPrefab;
    public GameObject shotgunWeaponPrefab;
    public GameObject flamethrowerWeaponPrefab;
    public string weaponSelectionTitle = "Choose Weapon";
    public string initialGunLabel = "Initial Gun";
    public string shotgunLabel = "Shotgun";
    public string flamethrowerLabel = "Flamethrower";
    public float selectionStickDeadZone = 0.45f;
    public float selectionTriggerThreshold = 0.35f;
    public float weaponSelectionYOffset = -4f;
    public float weaponSelectionOptionSpacing = 54f;

    [Header("Car Selection")]
    public bool showCarSelectionBeforeWaves = true;
    public GameObject startingPlayerCar;
    public VRCarSelectionOption[] carSelectionOptions;
    public string carSelectionTitle = "Choose Car";
    public float carSelectionYOffset = -4f;
    public float carSelectionOptionSpacing = 64f;

    [Header("Random Wave Layouts")]
    public GameObject[] randomWaveLayouts;
    public GameObject[] randomWaveLayoutPrefabs;
    public Transform layoutSpawnPoint;
    public bool randomizeLayoutEachWave = true;
    public bool avoidRepeatingLastLayout = true;
    public bool useEveryLayoutOncePerCycle = true;

    [Header("Debug")]
    public int currentWaveIndex = -1;
    public int aliveEnemies = 0;
    public bool missionCompleted = false;

    private readonly List<CarHealth> aliveEnemyHealths = new List<CarHealth>();
    private readonly List<Vector3> currentWaveEnemySpawnPositions = new List<Vector3>();
    private GameObject currentHealthPickup;
    private Coroutine waveRoutine;
    private GameObject weaponSelectionPanel;
    private TMP_Text weaponSelectionTitleText;
    private readonly List<Image> weaponSelectionBoxes = new List<Image>();
    private readonly List<TMP_Text> weaponSelectionLabels = new List<TMP_Text>();
    private GameObject carSelectionPanel;
    private TMP_Text carSelectionTitleText;
    private readonly List<Image> carSelectionBoxes = new List<Image>();
    private readonly List<TMP_Text> carSelectionLabels = new List<TMP_Text>();
    private readonly List<GameObject> carSelectionPreviews = new List<GameObject>();
    private readonly List<GameObject> carSelectionPreviewPrefabs = new List<GameObject>();
    private bool selectionStickLocked;
    private bool lastRightTriggerPressed;
    private GameObject currentSpawnedLayout;
    private int currentLayoutIndex = -1;
    private int lastLayoutIndex = -1;
    private readonly List<int> layoutOrder = new List<int>();
    private int layoutOrderCursor;

    private struct WeaponSelectionOption
    {
        public string label;
        public GameObject prefab;

        public WeaponSelectionOption(string label, GameObject prefab)
        {
            this.label = label;
            this.prefab = prefab;
        }
    }

    void Start()
    {
        HideAllTexts();

        waveRoutine = StartCoroutine(GameFlowRoutine());
    }

    IEnumerator GameFlowRoutine()
    {
        yield return StartCoroutine(ShowCarSelectionBeforeWaves());
        yield return StartCoroutine(RunWaves());
    }

    IEnumerator ShowCarSelectionBeforeWaves()
    {
        if (!showCarSelectionBeforeWaves || carSelectionOptions == null || carSelectionOptions.Length == 0)
        {
            EnsurePlayerWeaponSystem();
            yield break;
        }

        GameObject existingPlayer = GetStartingPlayerCar();
        if (startingPlayerCar == null)
            startingPlayerCar = existingPlayer;

        SetPlayerInputEnabled(existingPlayer, false);
        HideCountdownText();
        HideMissionText();

        if (waveText != null)
            waveText.gameObject.SetActive(false);

        int selectedIndex = carSelectionOptions.Length >= 3 ? 1 : 0;
        if (waveText == null)
        {
            ApplySelectedCar(carSelectionOptions[selectedIndex]);
            yield break;
        }

        EnsureCarSelectionPanel(carSelectionOptions.Length);
        RefreshCarSelectionPanel(selectedIndex);

        lastRightTriggerPressed = IsRightTriggerPressed();
        selectionStickLocked = false;

        while (true)
        {
            Vector2 stick = GetRightStick();
            float absX = Mathf.Abs(stick.x);

            if (!selectionStickLocked && absX >= selectionStickDeadZone)
            {
                selectedIndex += stick.x > 0f ? 1 : -1;
                selectedIndex = (selectedIndex + carSelectionOptions.Length) % carSelectionOptions.Length;
                selectionStickLocked = true;
                RefreshCarSelectionPanel(selectedIndex);
            }
            else if (selectionStickLocked && absX < selectionStickDeadZone * 0.5f)
            {
                selectionStickLocked = false;
            }

            bool triggerPressed = IsRightTriggerPressed();
            if (triggerPressed && !lastRightTriggerPressed)
            {
                ApplySelectedCar(carSelectionOptions[selectedIndex]);
                HideCarSelectionPanel();
                yield break;
            }

            lastRightTriggerPressed = triggerPressed;
            yield return null;
        }
    }

    GameObject GetStartingPlayerCar()
    {
        if (startingPlayerCar != null)
            return startingPlayerCar;

        CarPlayerInput[] inputs = FindObjectsByType<CarPlayerInput>(FindObjectsSortMode.None);
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] != null && inputs[i].isPlayerCar)
                return inputs[i].gameObject;
        }

        CarWeaponSystem[] systems = FindObjectsByType<CarWeaponSystem>(FindObjectsSortMode.None);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null && systems[i].isPlayerControlled)
                return systems[i].gameObject;
        }

        return null;
    }

    void SetPlayerInputEnabled(GameObject carObject, bool enabled)
    {
        if (carObject == null)
            return;

        CarPlayerInput input = carObject.GetComponent<CarPlayerInput>();
        if (input != null)
            input.enabled = enabled;
    }

    void ApplySelectedCar(VRCarSelectionOption option)
    {
        if (option == null)
            return;

        GameObject existingPlayer = GetStartingPlayerCar();
        Vector3 spawnPosition = existingPlayer != null ? existingPlayer.transform.position : Vector3.zero;
        Quaternion spawnRotation = existingPlayer != null ? existingPlayer.transform.rotation : Quaternion.identity;

        GameObject selectedCar = option.carPrefab != null
            ? Instantiate(option.carPrefab, spawnPosition, spawnRotation)
            : existingPlayer;

        if (selectedCar == null)
            return;

        selectedCar.name = option.displayName + " PlayerCar";
        if (existingPlayer != null)
            selectedCar.tag = existingPlayer.tag;

        if (existingPlayer != null && existingPlayer != selectedCar)
        {
            CarHealth existingHealth = existingPlayer.GetComponent<CarHealth>();
            if (HealthBarManager.Instance != null)
                HealthBarManager.Instance.UnregisterCar(existingHealth);

            existingPlayer.SetActive(false);
            Destroy(existingPlayer);
        }

        DisableOtherPlayerInputs(selectedCar);
        ApplySelectedCarStats(selectedCar, option);
        RefreshPlayerHealthBindings(selectedCar);

        playerWeaponSystem = selectedCar.GetComponent<CarWeaponSystem>();
        if (playerWeaponSystem != null && initialTopWeaponPrefab == null)
            initialTopWeaponPrefab = playerWeaponSystem.defaultTopWeaponPrefab;
    }

    void RefreshPlayerHealthBindings(GameObject selectedCar)
    {
        if (selectedCar == null)
            return;

        CarHealth selectedHealth = selectedCar.GetComponent<CarHealth>();
        if (selectedHealth == null)
            return;

        if (HealthBarManager.Instance != null)
            HealthBarManager.Instance.RegisterCar(selectedHealth);

        PlayerHealthUI[] healthUis = FindObjectsByType<PlayerHealthUI>(FindObjectsSortMode.None);
        for (int i = 0; i < healthUis.Length; i++)
        {
            if (healthUis[i] != null)
                healthUis[i].SetPlayerHealth(selectedHealth);
        }
    }

    void DisableOtherPlayerInputs(GameObject selectedCar)
    {
        CarPlayerInput[] inputs = FindObjectsByType<CarPlayerInput>(FindObjectsSortMode.None);
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] == null || inputs[i].gameObject == selectedCar)
                continue;

            inputs[i].isPlayerCar = false;
            inputs[i].enabled = false;
        }
    }

    void ApplySelectedCarStats(GameObject selectedCar, VRCarSelectionOption option)
    {
        RaycastCarController car = selectedCar.GetComponent<RaycastCarController>();
        if (car != null)
        {
            car.maxSpeed = option.maxSpeed;
            car.driveForce = option.driveForce;
            car.turnTorque = option.turnTorque;
            car.lateralGrip = option.lateralGrip;
            car.allowSelfRighting = true;
        }

        Rigidbody rb = selectedCar.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CarHealth health = selectedCar.GetComponent<CarHealth>();
        if (health != null)
        {
            health.maxHealth = option.maxHealth;
            health.currentHealth = option.maxHealth;
        }

        CarCollisionDamage collisionDamage = selectedCar.GetComponent<CarCollisionDamage>();
        if (collisionDamage != null)
            collisionDamage.isEnemy = false;

        CarPlayerInput input = selectedCar.GetComponent<CarPlayerInput>();
        if (input != null)
        {
            input.isPlayerCar = true;
            input.allowSelfRightingInput = true;
            input.enabled = true;
        }

        CarWeaponSystem weaponSystem = selectedCar.GetComponent<CarWeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.isPlayerControlled = true;
            if (weaponSystem.topGunWeaponPrefab != null)
                weaponSystem.defaultTopWeaponPrefab = weaponSystem.topGunWeaponPrefab;

            weaponSystem.EquipDefaultLoadout();
        }
    }

    IEnumerator RunWaves()
    {
        if (waves == null || waves.Length == 0)
        {
            ShowMissionText("No waves set");
            yield break;
        }

        for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
        {
            ClearOldHealthPickup();
            ApplyRandomLayoutForWave();

            ShowWaveText(currentWaveIndex + 1, waves.Length);
            HideCountdownText();
            HideMissionText();

            yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));

            while (aliveEnemies > 0)
            {
                yield return null;
            }

            if (currentWaveIndex >= waves.Length - 1)
            {
                CompleteMission();
                yield break;
            }

            if (spawnHealAfterEveryWave)
            {
                SpawnHealthPickup();
            }

            yield return StartCoroutine(ShowWeaponSelectionForCompletedWave(currentWaveIndex));
            yield return StartCoroutine(NextWaveCountdown());
        }

        CompleteMission();
    }

    void ApplyRandomLayoutForWave()
    {
        if (!randomizeLayoutEachWave)
            return;

        if (randomWaveLayoutPrefabs != null && randomWaveLayoutPrefabs.Length > 0)
        {
            SpawnRandomLayoutPrefab();
            return;
        }

        if (randomWaveLayouts == null || randomWaveLayouts.Length == 0)
            return;

        int selectedIndex = SelectLayoutIndex(randomWaveLayouts.Length);

        GameObject selectedLayout = randomWaveLayouts[selectedIndex];
        if (selectedLayout == null)
            return;

        for (int i = 0; i < randomWaveLayouts.Length; i++)
        {
            GameObject layout = randomWaveLayouts[i];
            if (layout != null)
                layout.SetActive(layout == selectedLayout);
        }

        currentLayoutIndex = selectedIndex;
        lastLayoutIndex = selectedIndex;
    }

    void SpawnRandomLayoutPrefab()
    {
        HideStaticArenaInstance();

        int selectedIndex = SelectLayoutIndex(randomWaveLayoutPrefabs.Length);

        GameObject selectedPrefab = randomWaveLayoutPrefabs[selectedIndex];
        if (selectedPrefab == null)
            return;

        if (currentSpawnedLayout != null)
            Destroy(currentSpawnedLayout);

        Vector3 pos = layoutSpawnPoint != null ? layoutSpawnPoint.position : Vector3.zero;
        Quaternion rot = layoutSpawnPoint != null ? layoutSpawnPoint.rotation : Quaternion.identity;

        currentSpawnedLayout = Instantiate(selectedPrefab, pos, rot);
        currentSpawnedLayout.name = selectedPrefab.name + "_Runtime";

        if (randomWaveLayouts != null)
        {
            for (int i = 0; i < randomWaveLayouts.Length; i++)
            {
                if (randomWaveLayouts[i] != null)
                    randomWaveLayouts[i].SetActive(false);
            }
        }

        currentLayoutIndex = selectedIndex;
        lastLayoutIndex = selectedIndex;
    }

    int SelectLayoutIndex(int layoutCount)
    {
        if (layoutCount <= 0)
            return -1;

        if (useEveryLayoutOncePerCycle)
            return SelectLayoutIndexFromShuffledCycle(layoutCount);

        int selectedIndex = Random.Range(0, layoutCount);

        if (avoidRepeatingLastLayout && layoutCount > 1)
        {
            int guard = 0;
            while (selectedIndex == lastLayoutIndex && guard < 20)
            {
                selectedIndex = Random.Range(0, layoutCount);
                guard++;
            }
        }

        return selectedIndex;
    }

    int SelectLayoutIndexFromShuffledCycle(int layoutCount)
    {
        if (layoutOrder.Count != layoutCount || layoutOrderCursor >= layoutOrder.Count)
            BuildShuffledLayoutOrder(layoutCount);

        return layoutOrder[layoutOrderCursor++];
    }

    void BuildShuffledLayoutOrder(int layoutCount)
    {
        layoutOrder.Clear();
        layoutOrderCursor = 0;

        for (int i = 0; i < layoutCount; i++)
            layoutOrder.Add(i);

        for (int i = layoutOrder.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = layoutOrder[i];
            layoutOrder[i] = layoutOrder[swapIndex];
            layoutOrder[swapIndex] = temp;
        }

        if (avoidRepeatingLastLayout && layoutOrder.Count > 1 && layoutOrder[0] == lastLayoutIndex)
        {
            int swapIndex = Random.Range(1, layoutOrder.Count);
            int temp = layoutOrder[0];
            layoutOrder[0] = layoutOrder[swapIndex];
            layoutOrder[swapIndex] = temp;
        }
    }

    void HideStaticArenaInstance()
    {
        GameObject staticArena = GameObject.Find("Arena");
        if (staticArena != null && staticArena != currentSpawnedLayout)
            staticArena.SetActive(false);
    }

    IEnumerator SpawnWave(VRWaveInfo wave)
    {
        aliveEnemyHealths.Clear();
        currentWaveEnemySpawnPositions.Clear();
        aliveEnemies = 0;

        if (wave == null)
            yield break;

        if (enemySpawnPoint == null)
        {
            Debug.LogWarning("VRWavePickupManager: Enemy spawn point is missing.");
            yield break;
        }

        if (wave.enemies == null || wave.enemies.Length == 0)
        {
            Debug.LogWarning("VRWavePickupManager: This wave has no enemy types.");
            yield break;
        }

        for (int typeIndex = 0; typeIndex < wave.enemies.Length; typeIndex++)
        {
            VREnemySpawnInfo enemyInfo = wave.enemies[typeIndex];

            if (enemyInfo == null)
                continue;

            if (enemyInfo.enemyPrefab == null)
            {
                Debug.LogWarning("VRWavePickupManager: Enemy prefab is missing in wave.");
                continue;
            }

            int count = Mathf.Max(0, enemyInfo.enemyCount);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetEnemySpawnPosition();
                Quaternion rot = enemySpawnPoint.rotation;

                GameObject enemy = Instantiate(enemyInfo.enemyPrefab, pos, rot);

                CarHealth health = enemy.GetComponentInChildren<CarHealth>();

                if (health != null)
                {
                    aliveEnemies++;
                    aliveEnemyHealths.Add(health);
                    health.OnDied += OnEnemyDied;

                    if (HealthBarManager.Instance != null)
                        HealthBarManager.Instance.RegisterCar(health);
                }
                else
                {
                    Debug.LogWarning("VRWavePickupManager: Spawned enemy has no CarHealth.");
                }

                if (enemySpawnSpacingTime > 0f)
                    yield return new WaitForSeconds(enemySpawnSpacingTime);
            }
        }
    }

    Vector3 GetEnemySpawnPosition()
    {
        if (enemySpawnPoint == null)
            return transform.position;

        if (enemySpawnRadius <= 0f)
            return RegisterEnemySpawnPosition(enemySpawnPoint.position);

        int attempts = Mathf.Max(1, enemySpawnPositionAttempts);
        Vector3 fallbackPosition = enemySpawnPoint.position;

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = GetRandomEnemySpawnPosition();
            fallbackPosition = candidate;

            if (IsFarEnoughFromExistingEnemySpawns(candidate))
                return RegisterEnemySpawnPosition(candidate);
        }

        return RegisterEnemySpawnPosition(fallbackPosition);
    }

    Vector3 GetRandomEnemySpawnPosition()
    {
        Vector2 offset = Random.insideUnitCircle * enemySpawnRadius;
        return enemySpawnPoint.position + new Vector3(offset.x, 0f, offset.y);
    }

    bool IsFarEnoughFromExistingEnemySpawns(Vector3 candidate)
    {
        if (minEnemySpawnDistance <= 0f)
            return true;

        float minSqrDistance = minEnemySpawnDistance * minEnemySpawnDistance;
        Vector2 candidateXZ = new Vector2(candidate.x, candidate.z);

        for (int i = 0; i < currentWaveEnemySpawnPositions.Count; i++)
        {
            Vector3 existing = currentWaveEnemySpawnPositions[i];
            Vector2 existingXZ = new Vector2(existing.x, existing.z);

            if ((candidateXZ - existingXZ).sqrMagnitude < minSqrDistance)
                return false;
        }

        return true;
    }

    Vector3 RegisterEnemySpawnPosition(Vector3 position)
    {
        currentWaveEnemySpawnPositions.Add(position);
        return position;
    }

    void OnEnemyDied(CarHealth deadHealth)
    {
        if (deadHealth != null)
            deadHealth.OnDied -= OnEnemyDied;

        if (aliveEnemyHealths.Contains(deadHealth))
            aliveEnemyHealths.Remove(deadHealth);

        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    void SpawnHealthPickup()
    {
        if (healthPickupPrefab == null)
            return;

        if (healthPickupSpawnPoint == null)
            return;

        if (onlyOneHealAlive)
            ClearOldHealthPickup();

        currentHealthPickup = Instantiate(
            healthPickupPrefab,
            healthPickupSpawnPoint.position,
            healthPickupSpawnPoint.rotation
        );
    }

    void ClearOldHealthPickup()
    {
        if (currentHealthPickup == null)
            return;

        Destroy(currentHealthPickup);
        currentHealthPickup = null;
    }

    IEnumerator NextWaveCountdown()
    {
        float timer = nextWaveDelay;

        ShowCountdownText(Mathf.CeilToInt(timer));

        while (timer > 0f)
        {
            ShowCountdownText(Mathf.CeilToInt(timer));

            timer -= Time.deltaTime;
            yield return null;
        }

        HideCountdownText();
    }

    IEnumerator ShowWeaponSelectionForCompletedWave(int completedWaveIndex)
    {
        List<WeaponSelectionOption> options = BuildWeaponOptions(completedWaveIndex);
        if (options.Count <= 0)
            yield break;

        if (waveText != null)
            waveText.gameObject.SetActive(false);

        EnsureWeaponSelectionPanel(options.Count);
        RefreshWeaponSelectionPanel(options, 0);
        HideCountdownText();

        int selectedIndex = 0;
        lastRightTriggerPressed = IsRightTriggerPressed();
        selectionStickLocked = false;

        while (true)
        {
            Vector2 stick = GetRightStick();
            float absX = Mathf.Abs(stick.x);

            if (!selectionStickLocked && absX >= selectionStickDeadZone)
            {
                selectedIndex += stick.x > 0f ? 1 : -1;
                selectedIndex = (selectedIndex + options.Count) % options.Count;
                selectionStickLocked = true;
                RefreshWeaponSelectionPanel(options, selectedIndex);
            }
            else if (selectionStickLocked && absX < selectionStickDeadZone * 0.5f)
            {
                selectionStickLocked = false;
            }

            bool triggerPressed = IsRightTriggerPressed();
            if (triggerPressed && !lastRightTriggerPressed)
            {
                EquipSelectedWeapon(options[selectedIndex].prefab);
                HideWeaponSelectionPanel();
                yield break;
            }

            lastRightTriggerPressed = triggerPressed;
            yield return null;
        }
    }

    List<WeaponSelectionOption> BuildWeaponOptions(int completedWaveIndex)
    {
        List<WeaponSelectionOption> options = new List<WeaponSelectionOption>();

        if (!showWeaponSelectionBetweenWaves)
            return options;

        EnsurePlayerWeaponSystem();

        GameObject initialPrefab = initialTopWeaponPrefab;
        if (initialPrefab == null && playerWeaponSystem != null)
            initialPrefab = playerWeaponSystem.defaultTopWeaponPrefab != null
                ? playerWeaponSystem.defaultTopWeaponPrefab
                : playerWeaponSystem.topGunWeaponPrefab;

        if (initialPrefab != null)
            options.Add(new WeaponSelectionOption(initialGunLabel, initialPrefab));

        if (completedWaveIndex >= 0 && shotgunWeaponPrefab != null)
            options.Add(new WeaponSelectionOption(shotgunLabel, shotgunWeaponPrefab));

        if (completedWaveIndex >= 1 && flamethrowerWeaponPrefab != null)
            options.Add(new WeaponSelectionOption(flamethrowerLabel, flamethrowerWeaponPrefab));

        return options;
    }

    void EquipSelectedWeapon(GameObject weaponPrefab)
    {
        EnsurePlayerWeaponSystem();

        if (playerWeaponSystem == null || weaponPrefab == null)
            return;

        playerWeaponSystem.EquipTopWeapon(weaponPrefab);
    }

    void EnsurePlayerWeaponSystem()
    {
        if (playerWeaponSystem != null)
            return;

        CarWeaponSystem[] systems = FindObjectsByType<CarWeaponSystem>(FindObjectsSortMode.None);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null && systems[i].isPlayerControlled)
            {
                playerWeaponSystem = systems[i];
                return;
            }
        }

        if (systems.Length > 0)
            playerWeaponSystem = systems[0];
    }

    void EnsureCarSelectionPanel(int optionCount)
    {
        if (waveText == null)
            return;

        if (carSelectionPanel == null)
            CreateCarSelectionPanel();

        while (carSelectionBoxes.Count < optionCount)
            CreateCarSelectionBox(carSelectionBoxes.Count);

        carSelectionPanel.SetActive(true);
    }

    void CreateCarSelectionPanel()
    {
        RectTransform waveRect = waveText.transform as RectTransform;
        Transform parent = waveText.transform.parent;

        carSelectionPanel = new GameObject("CarSelectionPanel", typeof(RectTransform));
        carSelectionPanel.transform.SetParent(parent, false);

        RectTransform panelRect = carSelectionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(220f, 82f);

        if (waveRect != null)
            panelRect.anchoredPosition = new Vector2(0f, waveRect.anchoredPosition.y + carSelectionYOffset);
        else
            panelRect.anchoredPosition = new Vector2(0f, carSelectionYOffset);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(carSelectionPanel.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 22f);
        titleRect.sizeDelta = new Vector2(190f, 10f);

        carSelectionTitleText = titleObj.GetComponent<TMP_Text>();
        carSelectionTitleText.alignment = TextAlignmentOptions.Center;
        carSelectionTitleText.fontSize = 4.5f;
        carSelectionTitleText.color = Color.white;
        carSelectionTitleText.text = carSelectionTitle;
    }

    void CreateCarSelectionBox(int index)
    {
        GameObject boxObj = new GameObject("CarOption" + index, typeof(RectTransform), typeof(Image));
        boxObj.transform.SetParent(carSelectionPanel.transform, false);

        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(58f, 42f);

        Image image = boxObj.GetComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.12f, 0.85f);
        carSelectionBoxes.Add(image);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(boxObj.transform, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(2f, -2f);
        labelRect.offsetMax = new Vector2(-2f, -1f);

        TMP_Text label = labelObj.GetComponent<TMP_Text>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 3.4f;
        label.richText = true;
        label.color = Color.white;
        carSelectionLabels.Add(label);

        carSelectionPreviews.Add(null);
        carSelectionPreviewPrefabs.Add(null);
    }

    void RefreshCarSelectionPanel(int selectedIndex)
    {
        if (carSelectionTitleText != null)
            carSelectionTitleText.text = carSelectionTitle;

        int optionCount = carSelectionOptions != null ? carSelectionOptions.Length : 0;
        float spacing = carSelectionOptionSpacing;
        float startX = -spacing * (optionCount - 1) * 0.5f;

        for (int i = 0; i < carSelectionBoxes.Count; i++)
        {
            bool active = i < optionCount;
            carSelectionBoxes[i].gameObject.SetActive(active);
            if (!active)
            {
                ClearSelectionModelPreview(carSelectionPreviews, carSelectionPreviewPrefabs, i);
                continue;
            }

            RectTransform rect = carSelectionBoxes[i].transform as RectTransform;
            if (rect != null)
                rect.anchoredPosition = new Vector2(startX + spacing * i, -5f);

            bool selected = i == selectedIndex;
            carSelectionBoxes[i].color = selected
                ? new Color(1f, 0.72f, 0.18f, 0.95f)
                : new Color(0.08f, 0.1f, 0.12f, 0.85f);

            VRCarSelectionOption option = carSelectionOptions[i];
            carSelectionLabels[i].text = option.displayName + "\n<size=80%>" + option.trait + "</size>";
            carSelectionLabels[i].color = selected ? Color.black : Color.white;

            SetSelectionModelPreview(carSelectionBoxes[i].transform, carSelectionPreviews, carSelectionPreviewPrefabs, i, option.carPrefab, true);
        }
    }

    void HideCarSelectionPanel()
    {
        if (carSelectionPanel != null)
            carSelectionPanel.SetActive(false);

        ClearSelectionModelPreviews(carSelectionPreviews, carSelectionPreviewPrefabs);
    }

    void EnsureWeaponSelectionPanel(int optionCount)
    {
        if (waveText == null)
            return;

        if (weaponSelectionPanel == null)
            CreateWeaponSelectionPanel();

        while (weaponSelectionBoxes.Count < optionCount)
            CreateWeaponSelectionBox(weaponSelectionBoxes.Count);

        weaponSelectionPanel.SetActive(true);
    }

    void CreateWeaponSelectionPanel()
    {
        RectTransform waveRect = waveText.transform as RectTransform;
        Transform parent = waveText.transform.parent;

        weaponSelectionPanel = new GameObject("WeaponSelectionPanel", typeof(RectTransform));
        weaponSelectionPanel.transform.SetParent(parent, false);

        RectTransform panelRect = weaponSelectionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(170f, 55f);

        if (waveRect != null)
            panelRect.anchoredPosition = new Vector2(0f, waveRect.anchoredPosition.y + weaponSelectionYOffset);
        else
            panelRect.anchoredPosition = new Vector2(0f, weaponSelectionYOffset);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(weaponSelectionPanel.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 18f);
        titleRect.sizeDelta = new Vector2(150f, 10f);

        weaponSelectionTitleText = titleObj.GetComponent<TMP_Text>();
        weaponSelectionTitleText.alignment = TextAlignmentOptions.Center;
        weaponSelectionTitleText.fontSize = 4.5f;
        weaponSelectionTitleText.color = Color.white;
        weaponSelectionTitleText.text = weaponSelectionTitle;
    }

    void CreateWeaponSelectionBox(int index)
    {
        GameObject boxObj = new GameObject("WeaponOption" + index, typeof(RectTransform), typeof(Image));
        boxObj.transform.SetParent(weaponSelectionPanel.transform, false);

        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(48f, 18f);

        Image image = boxObj.GetComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.12f, 0.85f);
        weaponSelectionBoxes.Add(image);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(boxObj.transform, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text label = labelObj.GetComponent<TMP_Text>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 3.6f;
        label.color = Color.white;
        weaponSelectionLabels.Add(label);
    }

    void RefreshWeaponSelectionPanel(List<WeaponSelectionOption> options, int selectedIndex)
    {
        if (weaponSelectionTitleText != null)
            weaponSelectionTitleText.text = weaponSelectionTitle;

        float spacing = weaponSelectionOptionSpacing;
        float startX = -spacing * (options.Count - 1) * 0.5f;

        for (int i = 0; i < weaponSelectionBoxes.Count; i++)
        {
            bool active = i < options.Count;
            weaponSelectionBoxes[i].gameObject.SetActive(active);
            if (!active)
                continue;

            RectTransform rect = weaponSelectionBoxes[i].transform as RectTransform;
            if (rect != null)
                rect.anchoredPosition = new Vector2(startX + spacing * i, -4f);

            bool selected = i == selectedIndex;
            weaponSelectionBoxes[i].color = selected
                ? new Color(1f, 0.72f, 0.18f, 0.95f)
                : new Color(0.08f, 0.1f, 0.12f, 0.85f);

            weaponSelectionLabels[i].text = options[i].label;
            weaponSelectionLabels[i].color = selected ? Color.black : Color.white;
        }
    }

    void HideWeaponSelectionPanel()
    {
        if (weaponSelectionPanel != null)
            weaponSelectionPanel.SetActive(false);
    }

    void SetSelectionModelPreview(
        Transform optionBox,
        List<GameObject> previewObjects,
        List<GameObject> previewPrefabs,
        int index,
        GameObject prefab,
        bool isCar)
    {
        if (optionBox == null)
            return;

        EnsurePreviewListSize(previewObjects, previewPrefabs, index + 1);

        if (prefab == null)
        {
            ClearSelectionModelPreview(previewObjects, previewPrefabs, index);
            return;
        }

        if (previewObjects[index] != null && previewPrefabs[index] == prefab)
        {
            previewObjects[index].SetActive(true);
            return;
        }

        ClearSelectionModelPreview(previewObjects, previewPrefabs, index);

        GameObject preview = Instantiate(prefab, optionBox);
        preview.name = prefab.name + "_SelectionModelPreview";
        previewPrefabs[index] = prefab;
        previewObjects[index] = preview;

        SetLayerRecursively(preview, optionBox.gameObject.layer);
        PreparePreviewInstance(preview);

        preview.transform.localPosition = Vector3.zero;
        preview.transform.localRotation = isCar
            ? Quaternion.Euler(0f, -90f, 0f)
            : Quaternion.Euler(12f, -45f, 0f);
        preview.transform.localScale = Vector3.one;

        if (!TryGetRendererBounds(preview, out Bounds bounds))
        {
            ClearSelectionModelPreview(previewObjects, previewPrefabs, index);
            return;
        }

        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension <= 0.001f)
            maxDimension = 1f;

        float parentScale = GetAverageWorldScale(optionBox);
        float desiredLocalSize = isCar ? 28f : 19f;
        float desiredWorldSize = desiredLocalSize * parentScale;
        float fittedScale = desiredWorldSize / maxDimension;
        preview.transform.localScale = Vector3.one * fittedScale;

        TryGetRendererBounds(preview, out bounds);

        Vector3 desiredLocalPosition = new Vector3(0f, isCar ? -10f : -9f, -10f);
        Vector3 desiredWorldCenter = optionBox.TransformPoint(desiredLocalPosition);
        preview.transform.position += desiredWorldCenter - bounds.center;
    }

    void EnsurePreviewListSize(List<GameObject> previewObjects, List<GameObject> previewPrefabs, int count)
    {
        while (previewObjects.Count < count)
            previewObjects.Add(null);

        while (previewPrefabs.Count < count)
            previewPrefabs.Add(null);
    }

    void ClearSelectionModelPreview(List<GameObject> previewObjects, List<GameObject> previewPrefabs, int index)
    {
        if (index < 0 || index >= previewObjects.Count)
            return;

        if (previewObjects[index] != null)
            Destroy(previewObjects[index]);

        previewObjects[index] = null;

        if (index < previewPrefabs.Count)
            previewPrefabs[index] = null;
    }

    void ClearSelectionModelPreviews(List<GameObject> previewObjects, List<GameObject> previewPrefabs)
    {
        for (int i = 0; i < previewObjects.Count; i++)
        {
            if (previewObjects[i] != null)
                Destroy(previewObjects[i]);

            previewObjects[i] = null;
        }

        for (int i = 0; i < previewPrefabs.Count; i++)
            previewPrefabs[i] = null;
    }

    float GetAverageWorldScale(Transform target)
    {
        Vector3 scale = target.lossyScale;
        float average = (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f;
        return Mathf.Max(average, 0.0001f);
    }

    void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }

    void PreparePreviewInstance(GameObject previewInstance)
    {
        MonoBehaviour[] behaviours = previewInstance.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = false;
        }

        Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Rigidbody[] rigidbodies = previewInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] == null)
                continue;

            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
        }

        AudioSource[] audioSources = previewInstance.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
                audioSources[i].enabled = false;
        }

        ParticleSystem[] particleSystems = previewInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(root.transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    Vector2 GetRightStick()
    {
        UnityEngine.XR.InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightDevice.isValid &&
            rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 stick))
        {
            return stick;
        }

        if (Gamepad.current != null)
            return Gamepad.current.rightStick.ReadValue();

        float x = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                x += 1f;
        }

        return new Vector2(x, 0f);
    }

    bool IsRightTriggerPressed()
    {
        UnityEngine.XR.InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightDevice.isValid &&
            rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float trigger))
        {
            return trigger >= selectionTriggerThreshold;
        }

        if (Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() >= selectionTriggerThreshold)
            return true;

        return Keyboard.current != null && Keyboard.current.enterKey.isPressed;
    }

    void CompleteMission()
    {
        missionCompleted = true;
        aliveEnemies = 0;

        HideCountdownText();
        HideCarSelectionPanel();
        HideWeaponSelectionPanel();
        ShowMissionText(missionCompleteText);
    }

    void ShowWaveText(int currentWave, int totalWaves)
    {
        if (waveText == null)
            return;

        waveText.gameObject.SetActive(true);
        waveText.text = string.Format(waveFormat, currentWave, totalWaves);
    }

    void ShowCountdownText(int seconds)
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(true);
        countdownText.text = string.Format(countdownFormat, seconds);
    }

    void HideCountdownText()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    void ShowMissionText(string text)
    {
        if (missionText == null)
            return;

        missionText.gameObject.SetActive(true);
        missionText.text = text;
    }

    void HideMissionText()
    {
        if (missionText != null)
            missionText.gameObject.SetActive(false);
    }

    void HideAllTexts()
    {
        if (waveText != null)
            waveText.gameObject.SetActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (missionText != null)
            missionText.gameObject.SetActive(false);

        HideWeaponSelectionPanel();
        HideCarSelectionPanel();
    }

    void OnDrawGizmosSelected()
    {
        if (enemySpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemySpawnPoint.position, enemySpawnRadius);
        }

        if (healthPickupSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(healthPickupSpawnPoint.position, 0.5f);
        }
    }
}
