using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;
    protected CapsuleCollider cc;
    protected RaycastHit hit, wallRunHit;

    [SerializeField]
    protected float speed, groundMaxSpeed, airStrafeForce, airDrag = 0.3f, crouchedSpeed, crouchedDrag = 0.6f, crouchedMaxSpeed = 1, wallDrag = 0.8f, wallForce;
    [SerializeField]
    protected float slideBoost, airMaxSpeed, counterDrag, jumpForce, sprintSpeed, lerpMod, maxExtraFOV = 30, maxSway = 15, swayMod = 3;//stamina,
    protected float originalDrag, jumpTime, originalHeight, normalFOV;//currentStamina,
    public float currentSway = 0;

    protected bool grounded, sprinting, sprintCD, crouched, wasOnAir, slideOnCD, swayDir, resetSway, doubleJump, wasOnWall;
    public bool wallRiding;

    Vector2 inputs;
    Vector3 moveDir;
    Vector3 wallRideNormal;
    Vector3 wallVector;
    protected Vector3 prevMov;

    protected Player player;

    protected Camera cm;
    protected Transform cmTransform;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
    }

    protected void Start()
    {
        player = Player.player;
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

        cm = player.playerCamera.GetComponent<Camera>();
        cmTransform = cm.transform;

        normalFOV = cm.fieldOfView;
    }

    protected void FixedUpdate()
    {
        moveDir = transform.forward * inputs.y + transform.right * inputs.x;

        if (wallRiding)
        {
            WallRiding();
            return;
        }

        if (!crouched)
        {
            grounded = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, cc.height / 2.4f);

            if (!grounded)
                wallRiding = Physics.SphereCast(transform.position, cc.radius * 0.7f, transform.right, out wallRunHit, cc.radius) || Physics.SphereCast(transform.position, cc.radius * 0.7f, -transform.right, out wallRunHit, cc.radius);

            if(wasOnWall != wallRiding)
            {
                EnteredWallRide();
                return;
            }
        }

        else
            grounded = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, cc.height / 3f);

        if(grounded)
            MovementGround();

        else
        {
            rb.drag = airDrag;
            resetSway = true;
            MovementAir();
        }

        HandleFOV();
        //CameraSway();

        //Debug.Log(rb.velocity.magnitude);

        wasOnAir = !grounded;
        wasOnWall = wallRiding;
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

            resetSway = true;

            if (wasOnAir)
                Slide();

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

        if (wasOnAir)
            doubleJump = true;

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

    protected void HandleFOV()
    {
        Vector3 locVel = transform.InverseTransformDirection(rb.velocity);

        if (locVel.z < 0)
            locVel.z = 0;

        float forwardMag = new Vector3(0, 0, locVel.z).magnitude;

        float mod = (forwardMag / maxExtraFOV);

        if (mod > 1)
            mod = 1;

        cm.fieldOfView = normalFOV + maxExtraFOV * mod;
    }

    protected virtual void CameraSway()
    {
        if (inputs.Equals(Vector2.zero) || !grounded)
            resetSway = true;

        else if (!crouched)
            resetSway = false;

        if (resetSway)
        {
            currentSway = Mathf.Lerp(currentSway, 0, Time.fixedDeltaTime * swayMod);
            return;
        }

        if (!swayDir)
        {
            if (currentSway < maxSway)
                currentSway += Time.fixedDeltaTime * swayMod;

            else
                swayDir = true;
        }

        else
        {
            if (currentSway > -maxSway)
                currentSway -= Time.fixedDeltaTime * swayMod;

            else
                swayDir = false;
        }

        Debug.Log(currentSway + " " + swayDir + " " + resetSway);
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
        if (wallRiding)
        {
            rb.AddForce(jumpForce * wallRunHit.normal, ForceMode.Impulse);
            return;
        }

        if (!grounded && doubleJump)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

            if (!moveDir.Equals(Vector3.zero))
                rb.velocity = moveDir * jumpForce / 1.5f;

            rb.AddForce(jumpForce * Vector3.up, ForceMode.Impulse);
            doubleJump = false;
        }

        else if (!grounded)
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

    void EnteredWallRide()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.drag = wallDrag;
        rb.useGravity = false;
        wallRideNormal = wallRunHit.normal;

        if (Physics.SphereCast(transform.position, cc.radius * 0.7f, -transform.right, out wallRunHit, cc.radius))
            wallVector = Vector3.Cross(wallRideNormal, transform.up);

        else
            wallVector = Vector3.Cross(wallRideNormal, -transform.up);

        player.transform.rotation = Quaternion.Euler(wallVector);
        player.playerCamera.yRotation = Quaternion.ToEulerAngles(cm.transform.rotation).y;
    }

    void WallRiding()
    {
        wallRideNormal = wallRunHit.normal;
        wallRiding = Physics.SphereCast(transform.position, cc.radius * 0.7f, -wallRideNormal, out wallRunHit, cc.radius);

        Debug.DrawRay(wallRunHit.point, wallVector, Color.green, Time.fixedDeltaTime);
        //rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        //Debug.LogError("WallRiding");

        rb.AddForce(wallVector * wallForce * inputs.y);

        if (wasOnWall != wallRiding)
            ExitWallRide();
    }

    void ExitWallRide()
    {

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