using System;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragManager : MonoBehaviour
{
    [Header("Input Refs")]
    public InputActionReference TrackingAction;
    public InputActionReference ClickingAction;

    public static DragManager Instance;

    public Camera Camera;

    private Vector3 _startPosition;
    private Vector2Int _startGridPos;
    private TileController _selectedTile;
    private Vector3 _currentPos;
    private Transform _selectedObject;
    private Vector3 _offset;
    private Plane _dragPlane;
    private bool _isBusy = false;

    public event Action<TransitionContext> OnDragCompleted;

    private void Awake()
    {
        Instance = this;
    }
    private void OnEnable()
    {
        TrackingAction.action.Enable();
        ClickingAction.action.Enable();

        TrackingAction.action.performed += OnTouchPosition;
        ClickingAction.action.performed += OnTouchPress;
        ClickingAction.action.canceled += OnTouchRelease;
    }
    private void OnDisable()
    {
        TrackingAction.action.performed -= OnTouchPosition;
        ClickingAction.action.performed -= OnTouchPress;
        ClickingAction.action.canceled -= OnTouchRelease;

        TrackingAction.action.Disable();
        ClickingAction.action.Disable();
    }

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTouchPress(InputAction.CallbackContext context)
    {
        if (_isBusy) return;
        _currentPos = Pointer.current.position.ReadValue();
        var ray = Camera.ScreenPointToRay(_currentPos);
        var hit = Physics2D.GetRayIntersection(ray);
        if (hit.collider != null)
        {
            _startPosition = hit.transform.position;
            _selectedObject = hit.transform;
            var tile = hit.transform.GetComponent<TileController>();
            _startGridPos = tile.GridPosition;
            _offset = _selectedObject.position - (Vector3)hit.point;
            _dragPlane = new Plane(-Camera.transform.forward, hit.point);

            _selectedObject.gameObject.layer = 2;
        }
            
    }

    private void OnTouchRelease(InputAction.CallbackContext context)
    {
        Debug.Log(_selectedObject is null);
        if (_selectedObject is not null)
        {
            Vector3 rayOrigin = _selectedObject.position;
            rayOrigin.z = Camera.transform.position.z;
            Ray ray = new Ray(rayOrigin, Camera.transform.forward);

            var hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider != null && hit.transform != _selectedObject)
            {
                var draggedTile = _selectedObject.GetComponent<TileController>();
                var hitTile = hit.transform.GetComponent<TileController>();
                var field = GetComponentInParent<FieldController>();
                OnDragCompleted?.Invoke(new TransitionContext { });
                //_ = field.TryPlayerMove(hitTile, _startGridPos, draggedTile, hitTile.GridPosition);
            } else
            {
                _selectedObject.position = _startPosition;
            }
            _selectedObject.gameObject.layer = 0;
        }
        
        _selectedObject = null;
    }

    private void OnTouchPosition(InputAction.CallbackContext context)
    {
        _currentPos = context.ReadValue<Vector2>();
        if (_selectedObject is not null)
        {
            var ray = Camera.ScreenPointToRay(_currentPos);
            if (_dragPlane.Raycast(ray, out var distance))
            {
                var targetPos = ray.GetPoint(distance) + _offset;
                var heading = targetPos - _startPosition;
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

                _selectedObject.position = new Vector3(
                    _startPosition.x + heading.x,
                    _startPosition.y + heading.y,
                    _startPosition.z
                );
            }
        }
    }

    internal void SetBusy(bool v)
    {
        _isBusy = v;
    }
}
