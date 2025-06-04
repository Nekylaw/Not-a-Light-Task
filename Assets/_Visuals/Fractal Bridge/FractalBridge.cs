using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FractalBridge : MonoBehaviour
{
    public bool _isTriggered = false;

    public List<Transform> Bricks = new List<Transform>();
    private List<Vector3> _basePositions = new List<Vector3>();
    private List<Vector3> _baseRotations = new List<Vector3>();

    public enum ETransformTarget { Position, Rotation, Both }

    [Header("Settings")]
    public ETransformTarget TransformTarget = ETransformTarget.Both;

    [Header("Amplitude (offsets)")]
    public Vector3 PositionAmplitude = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3 RotationAmplitude = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Frequency (speed of noise)")]
    public float Frequency = 1f;

    [Header("Axes enabled")]
    public bool XAxis = true;
    public bool YAxis = true;
    public bool ZAxis = true;

    [Header("Join Animation Settings")]
    public float JoinDuration = 1.5f;

    private Coroutine _moveCoroutine;

    public bool IsTriggered
    {
        get => _isTriggered;
        set
        {
            _isTriggered = value;

            if (_isTriggered)
                _moveCoroutine = StartCoroutine(JoinBricks());

        }
    }

    void Start()
    {
        _basePositions.Clear();
        _baseRotations.Clear();

        foreach (var brick in Bricks)
        {
            _basePositions.Add(brick.localPosition);
            _baseRotations.Add(brick.localEulerAngles);
        }
    }

    void Update()
    {
        if (_moveCoroutine != null)
            return;

        float t = Time.time * Frequency;
        for (int i = 0; i < Bricks.Count; i++)
        {
            var brick = Bricks[i];

            Vector3 noiseSeed = new Vector3(t + i, t + i * 1.3f, t + i * 1.7f);
            Vector3 perlin = new Vector3(
                Mathf.PerlinNoise(noiseSeed.x, 0f),
                Mathf.PerlinNoise(noiseSeed.y, 0f),
                Mathf.PerlinNoise(noiseSeed.z, 0f)
            );

            perlin = perlin * 2f - Vector3.one;

            Vector3 posOffset = new Vector3(
                XAxis ? perlin.x * PositionAmplitude.x : 0f,
                YAxis ? perlin.y * PositionAmplitude.y : 0f,
                ZAxis ? perlin.z * PositionAmplitude.z : 0f
            );

            Vector3 rotOffset = new Vector3(
                XAxis ? perlin.x * RotationAmplitude.x : 0f,
                YAxis ? perlin.y * RotationAmplitude.y : 0f,
                ZAxis ? perlin.z * RotationAmplitude.z : 0f
            );

            switch (TransformTarget)
            {
                case ETransformTarget.Position:
                    brick.localPosition = _basePositions[i] + posOffset;
                    break;

                case ETransformTarget.Rotation:
                    brick.localEulerAngles = _baseRotations[i] + rotOffset;
                    break;

                case ETransformTarget.Both:
                    brick.localPosition = _basePositions[i] + posOffset;
                    brick.localEulerAngles = _baseRotations[i] + rotOffset;
                    break;
            }
        }
    }

    private IEnumerator JoinBricks()
    {
        float elapsed = 0f;

        List<Vector3> startPos = new List<Vector3>();
        List<Vector3> startRot = new List<Vector3>();

        foreach (var brick in Bricks)
        {
            startPos.Add(brick.localPosition);
            startRot.Add(brick.localEulerAngles);
        }

        while (elapsed < JoinDuration)
        {
            float t = elapsed / JoinDuration;
            for (int i = 0; i < Bricks.Count; i++)
            {
                if (TransformTarget != ETransformTarget.Rotation)
                    Bricks[i].localPosition = Vector3.Lerp(startPos[i], _basePositions[i], t);

                if (TransformTarget != ETransformTarget.Position)
                    Bricks[i].localEulerAngles = Vector3.Lerp(startRot[i], _baseRotations[i], t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < Bricks.Count; i++)
        {
            Bricks[i].localPosition = _basePositions[i];
            Bricks[i].localEulerAngles = _baseRotations[i];
        }

        _moveCoroutine = null;
    }
}
