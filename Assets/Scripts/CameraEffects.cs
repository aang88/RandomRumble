using UnityEngine;
using System.Collections;

public class CameraEffects : MonoBehaviour
{
    [Header("Effect Settings")]
    public float jumpLookAmount = 3f;
    public float landLookAmount = 4f;
    public float cameraJumpDuration = 1.2f;
    public float cameraLandDuration = 0.8f;
    public float effectSmoothness = 2.5f;
    public float tileSmoothness = 5f;
    public float maxTiltAngle = 15f;

    [Header("Screen Shake Settings")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.5f;

    

    // Private variables
    private float currentPitchOffset = 0f;
    private float targetPitchOffset = 0f;
    private float currentTiltAngle = 0f;
    private float targetTiltAngle = 0f;
    private Vector3 lastPosition;
    private Camera cam;

    // Animation state tracking
    private Coroutine currentAnimation = null;
    private Coroutine shakeCoroutine = null;


    void Start()
    {
        // Get the camera component
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculate camera tilt based on horizontal movement
        CalculateCameraTilt();

        // Smoothly interpolate current values toward target values
        // This happens every frame regardless of animations
        SmoothUpdateValues();
    }

    private void SmoothUpdateValues()
    {
        // Smoothly update pitch offset for jump/land effects
        currentPitchOffset = Mathf.Lerp(currentPitchOffset, targetPitchOffset, Time.deltaTime * effectSmoothness);

        // Smoothly update tilt angle for strafing
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, Time.deltaTime * tileSmoothness);
    }

    void LateUpdate()
    {
        // Apply both pitch offset and roll tilt every frame
        ApplyCameraEffects();
    }

    private void ApplyCameraEffects()
    {
        // Get current rotation
        Vector3 rotation = cam.transform.localEulerAngles;

        // Apply our effects (pitch offset from jumping/landing and roll from movement)
        // We handle pitch (X) and roll (Z) but leave yaw (Y) alone
        // This preserves the player's ability to look around
        cam.transform.localEulerAngles = new Vector3(
            ClampAngle(rotation.x + currentPitchOffset),
            rotation.y,
            currentTiltAngle
        );
    }

    // Helper function to properly handle angle wrapping
    private float ClampAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    private void CalculateCameraTilt()
    {
        // Calculate movement
        Vector3 movement = transform.position - lastPosition;
        
        // Convert world movement to local movement relative to camera's forward direction
        // This ensures tilt works in all directions the camera is facing
        Vector3 localMovement = transform.InverseTransformDirection(movement);
        
        // Use local right axis (X) for calculating tilt
        float horizontalSpeed = localMovement.x / Time.deltaTime;

        // Set target tilt based on horizontal movement
        targetTiltAngle = -horizontalSpeed * maxTiltAngle * 0.1f;
        targetTiltAngle = Mathf.Clamp(targetTiltAngle, -maxTiltAngle, maxTiltAngle);

        // If not moving much horizontally, gradually return to center
        if (Mathf.Abs(horizontalSpeed) < 0.1f)
        {
            targetTiltAngle = 0f;
        }

        // Store current position for next frame
        lastPosition = transform.position;
    }

    public void TriggerJumpEffect()
    {
        // Stop any running animations
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        // Start new jump animation
        currentAnimation = StartCoroutine(JumpEffectCoroutine());
    }

    public void TriggerLandEffect()
    {
        // Don't interrupt a jump that's in progress
        if (currentAnimation != null && targetPitchOffset < 0)
            return;

        // Stop any running animations
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        // Start new landing animation  
        currentAnimation = StartCoroutine(LandEffectCoroutine());
    }

    private IEnumerator JumpEffectCoroutine()
    {
        float timer = 0f;

        while (timer < cameraJumpDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / cameraJumpDuration;

            if (normalizedTime < 0.4f)
            {
                // Look down at start of jump
                float t = normalizedTime / 0.4f;
                targetPitchOffset = -jumpLookAmount * Mathf.SmoothStep(0, 1, t);
            }
            else if (normalizedTime < 0.7f)
            {
                // Transition to looking up
                float t = (normalizedTime - 0.4f) / 0.3f;
                targetPitchOffset = Mathf.Lerp(-jumpLookAmount, jumpLookAmount * 0.5f, Mathf.SmoothStep(0, 1, t));
            }
            else
            {
                // Return to neutral
                float t = (normalizedTime - 0.7f) / 0.3f;
                targetPitchOffset = Mathf.Lerp(jumpLookAmount * 0.5f, 0, Mathf.SmoothStep(0, 1, t));
            }

            yield return null;
        }

        // Don't set directly to zero - let the smooth update handle it
        targetPitchOffset = 0f;
        currentAnimation = null;
    }

    public void TriggerScreenShake()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ScreenShakeCoroutine());
    }

    private IEnumerator ScreenShakeCoroutine()
    {
        Vector3 originalPosition = cam.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            cam.transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        cam.transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }

    private IEnumerator LandEffectCoroutine()
    {
        float timer = 0f;

        while (timer < cameraLandDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / cameraLandDuration;

            if (normalizedTime < 0.3f)
            {
                // Initial impact dip
                float t = normalizedTime / 0.3f;
                targetPitchOffset = landLookAmount * Mathf.SmoothStep(0, 1, t);
            }
            else
            {
                // Smooth recovery
                float t = (normalizedTime - 0.3f) / 0.7f;
                // Cubic easing for smoother return
                float easedT = 1f - (1f - t) * (1f - t) * (1f - t);
                targetPitchOffset = Mathf.Lerp(landLookAmount, 0, easedT);
            }

            yield return null;
        }

        // Don't set directly to zero - let the smooth update handle it
        targetPitchOffset = 0f;
        currentAnimation = null;
    }
}