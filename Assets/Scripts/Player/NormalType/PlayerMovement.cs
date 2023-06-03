using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;
    protected CapsuleCollider cc;
    protected RaycastHit hit, wallRunHit;

    [SerializeField]
    protected float speed, groundMaxSpeed, airStrafeForce, airDrag = 0.3f, crouchedSpeed, crouchedDrag = 0.6f, crouchedMaxSpeed = 1, wallDrag = 0.8f, wallForce, wallRunCD, wallrunExitAngle = 40;
    [SerializeField]
    protected float slideBoost, airMaxSpeed, counterDrag, jumpForce, sprintSpeed, lerpMod, maxExtraFOV = 30, maxSway = 15, swayMod = 3;//stamina,
    protected float originalDrag, jumpTime, originalHeight, normalFOV, wallrunYRotExit;//currentStamina,
    public float currentSway = 0;

    protected bool grounded, wallFloorCheck, sprinting, sprintCD, crouched, wasOnAir, slideOnCD, swayDir, resetSway, doubleJump, wasOnWall, wallRunOnCD;
    public bool wallRunning;

    Vector2 inputs;
    Vector3 moveDir;
    Vector3 wallRideNormal;
    Vector3 wallVector;
    protected Vector3 prevMov;

    protected Player player;

    protected Camera cm;
    protected Transform cmTransform;

    protected GameObject lastWall;

    IEnumerator wrCD;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
    }

    protected void Start()
    {
        player = Player.player;
        originalDrag = rb.drag;

        Application.targetFrameRate = 244;

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

        if (wallRunning)
        {
            WallRiding();
            return;
        }

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

        wasOnAir = !grounded;

        if (!crouched)
        {
            grounded = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, cc.height / 2.4f);
            wallFloorCheck = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, cc.height);

            if (!wallFloorCheck)
            {
                Vector3 wallDetDir = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized;

                //if (Physics.SphereCast(transform.position, cc.radius * 0.8f, transform.right, out wallRunHit, cc.radius * 1.3f) || Physics.SphereCast(transform.position, cc.radius * 0.8f, -transform.right, out wallRunHit, cc.radius * 1.3f))
                if (Physics.SphereCast(transform.position, cc.radius * 0.8f, wallDetDir, out wallRunHit, cc.radius * 1.3f))
                {
                    if (wallRunHit.transform.gameObject.layer == 8)
                    {
                        EnteredWallRide();
                        return;
                    }
                }
            }
        }

        else
            grounded = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, cc.height / 3f);
    }

    protected void ReadMovementInput()
    {
        inputs = InputManager.inputManager.p_actions.Move.ReadValue<Vector2>();
    }

    protected virtual void MovementGround()
    {
        float targetDrag, currentMaxSpeed;

        if (wasOnAir)
            doubleJump = true;

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
            TouchedGround();
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

    protected void TouchedGround()
    {
        float forwardSpeed = new Vector3(0, 0, rb.velocity.z).magnitude;

        if (forwardSpeed > groundMaxSpeed / 4f)
        {
            rb.drag = originalDrag;
        }

        if (wallRunning)
            ExitWallRide();

        if (wrCD != null)
        {
            StopCoroutine(wrCD);
            wallRunOnCD = false;
        }
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

        cm.fieldOfView = Mathf.Lerp(cm.fieldOfView, normalFOV + maxExtraFOV * mod, Time.deltaTime * 3);
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
        if (wallRunning)
        {
            ExitWallRide();
            rb.velocity = rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(jumpForce * wallRunHit.normal + Vector3.up * jumpForce * 0.75f, ForceMode.Impulse);
            return;
        }

        if (!grounded && doubleJump)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

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
        if (wallRunning)
        {
            ExitWallRide();
            return;
        }

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
        if ((lastWall == wallRunHit.transform.gameObject && wallRunOnCD))//|| wallRunHit.transform.gameObject.layer != 8)
            return;

        Debug.Log(lastWall + "  " + wallRunOnCD);

        lastWall = wallRunHit.transform.gameObject;

        Debug.Log(lastWall);

        doubleJump = true;
        wallRunning = true;
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.drag = wallDrag;
        rb.useGravity = false;
    }

    void WallRiding()
    {
        wallRideNormal = wallRunHit.normal;

        if (Vector3.Angle(Vector3.Project(transform.forward, Vector3.Cross(wallRideNormal, transform.up)), Vector3.Cross(wallRideNormal, transform.up)) < 90)
            wallVector = Vector3.Cross(wallRideNormal, transform.up);

        else
            wallVector = -Vector3.Cross(wallRideNormal, transform.up);

        Debug.DrawRay(wallRunHit.point, wallVector, Color.green, Time.fixedDeltaTime);
        Debug.DrawRay(wallRunHit.point, Vector3.Cross(wallRideNormal, transform.up), Color.white, Time.fixedDeltaTime);
        Debug.DrawRay(transform.position, -wallRideNormal, Color.red, Time.fixedDeltaTime);

        rb.AddForce(wallVector * wallForce * inputs.y + -wallRideNormal * 50);

        Debug.Log(!Physics.SphereCast(transform.position, cc.radius * 0.8f, -wallRideNormal, out wallRunHit, cc.radius));

        //if ((!Physics.SphereCast(transform.position, cc.radius * 0.5f, -wallRideNormal, out wallRunHit, cc.radius * 1.5f) && !Physics.Raycast(transform.position, -wallRideNormal, out wallRunHit, cc.radius * 1.5f)) || wallRunHit.transform.gameObject.layer != 8)
        
        //if(Physics.SphereCast(transform.position, cc.radius * 0.5f, -moveDir, out wallRunHit, cc.radius * 1.5f))
        float angle = Vector3.Angle(wallRideNormal, moveDir);
        Debug.Log(angle);

        if (angle < wallrunExitAngle && angle != 0)
        {
            ExitWallRide();
        }
    }

    void ExitWallRide()
    {
        Debug.Log("exit");

        wallRunning = false;
        rb.drag = airDrag;
        rb.useGravity = true;

        if (wrCD != null)
            StopCoroutine(wrCD);

        StartCoroutine(wrCD = WallRunCD());
    }

    public void ResetState()
    {
        rb.velocity = Vector3.zero;
        CrouchToggle(false);
        ExitWallRide();
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

    protected IEnumerator WallRunCD()
    {
        wallRunOnCD = true;

        yield return new WaitForSeconds(wallRunCD);

        wallRunOnCD = false;
    }
}