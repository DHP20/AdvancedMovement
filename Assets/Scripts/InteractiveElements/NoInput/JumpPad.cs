using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : InteractibleNoInput
{
    [SerializeField]
    float jumpForce = 10;

    protected override void Interaction()
    {
        player.GetComponent<Rigidbody>().AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
}
