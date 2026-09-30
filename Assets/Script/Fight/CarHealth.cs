using UnityEngine;
using System;
using System.Collections;

public class CarHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Damage Reduction")]
    public float baseDamageReductionPercent = 0f;
    public float bonusDamageReductionPercent = 0f;

    [Header("Death Visual")]
    public bool darkenOnDeath = true;
    [Range(0f, 1f)] public float deathDarknessMultiplier = 0.1f;
    public bool includeInactiveRenderers = true;

    [Header("Death Sink")]
    public bool sinkAndDestroyAfterDeath = true;
    public float stayBeforeSink = 8f;
    public float sinkDuration = 1.5f;
    public float sinkDistance = 1.6f;
    public AnimationCurve sinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool disableCollidersBeforeSink = true;

    public bool IsDead => isDead;

    public event Action<CarHealth> OnDied;

    private bool isDead = false;

    private Rigidbody cachedRb;
    private RaycastCarController cachedCarController;
    private MineLayerWeapon cachedMineLayer;
    private CarRamAI cachedRamAI;
    private CarGunAI cachedGunAI;
    private TurretGunWeapon cachedTurret;
    private CarCollisionDamage cachedCollisionDamage;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        currentHealth = maxHealth;

        cachedRb = GetComponent<Rigidbody>();
        cachedCarController = GetComponent<RaycastCarController>();
        cachedMineLayer = GetComponentInChildren<MineLayerWeapon>(true);
        cachedRamAI = GetComponent<CarRamAI>();
        cachedGunAI = GetComponent<CarGunAI>();
        cachedTurret = GetComponentInChildren<TurretGunWeapon>(true);
        cachedCollisionDamage = GetComponent<CarCollisionDamage>();

        cachedRenderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);
        propertyBlock = new MaterialPropertyBlock();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        float totalReduction = baseDamageReductionPercent + bonusDamageReductionPercent;
        totalReduction = Mathf.Clamp(totalReduction, 0f, 80f);

        float reducedDamage = damage * (1f - totalReduction / 100f);

        int finalDamage = Mathf.RoundToInt(reducedDamage);
        finalDamage = Mathf.Max(finalDamage, 1);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (darkenOnDeath)
        {
            DarkenRenderersOnDeath();
        }

        if (cachedRb != null)
        {
            cachedRb.linearVelocity *= 0.35f;
            cachedRb.angularVelocity *= 0.35f;
        }

        if (cachedCarController != null)
        {
            cachedCarController.SetDeadState();
            cachedCarController.enabled = false;
        }

        if (cachedMineLayer != null)
        {
            cachedMineLayer.enabled = false;
        }

        if (cachedRamAI != null)
        {
            cachedRamAI.enabled = false;
        }

        if (cachedGunAI != null)
        {
            cachedGunAI.enabled = false;
        }

        if (cachedTurret != null)
        {
            cachedTurret.aiWantsToFire = false;
            cachedTurret.aiTarget = null;
            cachedTurret.enabled = false;
        }

        if (cachedCollisionDamage != null)
        {
            cachedCollisionDamage.enabled = false;
        }

        if (sinkAndDestroyAfterDeath)
        {
            StartCoroutine(SinkAndDisableRoutine());
        }

        OnDied?.Invoke(this);
    }

    void DarkenRenderersOnDeath()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;

        float darkMul = Mathf.Clamp01(deathDarknessMultiplier);

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer r = cachedRenderers[i];
            if (r == null) continue;

            Material sharedMat = r.sharedMaterial;
            if (sharedMat == null) continue;

            r.GetPropertyBlock(propertyBlock);

            if (sharedMat.HasProperty("_BaseColor"))
            {
                Color baseColor = sharedMat.GetColor("_BaseColor");
                baseColor.r *= darkMul;
                baseColor.g *= darkMul;
                baseColor.b *= darkMul;
                baseColor.a = 1f;

                propertyBlock.SetColor("_BaseColor", baseColor);
                r.SetPropertyBlock(propertyBlock);
            }
            else if (sharedMat.HasProperty("_Color"))
            {
                Color color = sharedMat.GetColor("_Color");
                color.r *= darkMul;
                color.g *= darkMul;
                color.b *= darkMul;
                color.a = 1f;

                propertyBlock.SetColor("_Color", color);
                r.SetPropertyBlock(propertyBlock);
            }
        }
    }

    IEnumerator SinkAndDisableRoutine()
    {
        yield return new WaitForSeconds(stayBeforeSink);

        if (disableCollidersBeforeSink)
        {
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = false;
            }
        }

        if (cachedRb != null)
        {
            cachedRb.linearVelocity = Vector3.zero;
            cachedRb.angularVelocity = Vector3.zero;
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * sinkDistance;

        float t = 0f;
        while (t < sinkDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / sinkDuration);
            float eased = sinkCurve.Evaluate(normalized);

            transform.position = Vector3.Lerp(startPos, endPos, eased);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}