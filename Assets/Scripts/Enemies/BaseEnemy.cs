using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    [SerializeField]
    protected Vector3 currentSpawn;

    private void Start()
    {
        currentSpawn = transform.position;
    }

    public void Respawn()
    {
        transform.position = currentSpawn;
    }
}
