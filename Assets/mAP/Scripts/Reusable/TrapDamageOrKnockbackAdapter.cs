using UnityEngine;

[DisallowMultipleComponent]
public class TrapDamageOrKnockbackAdapter : MonoBehaviour
{
    [Header("Trap Tagging")]
    public string obstacleTag = "Obs";
    public bool applyObstacleTagOnAwake = true;
    public bool tagThisObject = true;
    public bool tagChildColliderObjects = true;

    [Header("Target Detection")]
    public LayerMask targetLayers = ~0;
    public bool reactToCollisions = true;
    public bool reactToTriggers = true;

    [Header("Knockback")]
    public float hitForce = 30f;
    public bool tryReceiverFirst = true;
    public bool fallbackToAttachedRigidbody = true;
    public bool flattenDirection = true;
    public float upwardBoost = 0f;
    public ForceMode forceMode = ForceMode.VelocityChange;

    [Header("Optional Damage Message")]
    public bool sendDamageMessage = false;
    public string damageMethodName = "ApplyDamage";
    public float damageAmount = 1f;

    private void Reset()
    {
        ApplyObstacleTags();
    }

    private void Awake()
    {
        if (applyObstacleTagOnAwake)
        {
            ApplyObstacleTags();
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

        HandleTarget(collision.gameObject, hitPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!reactToTriggers)
        {
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        HandleTarget(other.gameObject, hitPoint);
    }

    [ContextMenu("Apply Obs Tag To Trap")]
    public void ApplyObstacleTags()
    {
        if (tagThisObject)
        {
            TryAssignTag(gameObject);
        }

        if (!tagChildColliderObjects)
        {
            return;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            TryAssignTag(collider.gameObject);
        }
    }

    private void HandleTarget(GameObject targetObject, Vector3 hitPoint)
    {
        if (!IsAcceptedTarget(targetObject))
        {
            return;
        }

        if (sendDamageMessage)
        {
            targetObject.SendMessageUpwards(damageMethodName, damageAmount, SendMessageOptions.DontRequireReceiver);
        }

        if (tryReceiverFirst)
        {
            ObstacleKnockbackReceiver receiver = targetObject.GetComponentInParent<ObstacleKnockbackReceiver>();
            if (receiver != null)
            {
                receiver.ApplyExternalKnockback(hitPoint, hitForce, transform);
                return;
            }
        }

        if (!fallbackToAttachedRigidbody)
        {
            return;
        }

        Rigidbody targetRigidbody = targetObject.GetComponentInParent<Rigidbody>();
        if (targetRigidbody == null || targetRigidbody == GetComponentInParent<Rigidbody>())
        {
            return;
        }

        Vector3 direction = targetRigidbody.worldCenterOfMass - hitPoint;
        if (flattenDirection)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = targetRigidbody.worldCenterOfMass - transform.position;

            if (flattenDirection)
            {
                direction.y = 0f;
            }
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -transform.forward;
        }

        direction.Normalize();

        if (!Mathf.Approximately(upwardBoost, 0f))
        {
            direction = (direction + Vector3.up * upwardBoost).normalized;
        }

        targetRigidbody.AddForce(direction * hitForce, forceMode);
    }

    private bool IsAcceptedTarget(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        if (targetObject == gameObject || targetObject.transform.IsChildOf(transform))
        {
            return false;
        }

        int targetLayerMask = 1 << targetObject.layer;
        return (targetLayers.value & targetLayerMask) != 0;
    }

    private void TryAssignTag(GameObject targetObject)
    {
        if (targetObject == null || string.IsNullOrEmpty(obstacleTag))
        {
            return;
        }

        try
        {
            targetObject.tag = obstacleTag;
        }
        catch (UnityException)
        {
            Debug.LogWarning(
                "TrapDamageOrKnockbackAdapter could not assign tag '" + obstacleTag +
                "'. Add this tag in the target project before using the adapter.",
                this);
        }
    }
}
