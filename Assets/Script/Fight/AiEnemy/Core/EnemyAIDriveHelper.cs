using UnityEngine;

public static class EnemyAIDriveHelper
{
    public static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    public static float GetLocalForwardSpeed(Transform self, Rigidbody rb)
    {
        if (self == null || rb == null)
            return 0f;

        Vector3 flatVel = rb.linearVelocity;
        flatVel.y = 0f;

        Vector3 localVel = self.InverseTransformDirection(flatVel);
        return localVel.z;
    }

    public static void StopCar(RaycastCarController car)
    {
        if (car != null)
            car.SetInput(0f, 0f);
    }

    public static void SetDirectInput(RaycastCarController car, float throttle, float steer)
    {
        if (car != null)
            car.SetInput(throttle, Mathf.Clamp(steer, -1f, 1f));
    }

    public static void ReverseRecoverFromTarget(
        RaycastCarController car,
        Transform self,
        Transform target,
        float reverseThrottle
    )
    {
        if (car == null || self == null)
            return;

        if (target == null)
        {
            car.SetInput(-reverseThrottle, 0f);
            return;
        }

        Vector3 away = self.position - target.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -self.forward;

        Vector3 localAway = self.InverseTransformDirection(away.normalized);
        float steer = Mathf.Clamp(localAway.x, -1f, 1f);

        car.SetInput(-reverseThrottle, steer);
    }

    public static void MoveSmart(
        RaycastCarController car,
        Transform self,
        Vector3 point,
        float throttle,
        float reverseThrottle,
        float steerSensitivity,
        bool allowReverse,
        out float steerOut,
        out float finalThrottleOut
    )
    {
        steerOut = 0f;
        finalThrottleOut = 0f;

        if (car == null || self == null)
            return;

        Vector3 toPoint = point - self.position;
        toPoint.y = 0f;

        if (toPoint.sqrMagnitude < 0.05f)
        {
            car.SetInput(0f, 0f);
            return;
        }

        Vector3 localPoint = self.InverseTransformPoint(point);
        float distance = toPoint.magnitude;

        float steer = Mathf.Clamp(
            (localPoint.x / Mathf.Max(distance, 0.001f)) * steerSensitivity,
            -1f,
            1f
        );

        float finalThrottle = throttle;

        if (allowReverse && localPoint.z < -1.5f)
        {
            finalThrottle = -reverseThrottle;
            steer = Mathf.Clamp(-steer, -1f, 1f);
        }
        else if (localPoint.z < 0f)
        {
            finalThrottle *= 0.55f;
            steer = Mathf.Clamp(steer * 1.25f, -1f, 1f);
        }

        steerOut = steer;
        finalThrottleOut = finalThrottle;
        car.SetInput(finalThrottle, steer);
    }
}