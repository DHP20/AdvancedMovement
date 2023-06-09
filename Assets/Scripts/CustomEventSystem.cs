using System;
using UnityEngine;

public class CustomEventSystem : MonoBehaviour
{
    public static CustomEventSystem instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        else
            Destroy(this.gameObject);
    }

    public event Action UpdateUI;

    public void OnUpdateUI()
    {
        if (UpdateUI != null)
            UpdateUI();
    }

    public event Action HUDUpdate;

    public void OnHUDUpdate()
    {
        if (HUDUpdate != null)
            HUDUpdate();
    }

    public event Action ResetAll;

    public void OnResetAll()
    {
        if (ResetAll != null)
            ResetAll();
    }

    public event Action CinematicStart;

    public void OnCinematicStart()
    {
        if (CinematicStart != null)
            CinematicStart();
    }

    public event Action CinematicEnd;

    public void OnCinematicEnd()
    {
        if (CinematicEnd != null)
            CinematicEnd();
    }

    public event Action LevelCompletedEvent;

    public void OnLevelCompleted()
    {
        if (LevelCompletedEvent != null)
        {
            LevelCompletedEvent();
        }
    }
    public event Action GameOverEvent;

    public void OnGameOver()
    {
        if (GameOverEvent != null)
            GameOverEvent();

    }
}