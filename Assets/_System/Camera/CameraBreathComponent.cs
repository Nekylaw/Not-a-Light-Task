using UnityEngine;

public class CameraBreathComponent : MonoBehaviour
{
    public enum TransformTarget { Position, Rotation, Both }

    [Header("Settings")]
    public TransformTarget transformTarget = TransformTarget.Both;

    [Header("Amplitude (offsets)")]
    public Vector3 positionAmplitude = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3 rotationAmplitude = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Frequency (speed of noise)")]
    public float frequency = 1f;

    [Header("Axis enable")]
    public bool XAxis = true;
    public bool YAxis = true;
    public bool ZAxis = true;

    private Vector3 basePos;
    private Vector3 baseRot;

    private Vector3 noiseSeed;

    private void Start()
    {
        basePos = transform.localPosition;
        baseRot = transform.localEulerAngles;

        noiseSeed = new Vector3(
            Random.Range(0f, 100f),
            Random.Range(0f, 100f),
            Random.Range(0f, 100f)
        );
    }

    private void LateUpdate()
    {
        float time = Time.time * frequency;

        Vector3 perlinNoise = new Vector3(
            Mathf.PerlinNoise(noiseSeed.x, time),
            Mathf.PerlinNoise(noiseSeed.y, time),
            Mathf.PerlinNoise(noiseSeed.z, time)
        );

        // Remap from [0,1] to [-1,1]
        perlinNoise = perlinNoise * 2f - Vector3.one;

        Vector3 posOffset = new Vector3(
            XAxis ? perlinNoise.x * positionAmplitude.x : 0f,
            YAxis ? perlinNoise.y * positionAmplitude.y : 0f,
            ZAxis ? perlinNoise.z * positionAmplitude.z : 0f
        );

        Vector3 rotOffset = new Vector3(
            XAxis ? perlinNoise.x * rotationAmplitude.x : 0f,
            YAxis ? perlinNoise.y * rotationAmplitude.y : 0f,
            ZAxis ? perlinNoise.z * rotationAmplitude.z : 0f
        );

        if (Application.isPlaying)
        {
            switch (transformTarget)
            {
                case TransformTarget.Position:
                    transform.localPosition = basePos + posOffset;
                    break;

                case TransformTarget.Rotation:
                    transform.localEulerAngles = baseRot + rotOffset;
                    break;

                case TransformTarget.Both:
                    transform.localPosition = basePos + posOffset;
                    transform.localEulerAngles = baseRot + rotOffset;
                    break;
            }
        }
    }
}
