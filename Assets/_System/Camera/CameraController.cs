using UnityEngine;

public class CameraController : MonoBehaviour
{

    [SerializeField] private CameraSettings _settings = null;

    [SerializeField] private Transform _player; 
    private float xRotation = 0f;

    void Start()
    {
        if (_player == null)
            Debug.LogError($"{nameof(Transform)} player trarget not found", this);

        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Look(PlayerController.InputMode inputMode, Vector2 look, float delta)
    {
        float inverse = _settings.InverseYaxe ? -1 : 1;

        float pitchSensitivity = inputMode == PlayerController.InputMode.KeyBoard ?
            _settings.MousePitchSensitivity : _settings.ControllerPitchSensitivity;

        float yawSensitivity = inputMode == PlayerController.InputMode.KeyBoard ?
            _settings.MouseYawSensitivity : _settings.ControllerYawSensitivity;

        float pitch = look.y * pitchSensitivity * inverse * delta;
        float yaw = look.x * yawSensitivity * delta;

        xRotation -= pitch;
        xRotation = Mathf.Clamp(xRotation, _settings.RotationLimits.x, _settings.RotationLimits.y);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        _player.Rotate(Vector3.up * yaw);
    }
}
