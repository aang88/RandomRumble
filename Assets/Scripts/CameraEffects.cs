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

    [Header("Flash Settings")]
    public float flashDuration = 0.15f;
    public Color flashColor = Color.white;
    // Fix the animation curve initialization
    public AnimationCurve flashCurve;
    private Material flashMaterial;
    private Coroutine flashCoroutine = null;


    void Start()
    {
        // Get the camera component
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        lastPosition = transform.position;

        if (flashCurve.keys.Length == 0)
        {
            flashCurve = new AnimationCurve(
                new Keyframe(0, 1),
                new Keyframe(1, 0)
            );
            flashCurve.preWrapMode = WrapMode.ClampForever;
            flashCurve.postWrapMode = WrapMode.ClampForever;
        }

        flashMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        flashMaterial.hideFlags = HideFlags.HideAndDontSave;
        flashMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        flashMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        flashMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        flashMaterial.SetInt("_ZWrite", 0);
        flashMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
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

        // Calculate new rotation values with NaN checking
        float newPitch = ClampAngle(rotation.x + currentPitchOffset);
        float newYaw = rotation.y;
        float newRoll = currentTiltAngle;

        // Check for NaN values
        if (float.IsNaN(newPitch) || float.IsInfinity(newPitch)) newPitch = rotation.x;
        if (float.IsNaN(newYaw) || float.IsInfinity(newYaw)) newYaw = rotation.y;
        if (float.IsNaN(newRoll) || float.IsInfinity(newRoll)) newRoll = 0f;

        // Apply our effects only if they're valid
        try {
            cam.transform.localEulerAngles = new Vector3(newPitch, newYaw, newRoll);
        }
        catch (System.Exception e) {
            Debug.LogWarning("Failed to set camera rotation: " + e.Message);
            // Reset to safe values
            cam.transform.localEulerAngles = rotation;
        }
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

    public void TriggerFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
            
        flashCoroutine = StartCoroutine(FlashCoroutine());
    }


    private IEnumerator FlashCoroutine()
    {
        // Flash parameters
        float holdTime = 0.05f;        // Time to hold the full flash
        float fadeOutTime = 0.1f;      // Time for fade out
        float maxOpacity = 0.7f;       // Set maximum opacity to 70%
        
        // Hold phase - flash at 70% opacity
        for (int i = 0; i < 5; i++) // Draw for multiple frames to ensure it's visible
        {
            yield return new WaitForEndOfFrame();
            
            // Draw the flash at 70% opacity
            GL.PushMatrix();
            GL.LoadOrtho();
            flashMaterial.SetColor("_Color", new Color(flashColor.r, flashColor.g, flashColor.b, maxOpacity));
            flashMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(1, 0, 0);
            GL.End();
            GL.PopMatrix();
        }
        
        // Wait for the hold time
        yield return new WaitForSecondsRealtime(holdTime);
        
        // Fade-out phase
        float elapsed = 0;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = elapsed / fadeOutTime;
            
            // Smooth step gives a nicer fade out curve
            // Start from maxOpacity instead of 1.0
            float alpha = Mathf.SmoothStep(maxOpacity, 0f, normalizedTime);
            
            yield return new WaitForEndOfFrame();
            
            // Draw the flash with decreasing alpha
            GL.PushMatrix();
            GL.LoadOrtho();
            flashMaterial.SetColor("_Color", new Color(flashColor.r, flashColor.g, flashColor.b, alpha));
            flashMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(1, 0, 0);
            GL.End();
            GL.PopMatrix();
        }
        
        flashCoroutine = null;
    }

    // Add this OnDisable method to clean up materials
    void OnDisable()
    {
        if (flashMaterial != null)
            DestroyImmediate(flashMaterial);
    }


    private IEnumerator ScreenShakeCoroutine()
    {
        Vector3 originalPosition = cam.transform.localPosition;
        float elapsed = 0.0f;
        
        // Use a smoother shake pattern
        while (elapsed < shakeDuration)
        {
            // Use smoother random values with Perlin noise
            float time = Time.unscaledTime;
            // Perlin noise gives values between 0-1, so we adjust to -0.5 to 0.5 range and multiply by magnitude
            float x = (Mathf.PerlinNoise(time * 10, 0) - 0.5f) * shakeMagnitude * 0.6f;
            float y = (Mathf.PerlinNoise(0, time * 10) - 0.5f) * shakeMagnitude * 0.6f;

            // Apply with a smooth falloff as the shake ends
            float dampingFactor = 1f - (elapsed / shakeDuration);
            dampingFactor = dampingFactor * dampingFactor; // Square for a smoother falloff curve
            
            cam.transform.localPosition = new Vector3(
                originalPosition.x + x * dampingFactor, 
                originalPosition.y + y * dampingFactor, 
                originalPosition.z
            );

            elapsed += Time.unscaledDeltaTime;
            yield return new WaitForSecondsRealtime(0.01f); // More consistent timing
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