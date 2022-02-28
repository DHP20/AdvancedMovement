using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    protected Rigidbody rb;
    protected CapsuleCollider cc;
    protected RaycastHit hit;

    [SerializeField]
    protected float speed, groundMaxSpeed, airStrafeForce, airDrag = 0.3f, airMaxSpeed, counterDrag, jumpForce, sprintSpeed;//stamina,
    protected float originalDrag, jumpTime;//currentStamina,

    protected bool grounded, sprinting, sprintCD;

    Vector2 inputs;
    Vector3 moveDir;
    protected Vector3 prevMov;

    protected Player player;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        player = Player.player;
    }

    protected void Start()
    {
        originalDrag = rb.drag;
        //currentStamina = stamina;

        Application.targetFrameRate = 60;

        InputManager.inputManager.p_actions.Move.performed += ctx => ReadMovementInput();
        InputManager.inputManager.p_actions.Move.canceled += ctx => ReadMovementInput();
        InputManager.inputManager.p_actions.Jump.started += ctx => Jump();
        InputManager.inputManager.p_actions.Sprint.started += ctx => sprinting = true;
        InputManager.inputManager.p_actions.Sprint.canceled += ctx => sprinting = false;
    }

    protected void FixedUpdate()
    {
        grounded = Physics.SphereCast(transform.position, cc.radius * 0.9f, Vector3.down, out hit, cc.height / 2.75f);
        moveDir = transform.forward * inputs.y + transform.right * inputs.x;
        rb.useGravity = !grounded;

        if(grounded)
            MovementGround();

        else
        {
            rb.drag = airDrag;
            MovementAir();
        }

        //Debug.Log(rb.velocity.magnitude);
    }

    //protected void LateUpdate()
    //{
    //    prevMov = rb.velocity;
    //}

    protected void ReadMovementInput()
    {
        inputs = InputManager.inputManager.p_actions.Move.ReadValue<Vector2>();
    }

    protected virtual void MovementGround()
    {
        //Vector2 inputs = InputManager.inputManager.movementInput;
        //Vector3 moveDir = transform.forward * inputs.y + transform.right * inputs.x;
        //moveDir = transform.forward * inputs.y + transform.right * inputs.x;

        moveDir = moveDir * HandleSprint() + transform.up * rb.velocity.y;

        RaycastHit hit2;
        Physics.Raycast(transform.position, Vector3.down, out hit2);

        rb.AddForce(Vector3.ProjectOnPlane(moveDir, hit2.normal), ForceMode.Force);

        if (rb.velocity.magnitude < groundMaxSpeed)
            Vector3.ClampMagnitude(rb.velocity, groundMaxSpeed);

        //if (prevMov.magnitude > rb.velocity.magnitude)
        //    rb.drag = counterDrag;

        if (inputs == Vector2.zero)
            rb.drag = counterDrag;

        else
            rb.drag = originalDrag;

        //Debug.Log(rb.drag);

        //rb.velocity = Vector3.Lerp(rb.velocity, Vector3.ProjectOnPlane(moveDir, hit2.normal), Time.deltaTime * 15);
    }

    protected void MovementAir()
    {
        Vector3 projVel = Vector3.Project(rb.velocity, moveDir);

        bool goingAway = Vector3.Dot(moveDir, projVel.normalized) <= 0f;

        if (projVel.magnitude < airMaxSpeed || goingAway)
        {
            Vector3 targetForce = moveDir.normalized * airStrafeForce;

            if (!goingAway)
                targetForce = Vector3.ClampMagnitude(targetForce, airMaxSpeed - projVel.magnitude);

            else
                targetForce = Vector3.ClampMagnitude(targetForce, airMaxSpeed + projVel.magnitude);

            rb.AddForce(targetForce, ForceMode.VelocityChange);

            Debug.Log(targetForce + "  " + goingAway + "  " + projVel);
        }
    }

    protected void Jump()
    {
        if (!grounded)
            return;

        rb.drag = 0;

        rb.AddForce(jumpForce * rb.mass * Vector3.up, ForceMode.Impulse);
    }

    protected float HandleSprint()
    {
        if (InputManager.inputManager.movementInput.y <= 0)
            return speed;

        //if (sprintCD)
        //    return speed;

        //if (currentStamina <= 0)
        //    StartCoroutine(SprintCD());

        if (sprinting)// && currentStamina > 0
        {
            //currentStamina -= Time.fixedDeltaTime * 10;
            return sprintSpeed;
        }

        else
        {
            //if (currentStamina < stamina)
            //    currentStamina += Time.fixedDeltaTime * 12;

            return speed;
        }
    }

    protected IEnumerator SprintCD()
    {
        sprintCD = true;

        yield return new WaitForSeconds(1);

        sprintCD = false;
    }
}