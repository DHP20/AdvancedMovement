using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwapSkill : MonoBehaviour
{
    PlayerCamera p_cm;
    Transform cm;

    [SerializeField]
    LayerMask enemyLM;

    [SerializeField]
    GameObject uiSwap;

    Player player;

    Rigidbody playerRB;

    Image swapUI;

    [SerializeField]
    float maxReserve = 10;
    float currentReserve = 0;
    bool swapMode, swapOnCD;

    private void Start()
    {
        InputManager.inputManager.p_actions.DownSights.started += ctx => SwapOnOff(true); 
        InputManager.inputManager.p_actions.DownSights.canceled += ctx => SwapOnOff(false);

        InputManager.inputManager.p_actions.Interact.started += ctx => Swap();

        p_cm = PlayerCamera.playerCamera;
        cm = p_cm.transform;

        currentReserve = maxReserve;
        playerRB = Player.player.playerMovement.rb;

        if (uiSwap)
        {
            uiSwap = Instantiate(uiSwap, transform);
            swapUI = uiSwap.GetComponentInChildren<Image>();
        }

        player = Player.player;
    }

    private void Update()
    {
        if (swapMode)
            SwapMode();

        else if (currentReserve < maxReserve)
        {
            currentReserve += Time.deltaTime;
            UpdateUI();
        }
    }

    void SwapOnOff(bool on)
    {
        if (swapOnCD)
            return;

        swapMode = on;

        if (on)
        {
            Time.timeScale = 0.5f;
            player.playerCamera.sens /= 0.5f;
            Application.targetFrameRate = 120;
        }

        else
        {
            Time.timeScale = 1;
            player.playerCamera.sens *= 0.5f;
            Application.targetFrameRate = 60;
        }
    }

    void SwapMode()
    {
        if (currentReserve <= 0)
        {
            StartCoroutine(SwapCD());
            return;
        }  

        currentReserve -= Time.deltaTime;
        UpdateUI();
    }

    void Swap()
    {
        RaycastHit hit;
        bool hitEnemy = Physics.Raycast(cm.position, cm.forward, out hit, 100, enemyLM);

        if (!swapMode || !hitEnemy)
            return;

        Transform enemyT = hit.transform;
        
        Vector3 enemyPos = enemyT.position;
        Quaternion enemyRot = Quaternion.Euler(0, enemyT.rotation.eulerAngles.y, 0);
        //Vector3 newVel = Vector3.Project(playerRB.velocity, enemyT.up);

        enemyT.position = transform.position;
        enemyT.rotation = transform.rotation;

        transform.position = enemyPos;
        transform.rotation = enemyRot;

        //playerRB.velocity = newVel;

        currentReserve -= 2;

        if (currentReserve < 0)
            currentReserve = 0;

        UpdateUI();
    }

    void UpdateUI()
    {
        swapUI.fillAmount = currentReserve / maxReserve;

        if (swapUI.fillAmount == 1)
            uiSwap.SetActive(false);

        else
            uiSwap.SetActive(true);
    }

    IEnumerator SwapCD()
    {
        swapOnCD = true;
        Time.timeScale = 1;
        swapMode = false;

        yield return new WaitForSeconds(3);

        swapOnCD = false;
    }
}