using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastCarController : MonoBehaviour
{

    [Header("Extra Gravity")]
    public float extraGravity = 20f;
    public int groundedWheelsNeededToDisableExtraGravity = 2;

    [Header("Refs")]
    public Rigidbody rb;
    public Transform FL, FR, RL, RR;
    public LayerMask groundMask;

    [Header("Suspension")]
    public float restLength = 0.5f;
    public float springStrength = 120f;
    public float damperStrength = 40f;

    [Header("Drive")]
    public float driveForce = 35f;
    public float reverseForce = 15f;
    public float maxSpeed = 30f;
    public float reverseMaxSpeed = 12f;
    public float stopToReverseSpeed = 0.8f;
    public float reverseSteerRate = 0.2f;

    [Header("Steer Physics")]
    public float turnTorque = 25f;
    public float minSpeedToTurn = 2f;

    [Header("Grip")]
    public float lateralGrip = 5f;

    [Header("Dead Body Physics")]
    public float deadLongitudinalDamping = 8f;
    public float deadLateralGripBonus = 15f;
    public float deadAngularDamping = 6f;

    private bool deathGripApplied = false;
    private float currentGripBonus = 0f;

    [Header("Yaw Damping")]
    public float yawDamping = 3.5f;
    public float yawDampingWhenNoSteer = 6f;
    public float yawSnapThreshold = 0.08f;

    public Vector3 centerOfMass;

    [Header("Wheel Visuals")]
    public Transform FLSteer;
    public Transform FRSteer;
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    [Header("Wheel Setup")]
    public float wheelRadius = 0.24f;
    public float wheelMaxDrop = 0.2f;

    [Header("Wheel Steering Visual")]
    public float lowSpeedSteerAngle = 30f;
    public float highSpeedSteerAngle = 20f;
    public float steerAngleFadeSpeed = 15f;
    public float steerVisualSpeed = 120f;
    public float highSpeedForMinAngle = 20f;

    float wheelRollAngle = 0f;
    float currentBodyRoll = 0f;
    float currentBodyPitch = 0f;
    Vector3 lastVelocity = Vector3.zero;

    int groundedCount;
    float currentSteerAngle = 0f;
    float currentMaxSteerAngle = 0f;

    float throttleInput = 0f;
    float steerInput = 0f;
    float brakeInput = 0f;

    [Header("Omni Drive")]
    public bool omniDriveMode = false;
    public float omniMinInput = 0.1f;
    public float coastSteerFactor = 0.55f;

    Vector2 omniMoveInput = Vector2.zero;

    float currentOmniSteer = 0f;
    public float omniSteerSmooth = 20f;

    [Header("Drive Curves")]
    public AnimationCurve powerCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Steer Curve")]
    public AnimationCurve steerBySpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Body Visual Tilt")]
    public Transform bodyVisual;

    public float maxPitchAngle = 6f;
    public float maxRollAngle = 8f;
    public float tiltSmooth = 6f;

    public float pitchFromAccel = 0.15f;
    public float rollFromAccel = 0.10f;

    public float minPitchAccel = 1.5f;
    public float minRollAccel = 1.5f;

    [Header("Brake")]
    public float brakeForce = 45f;
    public float gamepadBrakeForce = 25f;
    public float idleBrakeForce = 7f;

    [Header("Active Self Righting")]
    public bool allowSelfRighting = true;
    public Key selfRightKey = Key.R;
    public float selfRightUpForce = 80f;
    public float selfRightRollTorque = 50f;
    public float selfRightCooldown = 1.2f;
    public float selfRightAngularDamping = 0.5f;
    public float stuckSpeedThreshold = 0.5f;
    public float stuckTimeToAllowRighting = 2f;

    [Header("Death State")]
    public bool isDead = false;

    float selfRightTimer = 0f;
    bool selfRightPressed = false;

    float stuckTimer = 0f;

    public void ApplyDeathGripBonus()
    {
        if (deathGripApplied) return;

        currentGripBonus += deadLateralGripBonus;
        deathGripApplied = true;
    }

    public void SetDeadState()
    {
        if (isDead) return;
        isDead = true;
        ApplyDeathGripBonus();
    }

    public void SetInput(float throttle, float steer)
    {
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
    }

    public void SetBrakeInput(float brake)
    {
        brakeInput = Mathf.Clamp01(brake);
    }

    public void SetOmniInput(Vector2 moveInput)
    {
        omniMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    public void RequestSelfRight()
    {
        if (!allowSelfRighting) return;
        selfRightPressed = true;
    }

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.centerOfMass = centerOfMass;
        currentMaxSteerAngle = lowSpeedSteerAngle;
        lastVelocity = rb.linearVelocity;
    }

    void Update()
    {
        if (isDead) return;
        if (!allowSelfRighting) return;

        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current[selfRightKey].wasPressedThisFrame;

        if (keyboardPressed)
        {
            RequestSelfRight();
        }
    }

    void FixedUpdate()
    {
        groundedCount = 0;

        Suspension(FL);
        Suspension(FR);
        Suspension(RL);
        Suspension(RR);

        if (!isDead)
        {
            if (omniDriveMode)
            {
                OmniDrive();
            }
            else
            {
                Drive();
                Steer();
            }

            ApplyExtraGravity();
            ApplyLateralGrip();
            ApplyYawDamping();

            UpdateWheelVisual(FL, wheelFL);
            UpdateWheelVisual(FR, wheelFR);
            UpdateWheelVisual(RL, wheelRL);
            UpdateWheelVisual(RR, wheelRR);

            UpdateSteeringVisual();
            UpdateWheelRollVisual();
            UpdateBodyVisualTilt();

            UpdateStuckState();
            HandleSelfRighting();
        }
        else
        {
            ApplyExtraGravity();
            ApplyLateralGrip();
            ApplyDeadBodyDamping();
        }
    }

    bool Suspension(Transform p)
    {
        Vector3 origin = p.position;
        Vector3 dir = -transform.up;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, restLength, groundMask))
        {
            groundedCount++;

            float offset = restLength - hit.distance;

            Vector3 pointVel = rb.GetPointVelocity(p.position);
            float velAlongSpring = Vector3.Dot(transform.up, pointVel);

            float force = (offset * springStrength) - (velAlongSpring * damperStrength);

            rb.AddForceAtPosition(transform.up * force, p.position, ForceMode.Force);

            Debug.DrawLine(origin, hit.point, Color.green);
            return true;
        }
        else
        {
            Debug.DrawLine(origin, origin + dir * restLength, Color.red);
            return false;
        }
    }

    void Drive()
    {
        float groundFactor = Mathf.Clamp01(groundedCount / 2f);
        if (groundFactor <= 0f) return;

        if (brakeInput > 0.01f)
        {
            currentOmniSteer = 0f;

            Vector3 brakeLocalVel = transform.InverseTransformDirection(rb.linearVelocity);
            float brakeForwardSpeed = brakeLocalVel.z;

            if (Mathf.Abs(brakeForwardSpeed) > 0.01f)
            {
                float brakeDir = -Mathf.Sign(brakeForwardSpeed);
                rb.AddForce(transform.forward * brakeDir * gamepadBrakeForce * brakeInput * groundFactor, ForceMode.Acceleration);

                if (Mathf.Abs(brakeForwardSpeed) < 0.2f)
                {
                    brakeLocalVel.z = 0f;
                    rb.linearVelocity = transform.TransformDirection(brakeLocalVel);
                }
            }

            return;
        }

        float v = throttleInput;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = localVel.z;

        if (v > 0f)
        {
            if (forwardSpeed < -0.1f)
            {
                rb.AddForce(transform.forward * brakeForce * v * groundFactor, ForceMode.Acceleration);
            }
            else
            {
                float speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
                float availablePower = powerCurve.Evaluate(speed01) * driveForce * v * groundFactor;

                if (forwardSpeed < maxSpeed)
                {
                    rb.AddForce(transform.forward * availablePower, ForceMode.Acceleration);
                }
            }
        }
        else if (v < 0f)
        {
            float reverseInput = -v;

            if (forwardSpeed > stopToReverseSpeed)
            {
                rb.AddForce(-transform.forward * brakeForce * reverseInput * groundFactor, ForceMode.Acceleration);
            }
            else
            {
                float reverseSpeed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / reverseMaxSpeed);
                float reversePower = reverseForce * reverseInput * (1f - reverseSpeed01);

                if (forwardSpeed > -reverseMaxSpeed)
                {
                    rb.AddForce(-transform.forward * reversePower * groundFactor, ForceMode.Acceleration);
                }
            }
        }
        else
        {
            if (Mathf.Abs(forwardSpeed) > 0.01f)
            {
                float brakeDir = -Mathf.Sign(forwardSpeed);
                rb.AddForce(transform.forward * brakeDir * idleBrakeForce * groundFactor, ForceMode.Acceleration);

                if (Mathf.Abs(forwardSpeed) < 0.15f)
                {
                    localVel.z = 0f;
                    rb.linearVelocity = transform.TransformDirection(localVel);
                }
            }
        }
    }

    void Steer()
    {
        float groundFactor = Mathf.Clamp01(groundedCount / 2f);
        if (groundFactor <= 0f) return;

        float h = omniDriveMode ? currentOmniSteer : steerInput;
        if (Mathf.Abs(h) < 0.01f) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeedSigned = localVel.z;
        float forwardSpeedAbs = Mathf.Abs(forwardSpeedSigned);

        float speedFactor = Mathf.InverseLerp(minSpeedToTurn, minSpeedToTurn + 8f, forwardSpeedAbs);
        speedFactor = speedFactor * speedFactor;

        float throttleAbs = Mathf.Abs(throttleInput);
        float throttleFactor = Mathf.Lerp(0.15f, 1f, throttleAbs);

        float speed01 = Mathf.Clamp01(forwardSpeedAbs / highSpeedForMinAngle);
        float steerFactor = steerBySpeedCurve.Evaluate(speed01);

        float launchSteerLimit = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(forwardSpeedAbs / 6f));

        float steerTorque = turnTorque * steerFactor * speedFactor * throttleFactor * launchSteerLimit * groundFactor;

        if (forwardSpeedSigned < -0.1f)
        {
            h = -h;
            steerTorque *= reverseSteerRate;
        }

        rb.AddTorque(transform.up * (h * steerTorque), ForceMode.Acceleration);
    }

    void ApplyLateralGrip()
    {
        float groundFactor = Mathf.Clamp01(groundedCount / 2f);
        if (groundFactor <= 0f) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        float finalGrip = lateralGrip + currentGripBonus;
        localVel.x /= (1f + finalGrip * groundFactor * Time.fixedDeltaTime);

        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    void UpdateWheelVisual(Transform point, Transform wheelVisual)
    {
        Vector3 origin = point.position;
        Vector3 dir = -transform.up;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, restLength, groundMask))
        {
            wheelVisual.position = hit.point + transform.up * wheelRadius;
        }
        else
        {
            wheelVisual.position = origin + dir * wheelMaxDrop;
        }
    }

    void UpdateSteeringVisual()
    {
        float h;

        if (omniDriveMode)
        {
            h = currentOmniSteer;

            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            if (localVel.z < -0.1f)
            {
                h = -h;
            }
        }
        else
        {
            h = steerInput;
        }

        Vector3 localVelForSpeed = transform.InverseTransformDirection(rb.linearVelocity);
        float speed = Mathf.Abs(localVelForSpeed.z);

        float speed01 = Mathf.Clamp01(speed / highSpeedForMinAngle);

        float targetMaxSteerAngle = Mathf.Lerp(lowSpeedSteerAngle, highSpeedSteerAngle, speed01);

        currentMaxSteerAngle = Mathf.Lerp(
            currentMaxSteerAngle,
            targetMaxSteerAngle,
            steerAngleFadeSpeed * Time.fixedDeltaTime
        );

        float targetSteerAngle = h * currentMaxSteerAngle;

        currentSteerAngle = Mathf.MoveTowards(
            currentSteerAngle,
            targetSteerAngle,
            steerVisualSpeed * Time.fixedDeltaTime
        );

        FLSteer.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
        FRSteer.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
    }

    void UpdateWheelRollVisual()
    {
        Vector3 flatVel = rb.linearVelocity;
        flatVel.y = 0f;

        float forwardSpeed = Vector3.Dot(flatVel, transform.forward);

        float circumference = 2f * Mathf.PI * wheelRadius;
        float degreesPerSecond = (forwardSpeed / circumference) * 360f;

        wheelRollAngle += degreesPerSecond * Time.fixedDeltaTime;

        wheelFL.localRotation = Quaternion.Euler(0, -90f, -wheelRollAngle);
        wheelFR.localRotation = Quaternion.Euler(0, -90f, -wheelRollAngle);
        wheelRL.localRotation = Quaternion.Euler(0, -90f, -wheelRollAngle);
        wheelRR.localRotation = Quaternion.Euler(0, -90f, -wheelRollAngle);
    }

    void UpdateBodyVisualTilt()
    {
        if (bodyVisual == null) return;

        Vector3 acceleration = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        Vector3 localAccel = transform.InverseTransformDirection(acceleration);

        float pitchAccel = localAccel.z;

        float targetPitch = 0f;
        if (Mathf.Abs(pitchAccel) > minPitchAccel)
        {
            targetPitch = -pitchAccel * pitchFromAccel;
        }

        targetPitch = Mathf.Clamp(targetPitch, -maxPitchAngle, maxPitchAngle);

        float rollAccel = localAccel.x;

        float targetRoll = 0f;
        if (Mathf.Abs(rollAccel) > minRollAccel)
        {
            targetRoll = rollAccel * rollFromAccel;
        }

        targetRoll = Mathf.Clamp(targetRoll, -maxRollAngle, maxRollAngle);

        currentBodyPitch = Mathf.Lerp(currentBodyPitch, targetPitch, tiltSmooth * Time.fixedDeltaTime);
        currentBodyRoll = Mathf.Lerp(currentBodyRoll, targetRoll, tiltSmooth * Time.fixedDeltaTime);

        bodyVisual.localRotation = Quaternion.Euler(currentBodyPitch, 0f, currentBodyRoll);

        lastVelocity = rb.linearVelocity;
    }

    void HandleSelfRighting()
    {
        if (!allowSelfRighting) return;

        if (selfRightTimer > 0f)
        {
            selfRightTimer -= Time.fixedDeltaTime;
        }

        if (!selfRightPressed) return;
        selfRightPressed = false;

        if (selfRightTimer > 0f) return;
        if (stuckTimer < stuckTimeToAllowRighting) return;

        rb.angularVelocity *= selfRightAngularDamping;
        rb.AddForce(Vector3.up * selfRightUpForce, ForceMode.Impulse);

        float rightDot = Vector3.Dot(transform.right, Vector3.up);
        Vector3 rollAxis = rightDot > 0f ? -transform.forward : transform.forward;

        rb.AddTorque(rollAxis * selfRightRollTorque, ForceMode.Impulse);

        selfRightTimer = selfRightCooldown;
        stuckTimer = 0f;
    }

    void UpdateStuckState()
    {
        bool tooFewWheelsGrounded = groundedCount <= 1;

        Vector3 flatVel = rb.linearVelocity;
        flatVel.y = 0f;
        bool isAlmostStopped = flatVel.magnitude < stuckSpeedThreshold;

        if (tooFewWheelsGrounded && isAlmostStopped)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    void OmniDrive()
    {
        float groundFactor = Mathf.Clamp01(groundedCount / 2f);
        if (groundFactor <= 0f) return;

        Vector2 input = omniMoveInput;
        Vector3 localVelNow = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeedSigned = localVelNow.z;
        float forwardSpeedAbs = Mathf.Abs(forwardSpeedSigned);

        if (brakeInput > 0.01f && throttleInput <= 0.01f)
        {
            if (forwardSpeedSigned > stopToReverseSpeed)
            {
                rb.AddForce(
                    -transform.forward * gamepadBrakeForce * brakeInput * groundFactor,
                    ForceMode.Acceleration
                );

                currentOmniSteer = 0f;
                return;
            }

            Vector2 reverseInput = input;

            if (reverseInput.magnitude >= omniMinInput)
            {
                Vector3 desiredReverseDir =
                    transform.forward * reverseInput.y + transform.right * reverseInput.x;
                desiredReverseDir.Normalize();

                Vector3 rearDir = -transform.forward;

                float reverseSignedAngle =
                    Vector3.SignedAngle(rearDir, desiredReverseDir, Vector3.up);

                if (Mathf.Abs(reverseSignedAngle) < 5f)
                {
                    reverseSignedAngle = 0f;
                }

                float reverseTargetSteer =
                    Mathf.Clamp(reverseSignedAngle / 90f, -1f, 1f);

                currentOmniSteer = Mathf.Lerp(
                    currentOmniSteer,
                    reverseTargetSteer,
                    omniSteerSmooth * Time.fixedDeltaTime
                );

                float reverseSpeedFactor =
                    Mathf.InverseLerp(minSpeedToTurn, minSpeedToTurn + 6f, forwardSpeedAbs);

                float reverseCurve01 =
                    Mathf.Clamp01(forwardSpeedAbs / highSpeedForMinAngle);

                float reverseSteerCurve = steerBySpeedCurve.Evaluate(reverseCurve01);

                float reverseSteerTorque =
                    turnTorque * reverseSteerCurve * reverseSpeedFactor * reverseSteerRate * groundFactor;

                rb.AddTorque(
                    transform.up * (currentOmniSteer * reverseSteerTorque),
                    ForceMode.Acceleration
                );

                Vector3 av = rb.angularVelocity;
                av.y *= 0.985f;
                rb.angularVelocity = av;
            }
            else
            {
                currentOmniSteer = Mathf.Lerp(
                    currentOmniSteer,
                    0f,
                    omniSteerSmooth * Time.fixedDeltaTime
                );

                Vector3 av = rb.angularVelocity;
                av.y *= 0.90f;
                rb.angularVelocity = av;
            }

            float reversePower01 = Mathf.Clamp01(forwardSpeedAbs / reverseMaxSpeed);
            float reversePower = reverseForce * brakeInput * (1f - reversePower01);

            if (forwardSpeedSigned > -reverseMaxSpeed)
            {
                rb.AddForce(
                    -transform.forward * reversePower * groundFactor,
                    ForceMode.Acceleration
                );
            }

            return;
        }

        if (throttleInput <= 0.01f)
        {
            if (Mathf.Abs(forwardSpeedSigned) > 0.01f)
            {
                float brakeDir = -Mathf.Sign(forwardSpeedSigned);
                rb.AddForce(
                    transform.forward * brakeDir * idleBrakeForce * groundFactor,
                    ForceMode.Acceleration
                );

                if (Mathf.Abs(forwardSpeedSigned) < 0.15f)
                {
                    localVelNow.z = 0f;
                    rb.linearVelocity = transform.TransformDirection(localVelNow);
                }
            }

            if (input.magnitude >= omniMinInput && forwardSpeedAbs > minSpeedToTurn)
            {
                Vector3 coastDesiredDir =
                    transform.forward * input.y + transform.right * input.x;
                coastDesiredDir.Normalize();

                float coastSignedAngle =
                    Vector3.SignedAngle(transform.forward, coastDesiredDir, Vector3.up);

                float coastTargetSteer =
                    Mathf.Clamp(coastSignedAngle / 70f, -1f, 1f);

                currentOmniSteer = Mathf.Lerp(
                    currentOmniSteer,
                    coastTargetSteer,
                    omniSteerSmooth * Time.fixedDeltaTime
                );

                float coastSteerAmount = currentOmniSteer;

                float coastSpeedFactor =
                    Mathf.InverseLerp(minSpeedToTurn, minSpeedToTurn + 6f, forwardSpeedAbs);

                float coastCurve01 =
                    Mathf.Clamp01(forwardSpeedAbs / highSpeedForMinAngle);

                float coastSteerCurve = steerBySpeedCurve.Evaluate(coastCurve01);

                float coastSteerTorque =
                    turnTorque * coastSteerCurve * coastSpeedFactor * coastSteerFactor * groundFactor;

                rb.AddTorque(
                    transform.up * (coastSteerAmount * coastSteerTorque),
                    ForceMode.Acceleration
                );
            }
            else
            {
                currentOmniSteer = 0f;
            }

            return;
        }

        if (input.magnitude < omniMinInput)
        {
            currentOmniSteer = 0f;

            float straightPower01 = Mathf.Clamp01(Mathf.Abs(forwardSpeedSigned) / maxSpeed);
            float straightDrivePower =
                powerCurve.Evaluate(straightPower01) * driveForce * throttleInput * groundFactor;

            if (forwardSpeedSigned < maxSpeed)
            {
                rb.AddForce(
                    transform.forward * straightDrivePower,
                    ForceMode.Acceleration
                );
            }

            return;
        }

        Vector3 driveDesiredDir =
            transform.forward * input.y + transform.right * input.x;
        driveDesiredDir.Normalize();

        float driveSignedAngle =
            Vector3.SignedAngle(transform.forward, driveDesiredDir, Vector3.up);

        float driveTargetSteer =
            Mathf.Clamp(driveSignedAngle / 70f, -1f, 1f);

        currentOmniSteer = Mathf.Lerp(
            currentOmniSteer,
            driveTargetSteer,
            omniSteerSmooth * Time.fixedDeltaTime
        );

        float driveSteerAmount = currentOmniSteer;

        float driveSpeedFactor =
            Mathf.InverseLerp(minSpeedToTurn, minSpeedToTurn + 6f, forwardSpeedAbs);

        float driveThrottleFactor = Mathf.Lerp(0.2f, 1f, throttleInput);

        float driveCurve01 =
            Mathf.Clamp01(forwardSpeedAbs / highSpeedForMinAngle);

        float driveSteerCurve = steerBySpeedCurve.Evaluate(driveCurve01);

        float driveSteerTorque =
            turnTorque * driveSteerCurve * driveSpeedFactor * driveThrottleFactor * groundFactor;

        rb.AddTorque(
            transform.up * (driveSteerAmount * driveSteerTorque),
            ForceMode.Acceleration
        );

        float drivePower01 = Mathf.Clamp01(forwardSpeedAbs / maxSpeed);
        float driveForwardPower =
            powerCurve.Evaluate(drivePower01) * driveForce * throttleInput * groundFactor;

        if (forwardSpeedSigned < maxSpeed)
        {
            rb.AddForce(
                transform.forward * driveForwardPower,
                ForceMode.Acceleration
            );
        }
    }

    void ApplyYawDamping()
    {
        float groundFactor = Mathf.Clamp01(groundedCount / 2f);
        if (groundFactor <= 0f) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float speedAbs = Mathf.Abs(localVel.z);

        Vector3 angVel = rb.angularVelocity;

        float baseDamping = Mathf.Abs(steerInput) > 0.05f ? yawDamping : yawDampingWhenNoSteer;

        float speedFactor = 1f - Mathf.Clamp01(speedAbs / highSpeedForMinAngle);
        float finalDamping = Mathf.Lerp(baseDamping * 0.45f, baseDamping, speedFactor);

        angVel.y /= (1f + finalDamping * groundFactor * Time.fixedDeltaTime);

        if (Mathf.Abs(angVel.y) < yawSnapThreshold)
            angVel.y = 0f;

        rb.angularVelocity = angVel;
    }

    void ApplyExtraGravity()
    {
        if (rb == null) return;

        if (groundedCount >= groundedWheelsNeededToDisableExtraGravity)
            return;

        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    void ApplyDeadBodyDamping()
    {
        if (!isDead) return;

        float groundFactor = Mathf.Clamp01(groundedCount / 2f);
        if (groundFactor <= 0f) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        localVel.z /= (1f + deadLongitudinalDamping * groundFactor * Time.fixedDeltaTime);

        if (Mathf.Abs(localVel.z) < 0.08f)
            localVel.z = 0f;

        rb.linearVelocity = transform.TransformDirection(localVel);

        Vector3 angVel = rb.angularVelocity;
        angVel.y /= (1f + deadAngularDamping * groundFactor * Time.fixedDeltaTime);

        if (Mathf.Abs(angVel.y) < 0.05f)
            angVel.y = 0f;

        rb.angularVelocity = angVel;
    }
}