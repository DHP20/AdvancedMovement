using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager inputManager;
    public PlayerInputs p_input;
    public PlayerInputs.PlayerActions p_actions;

    //[HideInInspector]
    public Vector2 movementInput;
    //[HideInInspector]
    public Vector2 cameraInput;

    private void Awake()
    {
        if (!inputManager)
            inputManager = this;

        p_input = new PlayerInputs();
        p_actions = p_input.Player;

        p_actions.Move.performed += ctx => movementInput = p_actions.Move.ReadValue<Vector2>();
        p_actions.Move.canceled += ctx => movementInput = p_actions.Move.ReadValue<Vector2>();

        p_actions.Look.performed += ctx => cameraInput = p_actions.Look.ReadValue<Vector2>();
        p_actions.Look.canceled += ctx => cameraInput = p_actions.Look.ReadValue<Vector2>();
    }

    private void OnEnable()
    {
        p_input.Enable();
    }

    private void OnDisable()
    {
        p_input.Disable();
    }
}
