using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;
    protected CapsuleCollider cc;
    protected RaycastHit hit;

    [SerializeField]
    protected float speed, groundMaxSpeed, airStrafeForce, airDrag = 0.3f, crouchedSpeed, crouchedDrag = 0.6f, crouchedMaxSpeed = 1, slideBoost, airMaxSpeed, counterDrag, jumpForce, sprintSpeed, lerpMod;//stamina,
    protected float originalDrag, jumpTime, originalHeight;//currentStamina,

    protected bool grounded, sprinting, sprintCD, crouched, wasOnAir, slideOnCD;

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

        Application.targetFrameRate = 90;

        InputManager.inputManager.p_actions.Move.performed += ctx => ReadMovementInput();
        InputManager.inputManager.p_actions.Move.canceled += ctx => ReadMovementInput();

        InputManager.inputManager.p_actions.Jump.started += ctx => Jump();

        InputManager.inputManager.p_actions.Sprint.started += ctx => sprinting = true;
        InputManager.inputManager.p_actions.Sprint.canceled += ctx => sprinting = false;

        InputManager.inputManager.p_actions.Crouch.started += ctx => CrouchToggle(true);
        InputManager.inputManager.p_actions.Crouch.canceled += ctx => CrouchToggle(false);

        originalHeight = cc.height;
    }

    protected void FixedUpdate()
    {
        grounded = Physics.SphereCast(transform.position, cc.radius * 0.9f, Vector3.down, out hit, cc.height / 2.75f);
        moveDir = transform.forward * inputs.y + transform.right * inputs.x;

        if(grounded)
            MovementGround();

        else
        {
            rb.drag = airDrag;
            MovementAir();
        }

        //Debug.Log(rb.velocity.magnitude);

        wasOnAir = !grounded;
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
        float targetDrag, currentMaxSpeed;

        if (crouched)
        {
            targetDrag = crouchedDrag;
            currentMaxSpeed = crouchedMaxSpeed;

            if (rb.velocity.magnitude > currentMaxSpeed * 1.5f)
            {
                rb.drag = rb.drag = Mathf.Lerp(rb.drag, targetDrag, Time.deltaTime * lerpMod);
                rb.useGravity = true;

                return;
            }
        }

        else if (wasOnAir)
        {
            float forwardSpeed = new Vector3(0, 0, rb.velocity.z).magnitude;

            if (forwardSpeed > groundMaxSpeed / 4f)
            {
                rb.drag = originalDrag;
            }
        }

        rb.useGravity = false;

        targetDrag = originalDrag;
        currentMaxSpeed = groundMaxSpeed;

        moveDir = moveDir * HandleSprint() + transform.up * rb.velocity.y;

        RaycastHit hit2;
        Physics.Raycast(transform.position, Vector3.down, out hit2);

        rb.AddForce(Vector3.ProjectOnPlane(moveDir, hit2.normal), ForceMode.Force);

        if (rb.velocity.magnitude < currentMaxSpeed)
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.ClampMagnitude(rb.velocity, currentMaxSpeed), Time.deltaTime);

        if (inputs == Vector2.zero)
            rb.drag = Mathf.Lerp(rb.drag, counterDrag, Time.deltaTime * lerpMod);

        else
            rb.drag = Mathf.Lerp(rb.drag, targetDrag, Time.deltaTime * lerpMod);
    }

    protected virtual void CameraSway()
    {

    }

    protected void MovementAir()
    {
        rb.useGravity = true;

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
        }
    }

    protected void Jump()
    {
        if (!grounded)
            return;

        rb.drag = 0;

        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(jumpForce * rb.mass * hit.normal, ForceMode.Impulse);
    }

    protected float HandleSprint()
    {
        if (InputManager.inputManager.movementInput.y <= 0)
            return speed;

        if (sprinting)
        {
            return sprintSpeed;
        }

        else
        {
            return speed;
        }
    }

    protected void CrouchToggle(bool toggle)
    {
        RaycastHit hitTemp;

        if (toggle && !crouched)
        {
            cc.height = originalHeight * 0.5f;
            transform.position -= transform.up * cc.height / 2;
            rb.drag = crouchedDrag;
            crouched = true;

            float zSpeed = new Vector3(0, 0, rb.velocity.z).magnitude;

            if (zSpeed > groundMaxSpeed / 1.5f)
                Slide();
        }

        else if(!Physics.SphereCast(transform.position, cc.radius, transform.up, out hitTemp, cc.height * 0.6f) && crouched)
        {
            transform.position += transform.up * cc.height / 2;
            cc.height = originalHeight;
            rb.drag = originalDrag;
            crouched = false;
        }
    }

    protected void Slide()
    {
        if (slideOnCD || !grounded)
            return;

        rb.AddForce(rb.velocity.normalized * slideBoost, ForceMode.Impulse);

        StartCoroutine(SlideCD());
    }

    protected IEnumerator SprintCD()
    {
        sprintCD = true;

        yield return new WaitForSeconds(1);

        sprintCD = false;
    }

    protected IEnumerator SlideCD()
    {
        Debug.Log("slide");

        slideOnCD = true;

        yield return new WaitForSeconds(2);

        slideOnCD = false;
    }
}