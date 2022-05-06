using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : InteractibleNoInput
{
    protected override void Interaction()
    {
        player.ChangeSpawn(transform.position);
    }
}
