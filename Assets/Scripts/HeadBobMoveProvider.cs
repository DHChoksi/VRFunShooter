using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

[RequireComponent(typeof(CharacterController))]
public class HeadBobMoveProvider : LocomotionProvider
{
    [Header("References")]
    [SerializeField] XROrigin xrOrigin;                 // If null, will auto-find
    [SerializeField] Camera playerCamera;               // If null, will use xrOrigin.Camera
    [SerializeField] CharacterController controller;    // Required (auto-fills)

    [Header("Input")]
    [Tooltip("Vector2 move input (e.g., left joystick).")]
    [SerializeField] InputActionProperty moveAction;

    [Header("Auto-Binding (optional)")]
    [SerializeField] bool autoBindLeftThumbstick = true;

    InputAction runtimeMoveAction;
    bool UsingRuntimeAction => runtimeMoveAction != null;

    [Header("Movement")]
    [Tooltip("Meters per second walking speed.")]
    [SerializeField, Min(0f)] float moveSpeed = 2.0f;

    [Tooltip("If true, ignore strafing and always move where the head is looking.")]
    [SerializeField] bool forwardOnly = true;

    [Tooltip("Apply gravity while moving.")]
    [SerializeField] bool useGravity = true;
    [SerializeField] float gravity = -9.81f;

    [Header("Head Bobbing")]
    [Tooltip("Vertical bob amplitude in meters.")]
    [SerializeField, Min(0f)] float bobAmplitude = 0.03f;
    [Tooltip("Base bob frequency in cycles/sec at full speed.")]
    [SerializeField, Min(0f)] float bobFrequency = 1.8f;
    [Tooltip("Minimum horizontal speed to start bobbing.")]
    [SerializeField, Min(0f)] float bobStartSpeed = 0.05f;
    [Tooltip("How quickly the bob returns to neutral when stopping.")]
    [SerializeField, Range(1f, 20f)] float bobDamp = 8f;

    [Header("Footsteps")]
    [SerializeField] AudioSource footstepSource;        // One-shot AudioSource (no loop)
    [SerializeField] AudioClip[] footstepClips;
    [Tooltip("Footstep cadence at full speed (steps/sec, each foot).")]
    [SerializeField, Min(0.1f)] float stepFrequency = 1.8f;

    // Internal
    float verticalVelocity;
    float stepPhase;               // 0..1 cycling phase for footstep timing
    float bobPhase;                // 0..1 cycling phase for head bob
    Vector3 camLocalStart;         // original camera local pos (for bobbing)
    bool cachedHadCamera;

    protected void Reset()
    {
        controller = GetComponent<CharacterController>();
        xrOrigin = GetComponentInParent<XROrigin>();
    }

    protected void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (xrOrigin == null) xrOrigin = GetComponentInParent<XROrigin>();
        if (playerCamera == null && xrOrigin != null) playerCamera = xrOrigin.Camera;

        if (playerCamera != null)
        {
            camLocalStart = playerCamera.transform.localPosition;
            cachedHadCamera = true;
        }

        EnsureMoveActionBound();
    }

    protected void OnEnable()
    {
        if (UsingRuntimeAction) runtimeMoveAction.Enable();
        else moveAction.action?.Enable(); 
    }

    protected void OnDisable()   
    {
        if (UsingRuntimeAction) runtimeMoveAction.Disable();
        else moveAction.action?.Disable();

        RestoreHeadPositionImmediate();
    }

    void OnDestroy()
    {
        // Clean up the runtime action if we created one
        if (UsingRuntimeAction) runtimeMoveAction.Dispose();
    }



    void Update() 
    {
        if (xrOrigin == null || controller == null)
            return;

        if (playerCamera == null && xrOrigin != null)
            playerCamera = xrOrigin.Camera;

        // Cache/start camera local position if camera just appeared (late init)
        if (!cachedHadCamera && playerCamera != null)
        {
            camLocalStart = playerCamera.transform.localPosition;
            cachedHadCamera = true;
        }

        // Read input (Vector2)
        Vector2 input = moveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        // Compute planar look-based direction(s)
        Vector3 fwd = Vector3.ProjectOnPlane(playerCamera != null ? playerCamera.transform.forward : transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd); // right-hand, planar

        Vector3 moveDir;
        float inputMag;

        if (forwardOnly)
        {
            inputMag = input.magnitude;             // any stick pressure -> forward
            moveDir = fwd;
        }
        else
        {
            inputMag = Mathf.Clamp01(input.magnitude);
            moveDir = (fwd * input.y + right * input.x).normalized;
        }

        // Exclusive locomotion request only when moving
        bool wantsToMove = inputMag > 0.01f;
        if (wantsToMove && system != null && system.RequestExclusiveOperation(this) == RequestResult.Success) // uses LocomotionSystem API.  :contentReference[oaicite:5]{index=5}
        {
            ApplyMovement(moveDir, inputMag);
            system.FinishExclusiveOperation(this); // release immediately this frame  :contentReference[oaicite:6]{index=6}
        }
        else
        {
            // Even if we can't acquire exclusivity, still keep bob returning to neutral.
            DampenHeadBobToNeutral();
        }
    }

    void EnsureMoveActionBound()
    {
        if (!autoBindLeftThumbstick) return;
        var assigned = moveAction.action;
        if (assigned != null && assigned.bindings.Count > 0) return;

        runtimeMoveAction = new InputAction("Runtime Move", InputActionType.Value, "<XRController>{LeftHand}/primary2DAxis");
        runtimeMoveAction.AddBinding("<XRController>{LeftHand}/thumbstick");
        runtimeMoveAction.AddBinding("<OculusTouchController>{LeftHand}/thumbstick");
        runtimeMoveAction.AddBinding("<Gamepad>/leftStick"); // fallback for testing
        runtimeMoveAction.Enable();

        moveAction = new InputActionProperty(runtimeMoveAction);
    }

    void ApplyMovement(Vector3 moveDir, float inputMag)
    {
        // Horizontal velocity
        Vector3 horiz = moveDir * (moveSpeed * inputMag);

        // Gravity
        if (useGravity)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -0.5f; // stick to ground
            else
                verticalVelocity += gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0f;
        }

        Vector3 motion = horiz * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime;
        controller.Move(motion);

        // Head bob & footsteps (based on horizontal speed)
        float horizSpeed = horiz.magnitude; // m/s scaled by inputMag
        UpdateHeadBobAndSteps(horizSpeed);
    }

    void UpdateHeadBobAndSteps(float horizSpeed)
    {
        if (playerCamera == null) return;

        float speed01 = Mathf.Clamp01(horizSpeed / Mathf.Max(0.01f, moveSpeed));

        // Step phase advances with speed; each 0..1 cycle = one full stride (2 footsteps).
        float stepAdv = speed01 * stepFrequency * Time.deltaTime;
        stepPhase = (stepPhase + stepAdv) % 1f;

        // Two steps per cycle at phases 0.0 and 0.5
        TriggerFootstepIfCrossed(0.0f);
        TriggerFootstepIfCrossed(0.5f);

        // Bobbing: simple vertical sine tied to the same phase, scaled by amplitude
        // Only bob when moving faster than threshold; otherwise, damp back to neutral.
        if (horizSpeed > bobStartSpeed)
        {
            bobPhase = stepPhase;
            float bobOffset = Mathf.Sin(bobPhase * Mathf.PI * 2f) * bobAmplitude;
            Vector3 local = playerCamera.transform.localPosition;
            local.y = Mathf.Lerp(local.y, camLocalStart.y + bobOffset, 1f - Mathf.Exp(-bobDamp * Time.deltaTime));
            playerCamera.transform.localPosition = local;
        }
        else
        {
            DampenHeadBobToNeutral();
        }
    }

    void TriggerFootstepIfCrossed(float targetPhase)
    {
        // Detect wrap-around crossing past targetPhase this frame.
        // Because stepPhase only increases, we can check if last frame was on the other side.
        // Store last phase per-target using static fields? Simpler: compute using delta and modulo.
        // We'll approximate by firing when stepPhase just entered a small window after target.
        const float window = 0.04f; // ~40ms at 60fps
        float dist = Mathf.Abs(Mathf.DeltaAngle(stepPhase * 360f, targetPhase * 360f)) / 360f;
        if (dist < window && footstepSource && footstepClips != null && footstepClips.Length > 0)
        {
            // Avoid double-fire by ensuring source is not already playing the same frame
            if (!footstepSource.isPlaying)
            {
                var clip = footstepClips[Random.Range(0, footstepClips.Length)];
                footstepSource.PlayOneShot(clip);
            }
        }
    }

    void DampenHeadBobToNeutral()
    {
        if (playerCamera == null) return;
        Vector3 local = playerCamera.transform.localPosition;
        local.y = Mathf.Lerp(local.y, camLocalStart.y, 1f - Mathf.Exp(-bobDamp * Time.deltaTime));
        playerCamera.transform.localPosition = local;
    }

    void RestoreHeadPositionImmediate()
    {
        if (playerCamera == null) return;
        var local = playerCamera.transform.localPosition;
        local.y = camLocalStart.y;
        playerCamera.transform.localPosition = local;
    }
}
