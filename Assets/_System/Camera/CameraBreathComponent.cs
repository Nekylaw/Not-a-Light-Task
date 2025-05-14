using UnityEngine;

public class CameraBreathComponent : MonoBehaviour
{
    public enum ETransformTarget { Position, Rotation, Both }

    [Header("Settings")]
    public ETransformTarget TransformTarget = ETransformTarget.Both;

    [Header("Amplitude (offsets)")]
    public Vector3 PositionAmplitude = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3 RotationAmplitude = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Frequency (speed of noise)")]
    public float frequency = 1f;

    [Header("Axis enable")]
    public bool XAxis = true;
    public bool YAxis = true;
    public bool ZAxis = true;

    private Vector3 _basePos;
    private Vector3 _baseRot;

    private Vector3 _noiseSeed;

    private void Start()
    {
        _basePos = transform.localPosition;
        _baseRot = transform.localEulerAngles;

        _noiseSeed = new Vector3(
            Random.Range(0f, 100f),
            Random.Range(0f, 100f),
            Random.Range(0f, 100f)
        );
    }

    private void LateUpdate()
    {
        float time = Time.time * frequency;

        Vector3 perlinNoise = new Vector3(
            Mathf.PerlinNoise(_noiseSeed.x, time),
            Mathf.PerlinNoise(_noiseSeed.y, time),
            Mathf.PerlinNoise(_noiseSeed.z, time)
        );

        // Remap from [0,1] to [-1,1]
        perlinNoise = perlinNoise * 2f - Vector3.one;

        Vector3 posOffset = new Vector3(
            XAxis ? perlinNoise.x * PositionAmplitude.x : 0f,
            YAxis ? perlinNoise.y * PositionAmplitude.y : 0f,
            ZAxis ? perlinNoise.z * PositionAmplitude.z : 0f
        );

        Vector3 rotOffset = new Vector3(
            XAxis ? perlinNoise.x * RotationAmplitude.x : 0f,
            YAxis ? perlinNoise.y * RotationAmplitude.y : 0f,
            ZAxis ? perlinNoise.z * RotationAmplitude.z : 0f
        );

        if (Application.isPlaying)
        {
            switch (TransformTarget)
            {
                case ETransformTarget.Position:
                    transform.localPosition = _basePos + posOffset;
                    break;

                case ETransformTarget.Rotation:
                    transform.localEulerAngles = _baseRot + rotOffset;
                    break;

                case ETransformTarget.Both:
                    transform.localPosition = _basePos + posOffset;
                    transform.localEulerAngles = _baseRot + rotOffset;
                    break;
            }
        }
    }
}
