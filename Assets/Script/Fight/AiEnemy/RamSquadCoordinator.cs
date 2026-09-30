using UnityEngine;
using System.Collections.Generic;

public class RamSquadCoordinator : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;

    [Header("Units")]
    public List<CarRamAI> ramUnits = new List<CarRamAI>();
    public bool autoFindRamUnits = true;
    public float refreshUnitsInterval = 1f;

    [Header("Formation")]
    public float updateCommandInterval = 0.2f;
    public float frontOffset = 4f;
    public float sideSpacing = 4f;
    public int maxFrontSlots = 5;

    [Header("Charge Control")]
    public int maxChargingUnits = 1;
    public float chargeCandidateDistance = 14f;

    private float unitRefreshTimer;
    private float commandTimer;

    void Start()
    {
        RefreshUnits();
        AutoAssignTargetIfNeeded();
    }

    void Update()
    {
        AutoAssignTargetIfNeeded();

        unitRefreshTimer -= Time.deltaTime;
        if (unitRefreshTimer <= 0f)
        {
            unitRefreshTimer = refreshUnitsInterval;
            if (autoFindRamUnits)
                RefreshUnits();
        }

        if (target == null || ramUnits.Count == 0)
            return;

        commandTimer -= Time.deltaTime;
        if (commandTimer <= 0f)
        {
            commandTimer = updateCommandInterval;
            UpdateFormation();
            UpdateChargePermissions();
        }
    }

    void AutoAssignTargetIfNeeded()
    {
        if (target != null) return;

        CarPlayerInput[] players = FindObjectsByType<CarPlayerInput>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            if (!players[i].isActiveAndEnabled) continue;

            target = players[i].transform;
            return;
        }
    }

    void RefreshUnits()
    {
        ramUnits.Clear();

        CarRamAI[] found = FindObjectsByType<CarRamAI>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null) continue;
            if (!found[i].isActiveAndEnabled) continue;

            ramUnits.Add(found[i]);
        }
    }

    void UpdateFormation()
    {
        List<CarRamAI> validUnits = new List<CarRamAI>();

        for (int i = 0; i < ramUnits.Count; i++)
        {
            if (ramUnits[i] == null) continue;
            if (!ramUnits[i].isActiveAndEnabled) continue;
            validUnits.Add(ramUnits[i]);
        }

        if (validUnits.Count == 0) return;

        Vector3 targetForward = target.forward;
        targetForward.y = 0f;
        if (targetForward.sqrMagnitude < 0.001f)
            targetForward = Vector3.forward;
        else
            targetForward.Normalize();

        Vector3 targetRight = Vector3.Cross(Vector3.up, targetForward).normalized;

        // 按与玩家距离排序：近的优先拿中间槽位
        validUnits.Sort((a, b) =>
        {
            float da = FlatDistance(a.transform.position, target.position);
            float db = FlatDistance(b.transform.position, target.position);
            return da.CompareTo(db);
        });

        for (int i = 0; i < validUnits.Count; i++)
        {
            int slotIndex = GetSlotIndex(i);
            float side = slotIndex * sideSpacing;

            float forwardMul = 1f - Mathf.Min(Mathf.Abs(slotIndex) * 0.15f, 0.5f);
            float forward = frontOffset * forwardMul;

            Vector3 point = target.position + targetRight * side + targetForward * forward;
            validUnits[i].SetCommandPoint(point);
        }
    }

    int GetSlotIndex(int orderedIndex)
    {
        // 生成顺序：0, -1, 1, -2, 2, -3, 3...
        if (orderedIndex == 0) return 0;

        int step = (orderedIndex + 1) / 2;
        bool left = orderedIndex % 2 == 1;
        return left ? -step : step;
    }

    void UpdateChargePermissions()
    {
        List<CarRamAI> candidates = new List<CarRamAI>();

        for (int i = 0; i < ramUnits.Count; i++)
        {
            CarRamAI ai = ramUnits[i];
            if (ai == null || !ai.isActiveAndEnabled)
                continue;

            ai.SetChargePermission(false);

            float dist = FlatDistance(ai.transform.position, target.position);
            if (dist <= chargeCandidateDistance)
                candidates.Add(ai);
        }

        candidates.Sort((a, b) =>
        {
            float da = FlatDistance(a.transform.position, target.position);
            float db = FlatDistance(b.transform.position, target.position);
            return da.CompareTo(db);
        });

        int allowCount = Mathf.Min(maxChargingUnits, candidates.Count);
        for (int i = 0; i < allowCount; i++)
        {
            candidates[i].SetChargePermission(true);
        }
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}