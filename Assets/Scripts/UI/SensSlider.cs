using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SensSlider : MonoBehaviour
{
    Text text;

    [SerializeField]
    public Slider slider;

    private void Awake()
    {
        text = GetComponent<Text>();
    }

    void Start()
    {
        slider.value = PlayerCamera.playerCamera.sens;
        text.text = slider.value.ToString();
    }

    public void UpdateValue()
    {
        PlayerCamera.playerCamera.sens = slider.value;
        PlayerPrefs.SetFloat("Sens", slider.value);
        text.text = slider.value.ToString();
    }
}
