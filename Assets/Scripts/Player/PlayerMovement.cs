using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    protected Rigidbody rb;
    protected CapsuleCollider cc;
    protected RaycastHit hit;

    [SerializeField]
    protected float speed, jumpForce, sprintSpeed, stamina;
    protected float currentStamina, jumpTime;

    protected bool grounded, sprinting, sprintCD;

    protected Player player;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        player = Player.player;
    }

    protected void Start()
    {
        currentStamina = stamina;
        Application.targetFrameRate = 90;

        InputManager.inputManager.p_actions.Jump.started += ctx => Jump();
        InputManager.inputManager.p_actions.Sprint.started += ctx => sprinting = true;
        InputManager.inputManager.p_actions.Sprint.canceled += ctx => sprinting = false;
    }

    protected void FixedUpdate()
    {
        grounded = Physics.SphereCast(transform.position, cc.radius * 0.9f, Vector3.down, out hit, cc.height / 2.75f);
        rb.useGravity = !grounded;

        if(grounded)
            Movement();
    }

    protected virtual void Movement()
    {
        Vector2 inputs = InputManager.inputManager.movementInput;
        Vector3 targetSpeed = transform.forward * inputs.y + transform.right * inputs.x;

        targetSpeed = targetSpeed * HandleSprint() + transform.up * rb.velocity.y;

        RaycastHit hit2;
        Physics.Raycast(transform.position, Vector3.down, out hit2);

        rb.velocity = Vector3.Lerp(rb.velocity, Vector3.ProjectOnPlane(targetSpeed, hit2.normal), Time.deltaTime * 15);
    }

    protected void Jump()
    {
        if (!grounded)
            return;

        rb.AddForce(jumpForce * rb.mass * Vector3.up, ForceMode.Impulse);
    }

    protected float HandleSprint()
    {
        if (InputManager.inputManager.movementInput.y <= 0)
            return speed;

        if (sprintCD)
            return speed;

        if (currentStamina <= 0)
            StartCoroutine(SprintCD());

        if (sprinting && currentStamina > 0)
        {
            currentStamina -= Time.fixedDeltaTime * 10;
            return sprintSpeed;
        }

        else
        {
            if (currentStamina < stamina)
                currentStamina += Time.fixedDeltaTime * 12;

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