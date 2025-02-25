using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    #region Nested

    public enum InputMode
    {
        Controller,
        KeyBoard
    }

    #endregion

    #region Fields

    private GameInputs _gameInputs = null;

    private CameraController _camera = null;
    private MovementBehaviorComponent _movement = null;
    private ShootBehaviorComponent _shoot = null;
    private JumpBehaviorComponent _jump = null;

    private Vector3 _movementDirection = Vector3.zero;
    private Vector3 _previousMovementDirection = Vector3.zero;
    private Vector2 _cameraLookInput = Vector2.zero;
    private bool _isAiming = false;
    private Ray _aimTargetRay;


    private InputMode _inputMode = InputMode.Controller;

    #endregion


    #region Lifecycle

    private void Awake()
    {
        if (!TryGetComponent<MovementBehaviorComponent>(out _movement))
            Debug.LogError($"{nameof(MovementBehaviorComponent)} component not found.", this);

        if (!TryGetComponent<ShootBehaviorComponent>(out _shoot))
            Debug.LogError($"{nameof(ShootBehaviorComponent)} component not found", this);

        if (!TryGetComponent<JumpBehaviorComponent>(out _jump))
            Debug.LogError($"{nameof(JumpBehaviorComponent)} component not found", this);

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
        UpdateCameraLook(_inputMode, delta);
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

        _gameInputs.Player.Jump.started += HandleJumpInput;
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

        _gameInputs.Player.Jump.started -= HandleJumpInput;
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
        //Debug.Log("Device : " + context.control.device);


        _inputMode = context.control.device is Keyboard ? InputMode.KeyBoard : InputMode.Controller;
        //Debug.Log("Mode : " + _inputMode);
        _cameraLookInput = context.ReadValue<Vector2>();
    }

    private void UpdateCameraLook(InputMode mode, float delta)
    {
        if (_cameraLookInput == Vector2.zero)
            return;

        Vector2.ClampMagnitude(_cameraLookInput, 1);
        _camera.Look(mode, _cameraLookInput);
    }

    private void HandleAimInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _isAiming = true;
            //Debug.Log("Aim");
        }
        else if (context.canceled)
        {
            _isAiming = false;
            //Debug.Log("Release Aim");
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

    private void HandleJumpInput(InputAction.CallbackContext context)
    {
        _jump.Jump();
    }

    #endregion

}
