using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector]
    public Rigidbody rb;
    [HideInInspector]
    public CapsuleCollider cc;
    RaycastHit hit, wallRunHit;
    RaycastHit hit2;
    RaycastHit climbHit;
    RaycastHit grappleHit;

    [SerializeField] [Header("Grounded parameters")]
    float speed = 4;
    [SerializeField]
    float groundMaxSpeed, sprintSpeed, counterDrag, crouchedSpeed, crouchedDrag = 8f, slideBoost = 3, slideMinSpeed = 1, slideDrag = 0.7f;

    [SerializeField] [Header("Air parameters")]
    float airStrafeForce = 0.16f;
    [SerializeField]
    float airDrag = 0.2f, airMaxSpeed;

    [SerializeField][Header("Wallrun parameters")]
    float wallrunDetRange = 1;
    [SerializeField]
    float wallDrag = 0.8f, wallForce, wallRunCD, wallrunExitAngle = 40, wallrunExitTime = 3, maxCmTilt = 15;

    [SerializeField][Header("Jump parameters")]
    float jumpForce;
    [SerializeField]
    float doubleJumpForce = 3, climbDetRange = 1f, climbSpeedMod = 2,  maxExtraFOV = 30, maxSway = 15, swayMod = 3;

    [SerializeField]
    [Header("Grapple Hook Parameters")]
    float hookSpeed = 20;
    [SerializeField]
    float hookReelSpeed = 5, hookSpring = 10, hookDamper = 18, hookMassScale = 18, hookCD = 5;

    float originalDrag, originalHeight, normalFOV, wallrunTimer, currentCmTilt;
    [HideInInspector]
    public float currentSway = 0;

    bool grounded, grappling, grappleThrown, grappleOnCD, wallFloorCheck, sprinting, sprintCD, crouched, sliding, wasOnAir, slideOnCD, swayDir, resetSway, jumpOnCD, doubleJump, wasOnWall, wallRunOnCD;
    [HideInInspector]
    public bool wallRunning, climbing;

    int wallTiltDir = 1;

    SpringJoint grappleJoint;

    Vector2 inputs;
    Vector3 moveDir;
    Vector3 horizontalVel;
    Vector3 wallRideNormal;
    Vector3 wallVector;

    Player player;

    Camera cm;
    Transform cmTransform;

    GameObject lastWall;
    GameObject grapple;

    InputManager inputM;
    Pools pools;

    IEnumerator wrCD;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        pools = Pools.instance;
    }

    private void Start()
    {
        player = Player.instance;
        originalDrag = rb.drag;

        inputM = InputManager.instance;

        inputM.p_actions.Jump.started += ctx => { Jump(); StartCoroutine(JumpCD()); };

        inputM.p_actions.Sprint.started += ctx => sprinting = true;
        inputM.p_actions.Sprint.canceled += ctx => sprinting = false;

        inputM.p_actions.Crouch.started += ctx => CrouchToggle(true);
        inputM.p_actions.Crouch.canceled += ctx => CrouchToggle(false);

        inputM.p_actions.Grapple.started += ctx => GrappleHook();

        originalHeight = cc.height;

        cm = player.playerCamera.GetComponent<Camera>();
        cmTransform = cm.transform;

        normalFOV = cm.fieldOfView;
        wallrunTimer = wallrunExitTime;
    }

    private void Update()
    {
        ReadMovementInput();
    }

    private void FixedUpdate()
    {
        moveDir = transform.forward * inputs.y + transform.right * inputs.x;
        horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        HandleFOV();
        //CameraSway();

        if (climbing)
            return;

        if (grappling)
        {
            MovementAir();
            Grappling();
            return;
        }

        ClimbDetection();
        HandleCmTilt();

        if (wallRunning)
        {
            WallRiding();
            return;
        }

        if(grounded)
            MovementGround();

        else
            MovementAir();

        wasOnAir = !grounded;

        if (!crouched)
        {
            grounded = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, originalHeight / 2.4f);
            wallFloorCheck = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, originalHeight / 1.75f);

            if (!wallFloorCheck)
            {
                Vector3 wallDetDirSpeed = horizontalVel.normalized;
                Vector3 wallDetDirInput = moveDir.normalized;

                //if (Physics.SphereCast(transform.position, cc.radius * 0.8f, transform.right, out wallRunHit, cc.radius * 1.3f) || Physics.SphereCast(transform.position, cc.radius * 0.8f, -transform.right, out wallRunHit, cc.radius * 1.3f))
                if ((Physics.SphereCast(transform.position, cc.radius * 0.8f, wallDetDirSpeed, out wallRunHit, wallrunDetRange) || Physics.SphereCast(transform.position, cc.radius * 0.8f, wallDetDirInput, out wallRunHit, wallrunDetRange)) && !jumpOnCD)
                {
                    EnteredWallRide();
                    return;
                }
            }
        }

        else
            grounded = Physics.SphereCast(transform.position, cc.radius * 0.7f, Vector3.down, out hit, originalHeight / 3f);
    }

    void ReadMovementInput()
    {
        inputs = InputManager.instance.p_actions.Move.ReadValue<Vector2>();
    }

    void MovementGround()
    {
        float targetDrag, currentMaxSpeed;

        if (wasOnAir)
            doubleJump = true;

        Physics.Raycast(transform.position, Vector3.down, out hit2);

        if (crouched)
        {
            MovementCrouched();
            return;
        }

        else if (wasOnAir)
        {
            TouchedGround();
        }

        rb.useGravity = false;

        targetDrag = originalDrag;
        currentMaxSpeed = groundMaxSpeed;

        moveDir = moveDir * HandleSprint() + transform.up * rb.velocity.y;

        horizontalVel = Vector3.ProjectOnPlane(moveDir, hit2.normal);
        horizontalVel = Vector3.ClampMagnitude(horizontalVel, currentMaxSpeed);

        if (!jumpOnCD)
        {
            transform.position = new Vector3(transform.position.x, hit2.point.y + Vector3.Distance(hit2.point, transform.position), transform.position.z);
            rb.velocity = horizontalVel;
        }
            
        else
            rb.velocity = new Vector3(horizontalVel.x, rb.velocity.y, horizontalVel.z);
        

        if (inputs == Vector2.zero)
            rb.drag = counterDrag;

        else
            rb.drag = targetDrag;

        //Debug.Log(horizontalVel.magnitude);
    }

    void MovementCrouched()
    {
        float targetDrag, currentMaxSpeed;

        targetDrag = crouchedDrag;
        currentMaxSpeed = slideMinSpeed;

        resetSway = true;

        if (wasOnAir)
            SlideBoost();

        if (sliding)
        {
            targetDrag = slideDrag;
            rb.drag = targetDrag;
            rb.useGravity = true;

            if (horizontalVel.magnitude < currentMaxSpeed)
            {
                sliding = false;
            }
        }

        else
        {
            rb.useGravity = false;
            rb.drag = targetDrag;
            moveDir = moveDir * crouchedSpeed + transform.up * rb.velocity.y;
            rb.velocity = Vector3.ProjectOnPlane(moveDir, hit2.normal);

            if (inputs == Vector2.zero)
                rb.drag = counterDrag;
        }
    }

    void ClimbDetection()
    {
        RaycastHit tempHit;

        if (Physics.SphereCast(transform.position, cc.radius * 0.8f, cmTransform.forward, out tempHit, wallrunDetRange))
            if (180 - Mathf.Abs(Vector3.Angle(transform.forward, tempHit.normal)) < 30)
                if(inputs.y > 0)
                    if (!Physics.SphereCast(transform.position, cc.radius, Vector3.up, out climbHit, climbDetRange + originalHeight / 1.5f))
                        if (!Physics.Raycast(transform.position + Vector3.up * originalHeight / 1.5f, -tempHit.normal, out climbHit, cc.radius * 2.1f + wallrunDetRange))
                            if (Physics.SphereCast(transform.position + Vector3.up * originalHeight / 1.5f + -tempHit.normal * (cc.radius * 2.1f + wallrunDetRange), cc.radius * 0.75f, Vector3.down, out climbHit, climbDetRange))
                                StartCoroutine(Climb());
    }

    Vector3 BezierCurve(Vector3 start, Vector3 p1, Vector3 final, float t)
    {
        return Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * final;
    }

    void TouchedGround()
    {
        float forwardSpeed = new Vector3(0, 0, horizontalVel.z).magnitude;
            

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

    void HandleFOV()
    {
        Vector3 locVel = transform.InverseTransformDirection(horizontalVel);

        if (locVel.z < 0)
            locVel.z = 0;

        float forwardMag = new Vector3(0, 0, locVel.z).magnitude;

        float mod = (forwardMag / maxExtraFOV);

        if (mod > 1)
            mod = 1;

        cm.fieldOfView = Mathf.Lerp(cm.fieldOfView, normalFOV + maxExtraFOV * mod, Time.deltaTime * 3);
    }

    void CameraSway()
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

    void MovementAir()
    {
        rb.drag = airDrag;
        resetSway = true;

        rb.useGravity = true;

        Vector3 projVel = Vector3.Project(horizontalVel, moveDir);
        Vector3 targetForce = moveDir.normalized * airStrafeForce;
        bool goingAway = Vector3.Dot(moveDir, projVel.normalized) <= 0f;

        if (goingAway)
        {
            targetForce = Vector3.ClampMagnitude(targetForce, airMaxSpeed + projVel.magnitude);

            rb.AddForce(targetForce, ForceMode.VelocityChange);
        }

        if (projVel.magnitude < airMaxSpeed)
        {
            rb.AddForce(moveDir * speed, ForceMode.Force);
        }
    }

    void Jump()
    {
        if (jumpOnCD || climbing)
            return;

        if (wallRunning)
        {
            ExitWallRide();
            rb.velocity = rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(jumpForce * 0.8f * wallRunHit.normal + Vector3.up * jumpForce * 0.75f, ForceMode.Impulse);
            return;
        }

        if (!grounded && doubleJump)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

            if (inputs == Vector2.zero)
                rb.AddForce(doubleJumpForce * Vector3.up, ForceMode.Impulse);

            else
                rb.AddForce(new Vector3(moveDir.x, doubleJumpForce, moveDir.z), ForceMode.Impulse);

            doubleJump = false;

            return;
        }

        else if (!grounded)
            return;

        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(jumpForce * Vector3.up, ForceMode.Impulse);
    }

    float HandleSprint()
    {
        if (inputs.y <= 0)
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

    void CrouchToggle(bool toggle)
    {
        if (wallRunning)
        {
            ExitWallRide();
            return;
        }

        if (grappling)
            ExitGrapple();

        RaycastHit hitTemp;

        if (toggle && !crouched)
        {
            cc.height = originalHeight * 0.5f;
            transform.position -= transform.up * cc.height / 2;
            rb.drag = crouchedDrag;
            crouched = true;

            float zSpeed = new Vector3(0, 0, horizontalVel.z).magnitude;

            if (zSpeed > groundMaxSpeed / 1.5f)
                SlideBoost();
        }

        else if(!Physics.SphereCast(transform.position, cc.radius, transform.up, out hitTemp, cc.height * 0.6f) && crouched)
        {
            transform.position += transform.up * cc.height / 2;
            cc.height = originalHeight;
            rb.drag = originalDrag;
            crouched = false;
        }
    }

    void SlideBoost()
    {
        if (slideOnCD || !grounded)
            return;

        rb.AddForce(rb.velocity.normalized * slideBoost, ForceMode.Impulse);
        sliding = true;

        StartCoroutine(SlideBoostCD());
    }

    void EnteredWallRide()
    {
        if ((lastWall == wallRunHit.transform.gameObject && wallRunOnCD))//|| wallRunHit.transform.gameObject.layer != 8)
            return;

        wallrunTimer = wallrunExitTime;
        lastWall = wallRunHit.transform.gameObject;

        doubleJump = true;
        wallRunning = true;
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        //rb.velocity = Vector3.ProjectOnPlane();
        rb.drag = wallDrag;
        rb.useGravity = false;
    }

    void WallRiding()
    {
        if(wallrunTimer <= 0)
        {
            wallrunTimer = wallrunExitTime;
            ExitWallRide();
            return;
        }

        wallrunTimer -= Time.fixedDeltaTime;

        wallRideNormal = wallRunHit.normal;

        if (Vector3.Angle(Vector3.Project(transform.forward, Vector3.Cross(wallRideNormal, transform.up)), Vector3.Cross(wallRideNormal, transform.up)) < 90)
        {
            wallVector = Vector3.Cross(wallRideNormal, transform.up);
            wallTiltDir = -1;
        }

        else
        {
            wallVector = -Vector3.Cross(wallRideNormal, transform.up);
            wallTiltDir = 1;
        }

        float angle = Vector3.Angle(wallRideNormal, moveDir);

        rb.AddForce(wallVector * wallForce * inputs.y + -wallRideNormal * 45);

        if ((angle < wallrunExitAngle && angle != 0) || !Physics.Raycast(transform.position, -wallRideNormal, out wallRunHit, wallrunDetRange * 2f))
        {
            ExitWallRide();
        }
    }

    void ExitWallRide()
    {
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

    void HandleCmTilt()
    {
        if (wallRunning && currentCmTilt < maxCmTilt)
            currentCmTilt += Time.fixedDeltaTime * 60;

        else if (wallRunning && currentCmTilt > maxCmTilt)
            currentCmTilt = maxCmTilt;

        else if (!wallRunning && currentCmTilt > 0)
            currentCmTilt -= Time.fixedDeltaTime * 60;

        else if (!wallRunning && currentCmTilt < 0)
            currentCmTilt = 0;

        cmTransform.localRotation = Quaternion.Euler(new Vector3 (cmTransform.eulerAngles.x, cmTransform.eulerAngles.y, currentCmTilt) *  wallTiltDir);
    }

    void GrappleHook()
    {
        if (grappleOnCD)
            return;

        Physics.Raycast(cmTransform.position, cmTransform.forward, out grappleHit, 200);

        if (!grappling && !grappleThrown)
        {
            grappleThrown = true;
            grapple = pools.GrabFromPool("Grapple", transform.position, Quaternion.identity);

            if(grappleHit.point != Vector3.zero)
                grapple.GetComponent<Rigidbody>().velocity = (grappleHit.point - transform.position).normalized * hookSpeed;

            else
                grapple.GetComponent<Rigidbody>().velocity = (cmTransform.position + cmTransform.forward * 200 - transform.position).normalized * hookSpeed;
        }

        else
            ExitGrapple();
    }

    public void GrappleConnect()
    {
        grappleJoint = gameObject.AddComponent<SpringJoint>();

        grappleJoint.autoConfigureConnectedAnchor = false;

        grappling = true;
        grappleJoint.connectedAnchor = grappleHit.point;
        grappleJoint.maxDistance = Vector3.Distance(grappleHit.point, transform.position);
        grappleJoint.minDistance = 1;
        grappleJoint.spring = hookSpring;
        grappleJoint.damper = hookDamper;
        grappleJoint.massScale = hookMassScale;

    }

    void Grappling()
    {
        float distance = Vector3.Distance(grappleHit.point, transform.position);

        if (distance < 2 || Vector3.Angle(transform.forward, (grappleHit.point - transform.position).normalized) > 90 || !grapple.activeInHierarchy)
            ExitGrapple();

        if(grappleJoint.maxDistance > 2)
            grappleJoint.maxDistance -= hookReelSpeed * Time.fixedDeltaTime;
    }

    void ExitGrapple()
    {
        Destroy(grappleJoint);
        grappleThrown = false;
        grappling = false;
        grapple.SetActive(false);
        StartCoroutine(GrappleCD());
    }

    IEnumerator JumpCD()
    {
        jumpOnCD = true;

        yield return new WaitForSeconds(0.3f);

        jumpOnCD = false;
    }

    IEnumerator GrappleCD()
    {
        grappleOnCD = true;

        yield return new WaitForSeconds(hookCD);

        grappleOnCD = false;
    }

    IEnumerator SprintCD()
    {
        sprintCD = true;

        yield return new WaitForSeconds(1);

        sprintCD = false;
    }

    IEnumerator SlideBoostCD()
    {
        slideOnCD = true;

        yield return new WaitForSeconds(2);

        slideOnCD = false;
    }

    IEnumerator WallRunCD()
    {
        wallRunOnCD = true;

        yield return new WaitForSeconds(wallRunCD);

        wallRunOnCD = false;
    }

    IEnumerator Climb()
    {
        if (wallRunning)
        {
            ExitWallRide();
        }
            

        climbing = true;

        float t = Time.deltaTime;

        Vector3 start = transform.position;
        Vector3 p1 = start + Vector3.up * (climbDetRange + originalHeight / 1.5f);
        //Vector3 final = p1 + -wallRunHit.normal * (cc.radius * 2.1f + + wallrunDetRange);
        Vector3 final = climbHit.point + climbHit.normal * (originalHeight / 2);

        Debug.DrawLine(start, p1, Color.red, 5);
        Debug.DrawLine(p1, final, Color.red, 5);

        while (transform.position != final && t < 1)
        {
            t += Time.deltaTime * climbSpeedMod;
            transform.position = BezierCurve(start, p1, final, t);
            yield return null;
        }

        climbing = false;
    }
}