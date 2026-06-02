using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class GoalController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField][Req] private Image GoalImage;
    [SerializeField][Req] private TextMeshProUGUI GoalCountText;
    [SerializeField][Req] private Image GoalCheckmark;

    private Color _color;
    private int _remainingCount;
    public bool IsCompleted;

    // Метод настройки при старте уровня
    public void SetupGoal(Color color, int count)
    {
        _color = color;
        _remainingCount = count;
        IsCompleted = false;
        if (GoalImage.material != null)
        {
            Material uniqueMaterial = new Material(GoalImage.material);
            uniqueMaterial.color = color;
            GoalImage.material = uniqueMaterial;
        }

        // Настройка текста и галочки
        GoalCountText.text = $"x{count}";
        GoalCheckmark.gameObject.SetActive(false);
        GoalCountText.gameObject.SetActive(true);
        GoalImage.gameObject.SetActive(true);
    }

    public void UpdateGoal(Color color, int destroyedCount)
    {
        // Если цель уже выполнена или цвет не совпадает — ничего не делаем
        if (IsCompleted || color != _color) return;

        _remainingCount -= destroyedCount;

        // Если собрали нужное количество или даже больше
        if (_remainingCount <= 0)
        {
            _remainingCount = 0;
            IsCompleted = true;

            GoalCountText.gameObject.SetActive(false);
            GoalCheckmark.gameObject.SetActive(true);
        }
        else
        {
            GoalCountText.text = $"x{_remainingCount}";
        }
    }
}
