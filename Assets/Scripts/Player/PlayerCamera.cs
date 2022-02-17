using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    Transform p_transform;

    public float sens;
    float xRotation;

    private void Awake()
    {
        p_transform = transform.parent;
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Look();
    }

    void Look()
    {
        Vector2 inputs = InputManager.inputManager.cameraInput;

        float mouseX = inputs.x * sens * Time.deltaTime;
        float mouseY = inputs.y * sens * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        p_transform.Rotate(Vector3.up * mouseX);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
