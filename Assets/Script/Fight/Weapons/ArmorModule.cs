using UnityEngine;

public class ArmorModule : MonoBehaviour
{
    [Header("Refs")]
    public CarHealth carHealth;

    [Header("Armor")]
    public float damageReductionPercent = 20f;

    void Awake()
    {
        if (carHealth == null)
            carHealth = GetComponentInParent<CarHealth>();
    }

    void OnEnable()
    {
        if (carHealth != null)
        {
            carHealth.bonusDamageReductionPercent += damageReductionPercent;
        }
    }

    void OnDisable()
    {
        if (carHealth != null)
        {
            carHealth.bonusDamageReductionPercent -= damageReductionPercent;
            carHealth.bonusDamageReductionPercent = Mathf.Max(0f, carHealth.bonusDamageReductionPercent);
        }
    }
}