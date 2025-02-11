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

    public float groundDrag;
    [SerializeField] private Transform capsuleTransform;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public float fallMultiplier = 2.5f;
    public float gravity = -15f;
    public ConstantForce cf;


    public float t = 1f;


    bool readyToJump;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;
    public Camera cam;

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

        if (rb.linearVelocity.y < 0.1f && rb.linearVelocity.y > -0.1f)  // Near peak of jump
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * 1.5f, ForceMode.Acceleration);
        }


        MyInput();
        SpeedControl();
        StateHandler();

        //Handle Drag
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horiziontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        UnityEngine.Debug.Log($"ReadyToJump: {readyToJump}, grounded: {grounded}");

        if (Input.GetKey(jumpKey)&& readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
           
            Invoke(nameof(ResetJump), jumpCooldown);
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
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }
}

