using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;
using UnityEngine;

public class PlayerMovement1 : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    private float originalWalkSpeed;
    private float originalSprintSpeed;

    [Header("Tilt Settings")]
    public float maxTiltAngle = 15f;
    public float tiltSpeed = 5f;
    public float returnSpeed = 3f;

    [Header("Realistic Jump & Land Camera")]
    public float jumpLookAmount = 3f;
    public float landLookAmount = 4f;
    public float cameraJumpDuration = 1.2f;
    public float cameraLandDuration = 0.8f;
    public float jumpSmoothness = 2.5f;

    [Header("Drag")]
    public float groundDrag;
    [SerializeField] private Transform capsuleTransform;

    [Header("Jump Physics")]
    public float jumpForce;
    public float jumpCooldown;
    private bool hasDoubleJumped = false;
    public float airMultiplier;
    public float fallMultiplier = 2.5f;
    public float gravity = -15f;
    public ConstantForce cf;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 40f;             // Maximum angle player can walk on
    private RaycastHit slopeHit;                  // Store information about the slope
    private bool exitingSlope;                    // Flag for when player jumps off slope
    public float slopeSlideSpeed = 10f;           // Speed at which player slides down steep slopes
    public bool useGravityOnSlopes = true;        // Whether to use gravity on slopes or not

    public Transform weaponHolder;
    public float t = 1f;
    private bool wasBlocking = false;
    bool readyToJump;

    [Header("Double Jump")]
    private float doubleJumpForce = 10f;
    private float doubleJumpDelay = 0.05f;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;
    public Camera cam;
    private bool canDoubleJump = false;
    private bool wasGrounded = true;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale = 0.5f;
    private float startYScale;
    private bool isCrouching = false;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    public Transform modelTransform; // Assign this in inspector to your visual model

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    public Transform orientation;
    public CapsuleCollider playerCollider;

    float horiziontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;
    private CameraEffects cameraEffects;

    // Debug variables
    private bool wasOnSlope = false;
    private bool wasUsingGravity = false;
    private Vector3 lastCFForce = Vector3.zero;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    void Start()
    {
        originalWalkSpeed = walkSpeed;
        originalSprintSpeed = sprintSpeed;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        cf = GetComponent<ConstantForce>();
        
        // If modelTransform isn't assigned, use this transform (for backward compatibility)
        if (modelTransform == null)
            modelTransform = transform;
            
        // Store original values
        startYScale = modelTransform.localScale.y;
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;

        cameraEffects = cam.GetComponent<CameraEffects>();
        if (cameraEffects == null)
            cameraEffects = cam.gameObject.AddComponent<CameraEffects>();

        cameraEffects.jumpLookAmount = jumpLookAmount;
        cameraEffects.landLookAmount = landLookAmount;
        cameraEffects.cameraJumpDuration = cameraJumpDuration;
        cameraEffects.cameraLandDuration = cameraLandDuration;
        cameraEffects.effectSmoothness = jumpSmoothness;
        cameraEffects.maxTiltAngle = maxTiltAngle;
        
        // Force to standing position
        isCrouching = false;
        ApplyStand();
    }

    void Update()
    {
        UnityEngine.Debug.Log($"Grounded Status:");
        UnityEngine.Debug.Log($"Grounded Status: {grounded}");
        float rayLength = (playerHeight * 0.3f);

        UnityEngine.Debug.DrawRay(capsuleTransform.position, Vector3.down * rayLength, Color.red);
        grounded = Physics.Raycast(capsuleTransform.position, Vector3.down, playerHeight * 0.35f, whatIsGround);

    
        // Debug info about gravity
        bool isUsingGravity = rb.useGravity;
        if (isUsingGravity != wasUsingGravity)
        {
            UnityEngine.Debug.Log("Gravity status changed: " + (isUsingGravity ? "GRAVITY ON" : "GRAVITY OFF"));
            wasUsingGravity = isUsingGravity;
        }

        // Debug info about constant force
        if (cf.force != lastCFForce)
        {
            UnityEngine.Debug.Log("Constant Force changed: " + cf.force.ToString("F2"));
            lastCFForce = cf.force;
        }

        // Debug info about movement
        bool isMoving = (Mathf.Abs(horiziontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f);
        UnityEngine.Debug.Log("Movement status: " + (isMoving ? "MOVING" : "NOT MOVING"));

        if (!wasGrounded && grounded)
        {
            ApplyLandCamera();
        }
        wasGrounded = grounded;

        if (rb.velocity.y < 0.1f && rb.velocity.y > -0.1f)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * 1.5f, ForceMode.Acceleration);
        }

        MyInput();
        SpeedControl();
        StateHandler();
        CheckForDoubleJumpBoots();
        CheckIfBlocking();

        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    void FixedUpdate()
    {
        
        MovePlayer();
    }

    private bool OnSlope()
    {
        // Detailed slope detection
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            
            // Debug visualization of slope detection
            UnityEngine.Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.3f), 
                (angle < maxSlopeAngle && angle > 1.0f) ? Color.green : Color.red);
            
            // Check if slope is within walkable angle
            bool isWalkableSlope = angle < maxSlopeAngle && angle > 1.0f;
            
            UnityEngine.Debug.Log($"Slope Detection - Angle: {angle}, Walkable: {isWalkableSlope}");
            
            return isWalkableSlope;
        }
        
        return false;
    }

    private bool OnSteepSlope()
    {
        // Center ray
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            
            if (angle >= maxSlopeAngle)
            {
                UnityEngine.Debug.Log("STEEP SLOPE DETECTED! Angle: " + angle + " degrees");
                return true;
            }
        }
        
        
        return false;
    }


    private void CheckIfBlocking()
    {
        WeaponController weaponController = weaponHolder.gameObject.GetComponent<WeaponController>();
        bool isBlocking = weaponController.isBlocking;

        if (isBlocking && !wasBlocking)
        {
            walkSpeed = originalWalkSpeed * 0.5f;
            sprintSpeed = originalSprintSpeed * 0.5f;
        }
        else if (!isBlocking && wasBlocking)
        {
            walkSpeed = originalWalkSpeed;
            sprintSpeed = originalSprintSpeed;
        }

        wasBlocking = isBlocking;
    }

    private void MyInput()
    {
        horiziontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey))
        {
            if (grounded && readyToJump)
            {
                readyToJump = false;
                hasDoubleJumped = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            else if (canDoubleJump && !hasDoubleJumped && !grounded)
            {
                hasDoubleJumped = true;
                StartCoroutine(DoubleJumpWithDelay());
            }
        }

        // Simple crouch toggle on key down only
        if (Input.GetKeyDown(crouchKey) && grounded)
        {
            ToggleCrouch();
        }
    }
    
    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        
        if (isCrouching)
        {
            ApplyCrouch();
        }
        else
        {
            // Check if there's room to stand
            if (!Physics.Raycast(transform.position, Vector3.up, 2f, whatIsGround))
            {
                ApplyStand();
            }
            else
            {
                // Can't stand up - obstacle above
                isCrouching = true;
            }
        }
    }

    private void ApplyCrouch()
    {
        // Scale the visual model - this won't affect physics
        modelTransform.localScale = new Vector3(
            modelTransform.localScale.x,
            startYScale * crouchYScale,
            modelTransform.localScale.z
        );
        
        // Adjust the collider height for physics
        playerCollider.height = originalColliderHeight * crouchYScale;
        
        // Calculate the center offset to keep the bottom of the collider in place
        float centerYOffset = (originalColliderHeight - playerCollider.height) / 2f;
        playerCollider.center = new Vector3(
            originalColliderCenter.x,
            originalColliderCenter.y - centerYOffset,
            originalColliderCenter.z
        );

        Vector3 cameraPosition = cam.transform.localPosition;
        cam.transform.localPosition = cameraPosition;
    }

    private void ApplyStand()
    {
        // Reset visual model scale
        modelTransform.localScale = new Vector3(
            modelTransform.localScale.x,
            startYScale,
            modelTransform.localScale.z
        );
        
        // Reset collider to original values
        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;
    }

    private void StateHandler()
    {
        if (isCrouching)
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 88, t);
        }
        else if (grounded)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 80, t);
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        // Calculate move direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horiziontalInput;
        
        // Check if player is providing input
        bool isMoving = (Mathf.Abs(horiziontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f);

        // Enhanced slope handling
        if (OnSlope())
        {
            // Disable gravity when on slope
            rb.useGravity = false;
            cf.force = Vector3.zero;

            if (!isMoving)
            {
                // Completely stop with additional checks
                rb.velocity = Vector3.zero;
                
                // Add extra friction to prevent any sliding
                Vector3 velocityToCancel = rb.velocity;
                velocityToCancel.y = 0; // Only cancel horizontal movement
                rb.AddForce(-velocityToCancel * rb.mass * 10f, ForceMode.Acceleration);
                
                UnityEngine.Debug.Log("Stopped on Slope - Velocity Zeroed");
            }
            else
            {
                // Get slope direction adjusted to player's movement
                Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
                
                // Apply movement along the slope
                rb.AddForce(slopeDirection * moveSpeed * 10f, ForceMode.Force);
                
                // Optional: Add slight downward force on downward slopes
                if (slopeDirection.y < 0)
                {
                    rb.AddForce(Vector3.down * 20f, ForceMode.Force);
                }
            }
        }
        else
        {
            // Normal movement physics
            rb.useGravity = !grounded;
            
            if (!grounded)
            {
                cf.force = new Vector3(0, gravity, 0);
            }
            else
            {
                cf.force = Vector3.zero;
            }

            // Normal ground or air movement
            float currentMoveSpeed = grounded ? moveSpeed : moveSpeed * airMultiplier;
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f, ForceMode.Force);
        }

        // Debug logging
        UnityEngine.Debug.Log($"Velocity: {rb.velocity}, On Slope: {OnSlope()}");
    }

    private void SpeedControl()
    {
        // Handling slope speed
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
            {
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }
        // Normal speed control
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        cameraEffects.TriggerJumpEffect();

        // Calculate jump direction based on whether we're on a slope
        Vector3 jumpDirection = transform.up;

        if (OnSlope())
        {
            // Jump perpendicular to the slope's surface
            jumpDirection = Vector3.ProjectOnPlane(jumpDirection, slopeHit.normal).normalized;
        }

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Reset vertical velocity to prevent sliding down during jump
        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse); // Apply the jump force
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    void CheckForDoubleJumpBoots()
    {
        foreach (Transform child in weaponHolder)
        {
            if (child.CompareTag("DoubleJump"))
            {
                canDoubleJump = true;
                break;
            }
        }
    }

    private IEnumerator DoubleJumpWithDelay()
    {
        yield return new WaitForSeconds(doubleJumpDelay);
        DoubleJump();
    }

    private void DoubleJump()
    {
        cameraEffects.TriggerJumpEffect();

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * doubleJumpForce, ForceMode.Impulse);
    }

    private void ApplyLandCamera()
    {
        cameraEffects.TriggerLandEffect();
    }
}