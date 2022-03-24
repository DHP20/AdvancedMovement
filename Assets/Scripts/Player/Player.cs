using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player player;

    [HideInInspector]
    public PlayerMovement playerMovement;

    [HideInInspector]
    public PlayerCamera playerCamera;

    private void Awake()
    {
        if (!player)
            player = this;

        playerMovement = GetComponent<PlayerMovement>();
        playerCamera = GetComponentInChildren<PlayerCamera>();
    }
}