using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    int fps;

    [SerializeField]
    bool vSync;

    private void Start()
    {
        Application.targetFrameRate = fps;
        QualitySettings.vSyncCount = vSync ? 1 : 0;
    }
}
