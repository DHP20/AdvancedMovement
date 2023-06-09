using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    InputManager inputs;

    [SerializeField]
    GameObject controlsGO, pauseMenu;

    bool onMenu;

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);

        else
            instance = this;
    }

    private void Start()
    {
        inputs = InputManager.instance;

        inputs.p_actions.Quit.started += ctx => Pause();

        inputs.p_actions.Controls.started += ctx => ControlsToggle(true);
        inputs.p_actions.Controls.canceled += ctx => ControlsToggle(false);
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