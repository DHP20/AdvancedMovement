using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractibleNoInput : MonoBehaviour
{
    protected Player player;

    protected void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out player))
            Interaction();
    }

    protected virtual void Interaction() { }
}
