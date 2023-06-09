using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    public static Player instance;

    [HideInInspector]
    public PlayerMovement playerMovement;

    Vector3 currentSpawn;

    private void Awake()
    {
        if (!instance)
            instance = this;

        playerMovement = GetComponent<PlayerMovement>();
    }

    protected override void Start()
    {
        base.Start();

        ChangeSpawn(transform.position);
    }

    public void ChangeSpawn(Vector3 spawn)
    {
        currentSpawn = spawn;
    }

    public override void Death()
    {
        Respawn();
    }

    void Respawn()
    {
        transform.position = currentSpawn;
        playerMovement.ResetState();
    }
}