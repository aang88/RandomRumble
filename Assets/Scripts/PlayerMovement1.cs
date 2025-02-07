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

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public float fallMultiplier = 2.5f;

    public float t = 1f;


    bool readyToJump;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;
    public Camera cam;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    public Transform orientation;

    float horiziontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;

    public enum MovementState
    {
        walking,
        sprinting,
        air
    }


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
    }

    // Update is called once per frame
    void Update()
    {
        float rayLength = playerHeight + 0.2f;
        //Check if Grounded
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight, whatIsGround);

        if (rb.velocity.y < 0.1f && rb.velocity.y > -0.1f)  // Near peak of jump
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * 1.5f, ForceMode.Acceleration);
        }


        MyInput();
        SpeedControl();
        StateHandler();

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

    }

    private void StateHandler()
    {
        //Spiriting
        if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 85, t);
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
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
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
}

