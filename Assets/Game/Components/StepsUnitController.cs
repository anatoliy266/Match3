using System;
using TMPro;
using UnityEngine;

public class StepsUnitController : MonoBehaviour
{
    [SerializeField][Req] private TextMeshProUGUI Count;

    private int _count = 0;
    internal void Initialize(int steps)
    {
        _count = steps;
        Count.text = $"{_count}";
    }
    public void UpdateSteps(int step)
    {
        Count.text = $"{_count - step}";
    }

}
