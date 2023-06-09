using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBox : InteractibleNoInput
{
    protected override void Interaction()
    {
        player.Death();
    }
}
