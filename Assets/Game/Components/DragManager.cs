using System;
using Unity.InferenceEngine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;

public class DragManager : MonoBehaviour
{
    [Header("Input Refs")]
    [Req] public InputActionReference TrackingAction;
    [Req] public InputActionReference ClickingAction;

    [Header("Camera Ref")]
    [Req] public Camera Camera;
    [Req] public Events Events;

    private Tile Source;
    private Tile Dest;

    private Vector3 SourceStartPos;
    private Vector3 DestStartPos;



    private void OnEnable()
    {
        TrackingAction.action.performed += OnTouchPosition;
        ClickingAction.action.started += OnTouchPress;
        ClickingAction.action.canceled += OnTouchRelease;
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Register(name, OnInputEventReceived);
    }

    private void OnInputEventReceived(bool enable)
    {
        if (enable)
        {
            TrackingAction.action.Enable();
            ClickingAction.action.Enable();
        }
        else
        {
            TrackingAction.action.Disable();
            ClickingAction.action.Disable();
        }
    }

    private void OnDisable()
    {
        TrackingAction.action.performed -= OnTouchPosition;
        ClickingAction.action.started -= OnTouchPress;
        ClickingAction.action.canceled -= OnTouchRelease;
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Unregister(name, OnInputEventReceived);


    }


    private void OnTouchPress(InputAction.CallbackContext context)
    {
        var pos = Pointer.current.position.ReadValue();
        Vector3 worldPoint = Camera.ScreenToWorldPoint(new Vector3(pos.x, pos.y, Camera.main.nearClipPlane));
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
        if (hit.collider != null)
        {
            if (hit.transform.TryGetComponent<Tile>(out var tile))
            {
                //Debug.Log($"[DragManager] Source tile found: {tile.Id}");
                Source = tile;
                SourceStartPos = tile.transform.position;
                Source.gameObject.layer = 2;
            }
        }
    }

    private void OnTouchRelease(InputAction.CallbackContext context)
    {
        // если не была записана таргет плитка - вернуть плитку на свою позицию, 
        //иначе записываем в структуру стартовые и текщие позиции, отправляем ивент
        if (Source is null) return;
        if (Dest is null)
        {
            Source.transform.position = SourceStartPos;
            Source.gameObject.layer = 0;
            Source = null;
            return;
        }

        var data = new SwapInfo
        {
            SourceId = Source.Id,
            DestId = Dest.Id
        };

        //Source.transform.position = DestStartPos;
        //Dest.transform.position = SourceStartPos;
        Source.transform.position = SourceStartPos;
        Dest.transform.position = DestStartPos;

        Source.gameObject.layer = 0;
        Dest.gameObject.layer = 0;

        Source = null;
        Dest = null;
        SourceStartPos = Vector3.zero;
        DestStartPos = Vector3.zero;

        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<SwapInfo>.Trigger(name, data);
    } 

    private void OnTouchPosition(InputAction.CallbackContext context)
    {
        //позиция плитки меняется вслед за позицией курсора
        //с ограничениями : не больше 1 стандартной длины(? типа до соседа)
        //                  только по вертикали и горизонтали.
        //если плитка на соседе - сосед прыгает на стартовую позицию перетаскиваемой плитки.
        //если курсор уносит с позиции - сосед прыгает обратно
        var pos = context.ReadValue<Vector2>();
        Vector3 worldPoint = Camera.ScreenToWorldPoint(new Vector3(pos.x, pos.y, Camera.main.nearClipPlane));

        if (Source is null) return; 

        if (Dest is null)
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.transform.TryGetComponent<Tile>(out var tile))
                {
                    Dest = tile;
                    DestStartPos = tile.transform.position;
                    Dest.transform.position = SourceStartPos;
                    Dest.gameObject.layer = 2;
                    return;
                }
            }
        }

        //надо проверять находится ли еще плитка сорс в позиции или рядом с дест и если нет - возвращать дест обратно и занулять.
        //тогда следущий рейкаст проверит новую плитку и поставит ее в сорс и поменяет ее позицию на стартовую сорса
        if (Dest != null && Vector2.Distance(worldPoint, DestStartPos) > 1.2f) // 1.2f ≈ размер клетки + небольшой допуск
        {
            Dest.transform.position = DestStartPos;
            Dest.gameObject.layer = 0;
            Dest = null;
            DestStartPos = Vector3.zero;
        }

        float dx = worldPoint.x - SourceStartPos.x;
        float dy = worldPoint.y - SourceStartPos.y;

        // Только по одной оси: выбираем доминирующее направление
        if (Mathf.Abs(dx) > Mathf.Abs(dy))
        {
            dy = 0; 
            dx = Mathf.Clamp(dx, -1f, 1f); 
        }
        else
        {
            dx = 0;
            dy = Mathf.Clamp(dy, -1f, 1f);
        }

        Source.transform.position = new Vector3(SourceStartPos.x + dx, SourceStartPos.y + dy, SourceStartPos.z);
    }
}
