using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player instance;

    [HideInInspector]
    public PlayerMovement playerMovement;

    [HideInInspector]
    public PlayerCamera playerCamera;

    Vector3 currentSpawn;

    [SerializeField]
    GameObject controlsGO, pauseMenu;

    BaseEnemy[] enemies;

    bool onMenu;

    private void Awake()
    {
        if (!instance)
            instance = this;

        playerMovement = GetComponent<PlayerMovement>();
        playerCamera = GetComponentInChildren<PlayerCamera>();
    }

    private void Start()
    {
        enemies = FindObjectsOfType<BaseEnemy>();
        InputManager.instance.p_actions.Quit.started += ctx => Pause();

        InputManager.instance.p_actions.Controls.started += ctx => ControlsToggle(true);
        InputManager.instance.p_actions.Controls.canceled += ctx => ControlsToggle(false);


        ChangeSpawn(transform.position);
    }

    public void ChangeSpawn(Vector3 spawn)
    {
        currentSpawn = spawn;
    }

    public void Death()
    {
        transform.position = currentSpawn;
        playerMovement.ResetState();

        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].Respawn();
        }
    }

    void ControlsToggle(bool toggle)
    {
        controlsGO.SetActive(toggle);
    }

    public void Pause()
    {
        pauseMenu.SetActive(!onMenu);

        if (!onMenu)
        {
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0;
            onMenu = true;
        }


        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Time.timeScale = 1;
            onMenu = false;
        }

        Cursor.visible = onMenu;
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}