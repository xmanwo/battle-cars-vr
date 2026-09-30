using UnityEngine;

[DisallowMultipleComponent]
public class ObstacleKnockbackReceiver : MonoBehaviour
{
    [Header("Target")]
    public Rigidbody targetRigidbody;
    public Transform directionReference;

    [Header("Automatic Obstacle Detection")]
    public bool reactToCollisions = true;
    public bool reactToTriggers = true;
    public string obstacleTag = "Obs";
    public bool requireObstacleTag = true;
    public bool filterByObstacleLayer = false;
    public LayerMask obstacleLayers = ~0;
    public bool ignoreTrapAdaptersDuringAutomaticDetection = false;

    [Header("Knockback")]
    public float defaultHitForce = 30f;
    public bool flattenDirection = true;
    public float upwardBoost = 0f;
    public bool clearVelocityBeforeKnockback = true;
    public ForceMode forceMode = ForceMode.VelocityChange;

    private void Reset()
    {
        targetRigidbody = GetComponentInParent<Rigidbody>();
        directionReference = transform;
    }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponentInParent<Rigidbody>();
        }

        if (directionReference == null)
        {
            directionReference = transform;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!reactToCollisions)
        {
            return;
        }

        Vector3 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : collision.transform.position;

        TryApplyFromCollisionSource(collision.transform, hitPoint, defaultHitForce);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!reactToTriggers)
        {
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(GetReferencePosition());
        TryApplyFromCollisionSource(other.transform, hitPoint, defaultHitForce);
    }

    public bool TryApplyFromCollisionSource(Transform collisionSource, Vector3 hitPoint, float hitForce)
    {
        if (targetRigidbody == null)
        {
            return false;
        }

        if (!IsAcceptedObstacle(collisionSource))
        {
            return false;
        }

        ApplyKnockbackFromPoint(hitPoint, hitForce, collisionSource);
        return true;
    }

    public void ApplyExternalKnockback(Vector3 hitPoint, float hitForce, Transform sourceTransform = null)
    {
        if (targetRigidbody == null)
        {
            return;
        }

        ApplyKnockbackFromPoint(hitPoint, hitForce, sourceTransform);
    }

    public void ApplyExternalKnockbackFromTransform(Transform sourceTransform, float hitForce)
    {
        if (sourceTransform == null)
        {
            return;
        }

        ApplyExternalKnockback(sourceTransform.position, hitForce, sourceTransform);
    }

    private void ApplyKnockbackFromPoint(Vector3 hitPoint, float hitForce, Transform sourceTransform)
    {
        Vector3 referencePosition = GetReferencePosition();
        Vector3 knockbackDirection = referencePosition - hitPoint;

        if (flattenDirection)
        {
            knockbackDirection.y = 0f;
        }

        if (knockbackDirection.sqrMagnitude < 0.0001f && sourceTransform != null)
        {
            knockbackDirection = referencePosition - sourceTransform.position;

            if (flattenDirection)
            {
                knockbackDirection.y = 0f;
            }
        }

        if (knockbackDirection.sqrMagnitude < 0.0001f && sourceTransform != null)
        {
            knockbackDirection = -sourceTransform.forward;
        }

        if (knockbackDirection.sqrMagnitude < 0.0001f)
        {
            knockbackDirection = Vector3.back;
        }

        knockbackDirection.Normalize();

        if (!Mathf.Approximately(upwardBoost, 0f))
        {
            knockbackDirection = (knockbackDirection + Vector3.up * upwardBoost).normalized;
        }

        if (clearVelocityBeforeKnockback)
        {
            targetRigidbody.linearVelocity = Vector3.zero;
        }

        targetRigidbody.AddForce(knockbackDirection * hitForce, forceMode);
    }

    private bool IsAcceptedObstacle(Transform collisionSource)
    {
        if (collisionSource == null)
        {
            return false;
        }

        TrapDamageOrKnockbackAdapter adapter = collisionSource.GetComponentInParent<TrapDamageOrKnockbackAdapter>();
        if (ignoreTrapAdaptersDuringAutomaticDetection && adapter != null)
        {
            return false;
        }

        if (filterByObstacleLayer)
        {
            GameObject layerSource = adapter != null ? adapter.gameObject : collisionSource.gameObject;
            if ((obstacleLayers.value & (1 << layerSource.layer)) == 0)
            {
                return false;
            }
        }

        if (!requireObstacleTag)
        {
            return true;
        }

        if (HasObstacleTag(collisionSource))
        {
            return true;
        }

        return adapter != null;
    }

    private bool HasObstacleTag(Transform current)
    {
        while (current != null)
        {
            if (SafeCompareTag(current.gameObject, obstacleTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool SafeCompareTag(GameObject target, string tagName)
    {
        if (target == null || string.IsNullOrEmpty(tagName))
        {
            return false;
        }

        try
        {
            return target.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private Vector3 GetReferencePosition()
    {
        return directionReference != null ? directionReference.position : transform.position;
    }
}
