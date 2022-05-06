using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractibleNoInput : MonoBehaviour
{
    protected Player player;

    protected virtual void OnValidate()
    {
        if (!GetComponent<Collider>())
            this.gameObject.AddComponent<BoxCollider>();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out player))
            Interaction();
    }

    protected virtual void Interaction() { }
}
