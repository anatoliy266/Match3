using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private ToggleManager togglerMusic;
    [SerializeField] private ToggleManager togglerSound;
    [SerializeField] private ToggleManager togglerVibro;

    

    public void Initialize()
    {
        return;
    }

    public void Close()
    {
        this.gameObject.SetActive(false);
    }
}
