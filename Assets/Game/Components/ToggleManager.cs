using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ToggleManager : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Events Events;

    [SerializeField] private string settingName;
    [SerializeField] private float sliderValue;

    [SerializeField] private Slider slider;
    [SerializeField] private Image toggleImage;

    private bool _currentValue;
    public bool CurrentValue
    {
        get => _currentValue;
        private set
        {
            _currentValue = value;

            slider.value = _currentValue ? slider.maxValue : slider.minValue;
            toggleImage.color = _currentValue ? Color.green : Color.red;
        }
    }




    private void OnEnable()
    {
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;

        var savedPos = PlayerPrefs.GetInt(settingName, 1);
        CurrentValue = savedPos > 0;
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        CurrentValue = !CurrentValue;

        PlayerPrefs.SetInt(settingName, CurrentValue ? 1 : 0);
        PlayerPrefs.Save();

        var name = Events.GetBusName(GameEvent.SettingChanged);
        GameplayEventBus<bool>.Trigger(name, true);

        
    }
}
