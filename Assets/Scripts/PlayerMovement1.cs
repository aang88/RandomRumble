using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;
using FishNet.Object;
using UnityEngine;

public class PlayerMovement1 : NetworkBehaviour
{
    #region Variables
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
    public float maxSlopeAngle = 40f;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    public float slopeSlideSpeed = 10f;
    public bool useGravityOnSlopes = true;
    public float downforceOnSlopes = 30f;

    [Header("Sliding")]
    public float slideForce = 400f;
    public float slideDuration = 1.0f;
    public float slideYScale = 0.4f;
    public float slideCooldown = 1.5f;
    private bool isSliding = false;
    private bool readyToSlide = true;
    private Vector3 slideDirection;
    private float slideTimer;
    public bool continueSlidingOnSlopes = true;
    public float slopeSlideAcceleration = 5f;
    public float maxSlopeSlideSpeed = 25f;
    public float upSlopeSpeedReduction = 0.7f;
    public float maxLaunchVelocity = 10f;
    private bool isSlidingOnSlope = false;
    private Vector3 previousSlopeNormal = Vector3.up;


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
    public float coyoteTime = 0.15f;
    private float coyoteTimeCounter;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale = 0.5f;
    private float startYScale;
    private bool isCrouching = false;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    public Transform modelTransform;

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

    public float fovTransitionSpeed = 3f;

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
        sliding,
        air
    }
    #endregion

    #region Core Functions
    public override void OnStartNetwork()
    {
        UnityEngine.Debug.Log("Initializing player");
        base.OnStartNetwork();

        if (IsServer)
        {
            Entity playerEntity = GetComponent<Entity>();
            if (playerEntity != null)
            {
                UnityEngine.Debug.Log("Attempting to add a player: " + playerEntity);
                GameStateManager.Instance.AddPlayer(playerEntity);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Player Entity is missing from the GameObject!");
            }
        }

        if (!base.Owner.IsLocalClient)
        {
         
            cam.gameObject.SetActive(false); // Disable camera for other players
            return;
        }

        InitializeVariables();
        InitializeCameraEffects();
    }

    void Update()
    {
        if (!base.IsOwner) return; // Prevent input handling for non-local players
        CheckGroundedStatus();
        HandleCoyoteTime();
        DebugGravityStatus();
        DebugMovementStatus();
        CheckLanding();
        HandleNearZeroVelocity();
        HandleInput();
        SpeedControl();
        UpdateMovementState();
        CheckForDoubleJumpBoots();
        CheckIfBlocking();
        UpdateDrag();
    }

    void FixedUpdate()
    {
        if (!base.IsOwner) return; // Prevent input handling for non-local players
        MovePlayer();
        ApplySlopeDownforce();
    }
    #endregion

    #region Initialization
    private void InitializeVariables()
    {
        originalWalkSpeed = walkSpeed;
        originalSprintSpeed = sprintSpeed;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        readyToSlide = true;
        cf = GetComponent<ConstantForce>();
        
        // If modelTransform isn't assigned, use this transform
        if (modelTransform == null)
            modelTransform = transform;
            
        // Store original values
        startYScale = modelTransform.localScale.y;
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;

        t = Time.deltaTime * fovTransitionSpeed;
        
        // Force to standing position
        isCrouching = false;
        isSliding = false;
        ApplyStand();
    }

    private void InitializeCameraEffects()
    {
        cameraEffects = cam.GetComponent<CameraEffects>();
        if (cameraEffects == null)
            cameraEffects = cam.gameObject.AddComponent<CameraEffects>();

        cameraEffects.jumpLookAmount = jumpLookAmount;
        cameraEffects.landLookAmount = landLookAmount;
        cameraEffects.cameraJumpDuration = cameraJumpDuration;
        cameraEffects.cameraLandDuration = cameraLandDuration;
        cameraEffects.effectSmoothness = jumpSmoothness;
        cameraEffects.maxTiltAngle = maxTiltAngle;
    }
    #endregion

    #region Status Checks
    private void CheckGroundedStatus()
    {
        // Use ground detection
        float rayLength = (playerHeight * 0.3f);
        UnityEngine.Debug.DrawRay(capsuleTransform.position, Vector3.down * rayLength, Color.red);
        grounded = Physics.Raycast(capsuleTransform.position, Vector3.down, playerHeight * 0.35f, whatIsGround);
        //UnityEngine.Debug.Log($"Grounded Status: {grounded}");

        // IMPORTANT FIX: If sliding and not grounded, ensure gravity is applied
        if (isSliding && !grounded)
        {
            ApplyAirGravity();
        }
    }

    private void HandleCoyoteTime()
    {
        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void DebugGravityStatus()
    {
        bool isUsingGravity = rb.useGravity;
        if (isUsingGravity != wasUsingGravity)
        {
            UnityEngine.Debug.Log("Gravity status changed: " + (isUsingGravity ? "GRAVITY ON" : "GRAVITY OFF"));
            wasUsingGravity = isUsingGravity;
        }
    }

    private void DebugMovementStatus()
    {
        // Debug info about constant force
        if (cf.force != lastCFForce)
        {
            UnityEngine.Debug.Log("Constant Force changed: " + cf.force.ToString("F2"));
            lastCFForce = cf.force;
        }

        // Debug info about movement
        bool isMoving = (Mathf.Abs(horiziontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f);
        //UnityEngine.Debug.Log("Movement status: " + (isMoving ? "MOVING" : "NOT MOVING"));
    }

    private void CheckLanding()
    {
        if (!wasGrounded && grounded)
        {
            ApplyLandCamera();
        }
        wasGrounded = grounded;
    }

    private void HandleNearZeroVelocity()
    {
        // Apply extra gravity when velocity is near zero to prevent floating
        if (rb.velocity.y < 0.1f && rb.velocity.y > -0.1f)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * 1.5f, ForceMode.Acceleration);
        }
    }

    private void UpdateDrag()
    {
        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
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
            
           // UnityEngine.Debug.Log($"Slope Detection - Angle: {angle}, Walkable: {isWalkableSlope}");
            
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

    private void CheckForDoubleJumpBoots()
    {
        canDoubleJump = false;
        foreach (Transform child in weaponHolder)
        {
            if (child.CompareTag("DoubleJump"))
            {
                canDoubleJump = true;
                break;
            }
        }
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        horiziontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        HandleJumpInput();
        HandleCrouchAndSlideInput();
        HandleSlideExit();
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(jumpKey))
        {
            UnityEngine.Debug.Log($"Trying to jump and grounded: {grounded} and readyToJump: {readyToJump}");
            if ((grounded || coyoteTimeCounter > 0) && readyToJump)
            {
                UnityEngine.Debug.Log("Jumping Key");
                readyToJump = false;
                hasDoubleJumped = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
                
                // Reset coyote time when jumping
                coyoteTimeCounter = 0;
            }
            else if (canDoubleJump && !hasDoubleJumped && !grounded)
            {
                hasDoubleJumped = true;
                StartCoroutine(DoubleJumpWithDelay());
            }
        }
    }

    private void HandleCrouchAndSlideInput()
    {
        // Check if sprint + crouch for slide, else just crouch
        bool isMovingFast = moveDirection.magnitude > 0.5f && Input.GetKey(sprintKey);
        
        if (Input.GetKeyDown(crouchKey) && grounded && isMovingFast && readyToSlide && !isSliding && !isCrouching)
        {
            // Start sliding
            StartSlide();
        }
        else if (Input.GetKey(crouchKey) && !isSliding && grounded)
        {
            // Hold crouch key to stay crouched
            if (!isCrouching)
            {
                isCrouching = true;
                ApplyCrouch();
            }
        }
        else if (isCrouching && !isSliding)
        {
            // Released crouch key, stand up if possible
            isCrouching = false;
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

    private void HandleSlideExit()
    {
        // Check if we're on a slope
        bool onSlope = OnSlope();
        
        // Update slope sliding status
        isSlidingOnSlope = isSliding && onSlope;
        
        // Exit slide if key is released and not on slope, or if slide timer is over and not on slope
        if (isSliding && ((Input.GetKeyUp(crouchKey) && !onSlope) || (slideTimer <= 0 && !onSlope)))
        {
            StopSlide();
        }
        // Continue slide on slopes if feature is enabled
        else if (isSliding && slideTimer <= 0 && onSlope && continueSlidingOnSlopes)
        {
            // Refresh slide direction to follow the slope
            AdjustSlideDirectionForSlope();
        }
    }
    #endregion

    #region Movement State
    private void UpdateMovementState()
    {
        // Update slide timer in StateHandler
        if (isSliding)
        {
            UpdateSlideState();
        }
        else if (isCrouching)
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

    private void UpdateSlideState()
    {
        state = MovementState.sliding;
        
        // Check if we're on a slope
        bool onSlope = OnSlope();
        isSlidingOnSlope = isSliding && onSlope;
        
        // Update slide timer only if not on a slope
        if (!onSlope)
        {
            slideTimer -= Time.deltaTime;
        }
        
        // Set movement speed
        if (isSlidingOnSlope)
        {
            // Speed is handled in HandleGroundedSlideMovement for slopes
        }
        else
        {
            // Normal slide speed
            moveSpeed = sprintSpeed * 1.2f;
            
            // Gradual slowdown over time for flat ground
            moveSpeed *= Mathf.Lerp(1.0f, 0.5f, 1 - (slideTimer / slideDuration));
        }
    }
    #endregion

    #region Movement Physics
    private void MovePlayer()
    {
        // Calculate move direction (unless sliding)
        if (!isSliding)
        {
            CalculateMoveDirection();
            ApplyNormalMovement();
        }
        else
        {
            ApplySlideMovement();
        }
    }

    private void CalculateMoveDirection()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horiziontalInput;
    }

    private void ApplyNormalMovement()
    {
        // Simple fix for downhill bounce - check if we're on a slope and moving downhill
        HandleDownhillMovement();
    
        // Normal movement physics
        rb.useGravity = !grounded;
        
        if (!grounded)
        {
            ApplyAirGravity();
        }
        else
        {
            cf.force = Vector3.zero;
        }

        // Normal ground or air movement
        float currentMoveSpeed = grounded ? moveSpeed : moveSpeed * airMultiplier;
        rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f, ForceMode.Force);
    }

    private void ApplyAirGravity(float? airGravity = null)
    {
        rb.useGravity = true;
        float gravityToUse = airGravity ?? gravity;
        cf.force = new Vector3(0, gravityToUse, 0);
        UnityEngine.Debug.Log("Applying air gravity: " + gravityToUse);
    }

    private void HandleDownhillMovement()
    {
        if (grounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, playerHeight * 0.5f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > 5f) // Only on actual slopes, not tiny bumps
            {
                // Check if we're moving downhill
                Vector3 forward = orientation.forward;
                if (Vector3.Dot(forward, Vector3.ProjectOnPlane(Vector3.down, hit.normal)) > 0)
                {
                    // Going downhill - add strong downforce and zero out any upward velocity
                    rb.AddForce(Vector3.down * 50f, ForceMode.Force);
                    
                    // Prevent upward velocity entirely
                    if (rb.velocity.y > 0)
                    {
                        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                    }
                }
            }
        }
    }

    private void ApplySlideMovement()
    {
        // IMPORTANT FIX: Apply gravity when sliding in air
        if (!grounded)
        {
            ApplyAirGravity(-25);
            
            // Limit upward velocity when in air to prevent extreme launches
            if (rb.velocity.y > 10f)
            {
                rb.velocity = new Vector3(rb.velocity.x, 10f, rb.velocity.z);
            }
        }
        else 
        {
            // On ground sliding behavior
            HandleGroundedSlideMovement();
        }
        
        // Apply force in slide direction
        rb.AddForce(slideDirection * moveSpeed * 10f, ForceMode.Force);
    }

    private void HandleGroundedSlideMovement()
    {
        // Check if we need to update direction for slope
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, playerHeight * 0.5f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > 5f) // If we're on a slope
            {
                // Keep adjusting direction to follow the slope
                slideDirection = Vector3.ProjectOnPlane(slideDirection, hit.normal).normalized;
                
                // Calculate downhill direction - the normalized projection of gravity onto the slope
                Vector3 downhillDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                
                // Blend current direction with downhill direction (bias toward downhill)
                slideDirection = Vector3.Lerp(slideDirection, downhillDirection, 0.3f).normalized;
                
                // Add strong downforce to keep on slope - more force when going uphill
                float downforceMagnitude = 60f;
                rb.AddForce(Vector3.down * downforceMagnitude, ForceMode.Force);
                
                // Build momentum when sliding downhill
                if (Vector3.Dot(slideDirection, downhillDirection) > 0.5f)
                {
                    // Add acceleration in the downhill direction
                    rb.AddForce(downhillDirection * slopeSlideAcceleration, ForceMode.Acceleration);
                    
                    // Increase speed limit based on steepness
                    moveSpeed = Mathf.Lerp(sprintSpeed * 1.2f, maxSlopeSlideSpeed, angle / maxSlopeAngle);
                    
                    UnityEngine.Debug.Log($"Downhill slide, speed: {moveSpeed}, angle: {angle}");
                }
                // Going uphill - prevent launches
                else if (Vector3.Dot(slideDirection, downhillDirection) < -0.2f)
                {
                    // Cancel upward velocity to prevent launching
                    if (rb.velocity.y > 0)
                    {
                        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    }
                    
                    // Add extra downforce when going uphill to prevent launch
                    rb.AddForce(Vector3.down * 400f, ForceMode.Force);
                    
                    UnityEngine.Debug.Log("Uphill slide - applying launch prevention");
                }
            }
        }
    }

    private void ApplySlopeDownforce()
    {
        // Only apply when grounded
        if (!grounded)
            return;
        
        ApplyDownforceOnSlope();
        ApplySprintDownforceOnSlope();
    }

    private void ApplyDownforceOnSlope()
    {
        // Check if we're on a slope
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, playerHeight * 0.5f + 0.3f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            
            // Only apply on actual slopes (not flat ground)
            if (angle > 5f && angle < maxSlopeAngle)
            {
                // Get movement direction relative to slope
                Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, hit.normal).normalized;
                
                // Check if we're moving downhill
                if (Vector3.Dot(slopeDirection, Vector3.down) > 0.1f)
                {
                    // Apply downforce ONLY when moving downhill, not uphill
                    float downforceMagnitude = 50f;
                    
                    // Only cancel upward velocity, don't add downforce when jump is pressed
                    if (rb.velocity.y > 0 && !Input.GetKey(jumpKey))
                    {
                        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    }
                    else if (!Input.GetKey(jumpKey))
                    {
                        // Only apply downforce if not trying to jump
                        rb.AddForce(Vector3.down * downforceMagnitude, ForceMode.Force);
                    }
                }
            }
        }
    }

    private void ApplySprintDownforceOnSlope()
    {
        // For sprinting, ONLY apply downforce when moving downhill, not uphill
        if (grounded && state == MovementState.sprinting)
        {
            // Check direction relative to slope
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, playerHeight * 0.5f, whatIsGround))
            {
                Vector3 moveDir = orientation.forward * verticalInput + orientation.right * horiziontalInput;
                moveDir = moveDir.normalized;
                
                // Project move direction onto slope
                Vector3 slopeDir = Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
                
                // If we're going downhill and not trying to jump
                if (slopeDir.y < 0 && !Input.GetKey(jumpKey))
                {
                    // Add downforce and cancel upward velocity
                    if (rb.velocity.y > 0)
                    {
                        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    }
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
                }
            }
        }
    }

    private void SpeedControl()
    {
        if (isSliding)
        {
            ControlSlideSpeed();
            return;
        }
        
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

    private void ControlSlideSpeed()
    {
        // Calculate max speed - either from slope slide or normal slide
        float slideMaxSpeed = isSlidingOnSlope ? moveSpeed : moveSpeed * 1.2f;
        
        if (OnSlope())
        {
            // Adjust max speed based on uphill/downhill
            Vector3 slopeDir = Vector3.ProjectOnPlane(slideDirection, slopeHit.normal).normalized;
            Vector3 downhillDir = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
            float downhillAlignment = Vector3.Dot(slopeDir, downhillDir);
            
            if (downhillAlignment < 0)
            {
                // Going uphill, reduce max speed
                slideMaxSpeed *= 0.7f;
            }
            else if (downhillAlignment > 0.7f)
            {
                // Going directly downhill, allow higher speed
                slideMaxSpeed = moveSpeed;
            }
        }
        
        // Apply speed limit
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (horizontalVelocity.magnitude > slideMaxSpeed)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * slideMaxSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }
    #endregion

    #region Jump Functions
    private void Jump()
    {
        exitingSlope = true;
        cameraEffects.TriggerJumpEffect();

        // Use a simple vertical jump with some forward direction
        Vector3 jumpDirection = Vector3.up;
        
        // Add a bit of your look direction to the jump
        if (moveDirection.magnitude > 0.1f)
        {
            // If you're moving, add some of that direction to the jump
            Vector3 horizontalDir = new Vector3(moveDirection.x, 0f, moveDirection.z).normalized;
            jumpDirection = (jumpDirection + horizontalDir * 0.3f).normalized;
        }

        // Reset vertical velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        
        // Apply the jump force
        rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
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
    #endregion

    #region Crouch and Slide Functions
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

    private void StartSlide()
    {
        isSliding = true;
        readyToSlide = false;
        slideTimer = slideDuration;
        
        // Apply slide height
        modelTransform.localScale = new Vector3(
            modelTransform.localScale.x,
            startYScale * slideYScale,
            modelTransform.localScale.z
        );
        
        // Adjust the collider height
        playerCollider.height = originalColliderHeight * slideYScale;
        float centerYOffset = (originalColliderHeight - playerCollider.height) / 2f;
        playerCollider.center = new Vector3(
            originalColliderCenter.x,
            originalColliderCenter.y - centerYOffset,
            originalColliderCenter.z
        );
        
        // Get direction from input
        slideDirection = orientation.forward * verticalInput + orientation.right * horiziontalInput;
        slideDirection = new Vector3(slideDirection.x, 0, slideDirection.z).normalized;
        
        // Handle slope slide direction
        AdjustSlideDirectionForSlope();
        
        // Apply initial force
        rb.AddForce(slideDirection * slideForce, ForceMode.Impulse);
    }

    private void AdjustSlideDirectionForSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, playerHeight * 0.5f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > 5f) // If we're on a slope
            {
                // Use the slope normal to determine slide direction
                // This makes you slide DOWN the slope instead of horizontally
                slideDirection = Vector3.ProjectOnPlane(slideDirection, hit.normal).normalized;
            }
        }
    }

    private void StopSlide()
    {
        isSliding = false;
        
        // If we're in the air, ensure gravity is applied
        if (!grounded)
        {
            ApplyAirGravity(-20);
            state = MovementState.air;
        }
        // If we're still holding crouch key, go to crouch state
        else if (Input.GetKey(crouchKey))
        {
            isCrouching = true;
        }
        // Otherwise stand up
        else if (!Physics.Raycast(transform.position, Vector3.up, 2f, whatIsGround))
        {
            ApplyStand();
        }
        
        // Start cooldown
        Invoke(nameof(ResetSlide), slideCooldown);
    }

    private void ResetSlide()
    {
        readyToSlide = true;
    }
    #endregion
}