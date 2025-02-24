using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static UnityEditor.SceneView;

public class PlayerController : MonoBehaviour
{

    #region Fields

    private GameInputs _gameInputs = null;

    private CameraController _camera = null;
    private MovementComponent _movement = null;
    private ShootComponent _shoot = null;

    private Vector3 _movementDirection = Vector3.zero;
    private Vector3 _previousMovementDirection = Vector3.zero;
    private Vector2 _cameraLookInput = Vector2.zero;
    private bool _isAiming = false;
    private Ray _aimTargetRay;

    #endregion


    #region Lifecycle

    private void Awake()
    {
        if (!TryGetComponent<MovementComponent>(out _movement))
            Debug.LogError($"{nameof(MovementComponent)} component not found.", this);

        if (!TryGetComponent<ShootComponent>(out _shoot))
            Debug.LogError($"{nameof(ShootComponent)} component not found", this);

        _camera = GetComponentInChildren<CameraController>();
        if (_camera == null)
            Debug.LogError($"{nameof(CameraController)} component not found", this);
    }

    private void OnEnable()
    {
        if (_gameInputs == null)
        {
            _gameInputs = new GameInputs();
            BindInputs();
        }

        _gameInputs.Enable();
    }
    private void OnDisable()
    {
        UnBindInputs();
        _gameInputs.Disable();
    }

    void FixedUpdate()
    {
        float delta = Time.fixedDeltaTime;
        UpdateMovement(delta);
    }

    private void LateUpdate()
    {
        float delta = Time.deltaTime;
        UpdateCameraLook(delta);
    }

    #endregion


    #region Handlers

    private void HandleMovement(InputAction.CallbackContext context)
    {
        Debug.Log("Move");
        var direction = context.ReadValue<Vector2>();
        Debug.Log("Mvt: " + direction);
    }

    #endregion


    #region Private API

    private void BindInputs()
    {
        _gameInputs.Player.Move.performed += HandleMoveInput;
        _gameInputs.Player.Move.canceled += HandleMoveInput;

        _gameInputs.Player.Look.performed += HandleLookInput;
        _gameInputs.Player.Look.canceled += HandleLookInput;

        _gameInputs.Player.Aim.performed += HandleAimInput;
        _gameInputs.Player.Aim.canceled += HandleAimInput;

        _gameInputs.Player.Shoot.performed += HandleShootInput;
        //_gameInputs.Game.Shoot.canceled += HandleShootInput;
    }

    private void UnBindInputs()
    {
        _gameInputs.Player.Move.performed -= HandleMoveInput;
        _gameInputs.Player.Move.canceled -= HandleMoveInput;

        _gameInputs.Player.Look.performed -= HandleLookInput;
        _gameInputs.Player.Look.canceled -= HandleLookInput;

        _gameInputs.Player.Aim.performed -= HandleAimInput;
        _gameInputs.Player.Aim.canceled -= HandleAimInput;

        _gameInputs.Player.Shoot.performed -= HandleShootInput;
        //_gameInputs.Game.Shoot.canceled -= HandleShootInput;
    }

    private void UpdateMovement(float delta)
    {
        Vector3 cameraForward = _camera.transform.forward;
        Vector3 cameraRight = _camera.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        Vector3 movement3D = cameraForward * _movementDirection.y + cameraRight * _movementDirection.x;

        _movement.Move(movement3D, delta, _isAiming);

        _previousMovementDirection = movement3D;
    }

    private void HandleMoveInput(InputAction.CallbackContext context)
    {
        _movementDirection = context.ReadValue<Vector2>();
    }

    private void HandleLookInput(InputAction.CallbackContext context)
    {
        _cameraLookInput = context.ReadValue<Vector2>();
    }

    private void UpdateCameraLook(float delta)
    {
        if (_cameraLookInput == Vector2.zero)
            return;

        Vector2.ClampMagnitude(_cameraLookInput, 1);
        _camera.Look(_cameraLookInput);
    }

    private void HandleAimInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _isAiming = true;
            Debug.Log("Aim");
        }
        else if (context.canceled)
        {
            _isAiming = false;
            Debug.Log("Release Aim");
        }
    }

    private void HandleShootInput(InputAction.CallbackContext context)
    {
        if (!_isAiming)
            return;

        Vector3 crossHair = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        _aimTargetRay = Camera.main.ScreenPointToRay(crossHair);

        _shoot.Shoot(_aimTargetRay);
    }

    #endregion

}
