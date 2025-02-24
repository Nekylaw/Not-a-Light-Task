using UnityEngine;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private CameraSettings _settings = null;

    private Transform _player;
    private float xRotation = 0f;

    void Start()
    {
        _player = GetComponentInParent<PlayerController>()?.transform;
        if (_player == null)
            Debug.LogError($"{nameof(PlayerController)} component not found", this);

        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Look(Vector2 look)
    {

        float inverse = _settings.InverseYaxe ? -1 : 1;
        float pitch = look.y * _settings.PitchSensitivity * inverse;
        float yaw = look.x * _settings.YawSensitivity;

        xRotation -= pitch;
        xRotation = Mathf.Clamp(xRotation, _settings.RotationLimits.x, _settings.RotationLimits.y);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        _player.Rotate(Vector3.up * yaw);
    }
}
