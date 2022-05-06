using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : InteractibleNoInput
{
    [SerializeField]
    float jumpForce = 10;

    Rigidbody p_rb;

    protected override void Interaction()
    {
        p_rb = player.GetComponent<Rigidbody>();

        p_rb.velocity = new Vector3(p_rb.velocity.x, 0 , p_rb.velocity.z);
        p_rb.AddForce(transform.up * jumpForce + transform.forward * jumpForce * 0.25f, ForceMode.Impulse);
    }
}
