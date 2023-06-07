using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : MonoBehaviour
{
    Rigidbody rb;
    RigidbodyConstraints originalConstraints;
    LineRenderer lr;
    Transform playerT;
    PlayerMovement playerM;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lr = GetComponent<LineRenderer>();
        originalConstraints = rb.constraints;
    }

    private void Start()
    {
        Physics.IgnoreCollision(GetComponent<Collider>(), Player.instance.playerMovement.cc, true);
        playerM = Player.instance.playerMovement;
    }

    private void OnDisable()
    {
        rb.constraints = originalConstraints;
        rb.velocity = Vector3.zero;
    }

    private void OnEnable()
    {
        playerT = Player.instance.transform;
        rb.constraints = originalConstraints;
    }

    private void OnCollisionEnter(Collision collision)
    {
        rb.velocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        playerM.GrappleConnect();
    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, playerT.position) > 30)
            gameObject.SetActive(false);

        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, playerT.position);
    }
}