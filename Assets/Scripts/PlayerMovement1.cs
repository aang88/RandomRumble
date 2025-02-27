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
    public float crouchYScale;
    private float startYScale;

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
        startYScale = transform.localScale.y;

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

    void Update()
    {
        float rayLength = (playerHeight * 0.3f);

        UnityEngine.Debug.DrawRay(capsuleTransform.position, Vector3.down * rayLength, Color.red);
        grounded = Physics.Raycast(capsuleTransform.position, Vector3.down, playerHeight * 0.3f, whatIsGround);

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
        UnityEngine.Debug.Log($"ReadyToJump: {readyToJump}, grounded: {grounded}");

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

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        }

        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        }
    }

    private void StateHandler()
    {
        if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 88, t);
        }
        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
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
        moveDirection = orientation.forward * verticalInput + orientation.right * horiziontalInput;
        if (grounded)
        {
            cf.force = new Vector3(0, 0, 0);
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
            cf.force = new Vector3(0, gravity, 0);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        cameraEffects.TriggerJumpEffect();

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
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

    void LateUpdate()
    {
        // Empty - camera effects are now handled by FPSCameraEffects
    }
}