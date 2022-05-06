using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBox : InteractibleNoInput
{
    BaseEnemy enemy;

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.TryGetComponent<BaseEnemy>(out enemy))
            enemy.Respawn();
    }

    protected override void Interaction()
    {
        player.Death();
    }
}
