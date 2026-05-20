using System;
using Unity.InferenceEngine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragManager : MonoBehaviour
{
    [Header("Input Refs")]
    [Req] public InputActionReference TrackingAction;
    [Req] public InputActionReference ClickingAction;

    [Header("Camera Ref")]
    [Req] public Camera Camera;
    [Req] public Events Events;

    //public event Action<TransitionContext> OnDragCompleted;

    private Vector3 _startPosition;
    private Vector2Int _startGridPos;
    private TileController _selectedTile;
    private Vector3 _currentPos;
    private TileController _selectedObject;
    private Vector3 _offset;
    private Plane _dragPlane;
    private bool _isBusy = false;

    

    private void OnEnable()
    {
        TrackingAction.action.performed += OnTouchPosition;
        ClickingAction.action.performed += OnTouchPress;
        ClickingAction.action.canceled += OnTouchRelease;
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Register(name, OnInputEventReceived);
    }

    private void OnInputEventReceived(bool enable)
    {
        Debug.Log("inputsystem switch event called");
        if (enable)
        {
            TrackingAction.action.Enable();
            ClickingAction.action.Enable();
        } else
        {
            TrackingAction.action.Disable();
            ClickingAction.action.Disable();
        }
    }

    private void OnDisable()
    {
        TrackingAction.action.performed -= OnTouchPosition;
        ClickingAction.action.performed -= OnTouchPress;
        ClickingAction.action.canceled -= OnTouchRelease;
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Unregister(name, OnInputEventReceived);

        
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //private void OnTouchPress(InputAction.CallbackContext context)
    //{
    //    if (_isBusy) return;
    //    _currentPos = Pointer.current.position.ReadValue();
    //    var ray = Camera.ScreenPointToRay(_currentPos);
    //    var hit = Physics2D.GetRayIntersection(ray);
    //    if (hit.collider != null)
    //    {
    //        if (hit.transform.TryGetComponent<TileController>(out var tile))
    //        {
    //            _startPosition = hit.transform.position;
    //            _selectedObject = tile;
    //            _startGridPos = tile.GridPosition;
    //            _offset = tile.transform.position - (Vector3)hit.point;
    //            _dragPlane = new Plane(-Camera.transform.forward, hit.point);

    //            _selectedObject.gameObject.layer = 2;
    //        }
    //    }
    //}



    private void OnTouchPress(InputAction.CallbackContext context)
    {
        if (_isBusy) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        _currentPos = Pointer.current.position.ReadValue();

        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(_currentPos.x, _currentPos.y, Mathf.Abs(cam.transform.position.z)));
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.transform.TryGetComponent<TileController>(out var tile))
            {
                _selectedObject = tile;
                _startPosition = tile.transform.position;
                _startGridPos = tile.GridPosition;
                Vector3 hitPoint2D = new Vector3(hit.point.x, hit.point.y, _startPosition.z);
                _offset = _startPosition - hitPoint2D;
                _selectedObject.gameObject.layer = 2;
            }
        }
    }

    private void OnTouchRelease(InputAction.CallbackContext context)
    {
        Debug.Log(_selectedObject is null);
        if (_selectedObject is not null)
        {
            Vector3 rayOrigin = _selectedObject.transform.position;
            rayOrigin.z = Camera.transform.position.z;
            Ray ray = new Ray(rayOrigin, Camera.transform.forward);

            var hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider != null && hit.transform != _selectedObject)
            {
                var to = hit.transform.GetComponent<TileController>();
                var ctx = new TransitionContext
                {
                    Type = StateEvent.MoveTiles,
                    From = _selectedObject,
                    To = to,
                    PositionFrom = _startGridPos,
                    PositionTo = to.GridPosition
                };

                var name = Events.GetBusName(GameEvent.Input);
                GameplayEventBus<TransitionContext>.Trigger(name, ctx);

                //OnDragCompleted?.Invoke(ctx);
            } else
            {
                _selectedObject.transform.position = _startPosition;
            }
            _selectedObject.gameObject.layer = 0;
        }
        
        _selectedObject = null;
    }

    //private void OnTouchPosition(InputAction.CallbackContext context)
    //{
    //    _currentPos = context.ReadValue<Vector2>();
    //    if (_selectedObject is not null)
    //    {
    //        var ray = Camera.ScreenPointToRay(_currentPos);
    //        if (_dragPlane.Raycast(ray, out var distance))
    //        {
    //            var targetPos = ray.GetPoint(distance) + _offset;
    //            var heading = targetPos - _startPosition;
    //            if (Mathf.Abs(heading.x) > Mathf.Abs(heading.y))
    //            {
    //                heading.y = 0;
    //                heading.x = Mathf.Clamp(heading.x, -1f, 1f);
    //            }
    //            else
    //            {
    //                heading.x = 0;
    //                heading.y = Mathf.Clamp(heading.y, -1f, 1f); 
    //            }

    //            _selectedObject.transform.position = new Vector3(
    //                _startPosition.x + heading.x,
    //                _startPosition.y + heading.y,
    //                _startPosition.z
    //            );
    //        }
    //    }
    //}

    private void OnTouchPosition(InputAction.CallbackContext context)
    {
        _currentPos = context.ReadValue<Vector2>();

        if (_selectedObject is not null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(_currentPos.x, _currentPos.y, Mathf.Abs(cam.transform.position.z)));

            Vector3 targetPos = worldPoint + _offset;

            Vector3 heading = targetPos - _startPosition;

            if (Mathf.Abs(heading.x) > Mathf.Abs(heading.y))
            {
                heading.y = 0;
                heading.x = Mathf.Clamp(heading.x, -1f, 1f);
            }
            else
            {
                heading.x = 0;
                heading.y = Mathf.Clamp(heading.y, -1f, 1f);
            }

            _selectedObject.transform.position = new Vector3(
                _startPosition.x + heading.x,
                _startPosition.y + heading.y,
                _startPosition.z
            );
        }
    }
}
