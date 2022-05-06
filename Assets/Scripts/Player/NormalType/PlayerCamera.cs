using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    Transform p_transform;

    public static PlayerCamera playerCamera;
    PlayerMovement playerMov;

    public float sens;
    float xRotation;
    public float yRotation;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = this;

        else
            Destroy(gameObject);

        p_transform = transform.parent;
        sens = PlayerPrefs.GetFloat("Sens");

        if (sens == 0)
            sens = 5;
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        playerMov = Player.player.playerMovement;
    }

    private void Update()
    {
        Look();
    }

    void Look()
    {
        Vector2 inputs = InputManager.inputManager.cameraInput;

        float mouseX = inputs.x * sens / 2 * Time.deltaTime;
        float mouseY = inputs.y * sens / 2 * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;

        #region CameraBugged

        //if (!playerMov.wallRiding)
        //{
        //    p_transform.Rotate(Vector3.up * mouseX);

        //    transform.localRotation = Quaternion.Euler(xRotation, 0, playerMov.currentSway);
        //}

        //else
        //{
        //    transform.localRotation = Quaternion.Euler(xRotation, yRotation, playerMov.currentSway);
        //}

        #endregion

        p_transform.Rotate(Vector3.up * mouseX);

        transform.localRotation = Quaternion.Euler(xRotation, 0, playerMov.currentSway);

    }
}