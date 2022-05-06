using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tp : InteractibleNoInput
{
    [SerializeField]
    Transform destination;

    protected override void Interaction()
    {
        base.Interaction();

        player.transform.position = destination.position;
    }
}
