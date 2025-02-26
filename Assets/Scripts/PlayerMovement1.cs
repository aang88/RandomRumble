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


    public float groundDrag;
    [SerializeField] private Transform capsuleTransform;

    public float jumpForce;
    public float jumpCooldown;
    private bool hasDoubleJumped = false;


    public float airMultiplier;
    public float fallMultiplier = 2.5f;
    public float gravity = -15f;
    public ConstantForce cf;

    public Transform weaponHolder;

    public float t = 1f;

    //Need this so it doesn't apply speed reduction over and over lol
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

    public MovementState state;

    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }


    // Start is called before the first frame update
    void Start()
    {
        //Store these in case of speed modification
        originalWalkSpeed = walkSpeed;
        originalSprintSpeed = sprintSpeed;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        cf = GetComponent<ConstantForce>();
        startYScale = transform.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        float rayLength = (playerHeight*0.3f);  
       
        UnityEngine.Debug.DrawRay(capsuleTransform.position, Vector3.down * rayLength, Color.red);
        //Check if Grounded 
        grounded = Physics.Raycast(capsuleTransform.position, Vector3.down, playerHeight * 0.3f, whatIsGround);

        if (rb.velocity.y < 0.1f && rb.velocity.y > -0.1f)  // Near peak of jump
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * 1.5f, ForceMode.Acceleration);
        }


        MyInput();
        SpeedControl();
        StateHandler();
        CheckForDoubleJumpBoots();
        CheckIfBlocking();

    

        //Handle Drag
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

        // Only modify speeds when blocking state changes
        if (isBlocking && !wasBlocking)
        {
            // First time entering blocking state
            walkSpeed = originalWalkSpeed * 0.5f;
            sprintSpeed = originalSprintSpeed * 0.5f;
        }
        else if (!isBlocking && wasBlocking)
        {
            // Just stopped blocking
            walkSpeed = originalWalkSpeed;
            sprintSpeed = originalSprintSpeed;
        }

        wasBlocking = isBlocking; // Update the previous state
    }

    private void MyInput()
    {
        horiziontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        UnityEngine.Debug.Log($"ReadyToJump: {readyToJump}, grounded: {grounded}");

        if (Input.GetKeyDown(jumpKey))
        {
            // Normal jump when grounded
            if (grounded && readyToJump)
            {
                readyToJump = false;
                hasDoubleJumped = false;  // Reset double jump when doing normal jump
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            // Double jump when in air and we have the boots
            else if (canDoubleJump && !hasDoubleJumped && !grounded)
            {
                hasDoubleJumped = true;
                StartCoroutine(DoubleJumpWithDelay());
            }
        }

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z); ;
            transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        }

        //Stop Crouch

        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        }

    }

    private void StateHandler()
    {
        //Spiriting
        if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 88, t);
        }

        //Crouching
        if(Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        //Walking
        else if(grounded)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 80, t);
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        //Air
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

        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    void CheckForDoubleJumpBoots()
    {
        // Look for child objects with the "DoubleJumpBoots" tag
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
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * doubleJumpForce, ForceMode.Impulse);
    }
}

