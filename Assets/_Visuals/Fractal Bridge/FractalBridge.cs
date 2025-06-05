using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FractalBridge : MonoBehaviour
{
    public enum ETransformTarget { Position, Rotation, Both }

    [Header("Bridge Setup")]
    public List<Transform> Bricks = new List<Transform>();

    [Header("Transform Target Options")]
    public ETransformTarget TransformTarget = ETransformTarget.Both;

    [Header("Amplitude (offsets)")]
    public Vector3 PositionAmplitude = new Vector3(0.01f, 0.01f, 0.01f);
    public Vector3 RotationAmplitude = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Frequency (noise speed)")]
    public float Frequency = 1f;

    [Header("Axes enabled")]
    public bool XAxis = true;
    public bool YAxis = true;
    public bool ZAxis = true;

    [Header("Join Animation Settings")]
    public float JoinDuration = 1.5f;

    private List<Vector3> _basePositions = new List<Vector3>();
    private List<Vector3> _baseRotations = new List<Vector3>();

    private Coroutine _currentCoroutine;
    private bool _isTriggered = false;

    public bool IsTriggered
    {
        get => _isTriggered;
        set
        {
            if (_isTriggered == value)
                return;

            _isTriggered = value;

            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);

            if (_isTriggered)
                _currentCoroutine = StartCoroutine(JoinBricks());
            else
                _currentCoroutine = StartCoroutine(FragmentBricks());
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
        if (_currentCoroutine != null || _isTriggered)
            return;

        float time = Time.time * Frequency;

        for (int i = 0; i < Bricks.Count; i++)
        {
            Transform brick = Bricks[i];

            Vector3 seed = new Vector3(time + i, time + i * 1.3f, time + i * 1.7f);
            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(seed.x, 0f),
                Mathf.PerlinNoise(seed.y, 0f),
                Mathf.PerlinNoise(seed.z, 0f)
            ) * 2f - Vector3.one;

            Vector3 posOffset = new Vector3(
                XAxis ? noise.x * PositionAmplitude.x : 0f,
                YAxis ? noise.y * PositionAmplitude.y : 0f,
                ZAxis ? noise.z * PositionAmplitude.z : 0f
            );

            Vector3 rotOffset = new Vector3(
                XAxis ? noise.x * RotationAmplitude.x : 0f,
                YAxis ? noise.y * RotationAmplitude.y : 0f,
                ZAxis ? noise.z * RotationAmplitude.z : 0f
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

        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> startRotations = new List<Vector3>();

        for (int i = 0; i < Bricks.Count; i++)
        {
            startPositions.Add(Bricks[i].localPosition);
            startRotations.Add(Bricks[i].localEulerAngles);
        }

        while (elapsed < JoinDuration)
        {
            float t = elapsed / JoinDuration;

            for (int i = 0; i < Bricks.Count; i++)
            {
                if (TransformTarget != ETransformTarget.Rotation)
                    Bricks[i].localPosition = Vector3.Lerp(startPositions[i], _basePositions[i], t);

                if (TransformTarget != ETransformTarget.Position)
                    Bricks[i].localEulerAngles = Vector3.Lerp(startRotations[i], _baseRotations[i], t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < Bricks.Count; i++)
        {
            Bricks[i].localPosition = _basePositions[i];
            Bricks[i].localEulerAngles = _baseRotations[i];
        }

        _currentCoroutine = null;
    }

    private IEnumerator FragmentBricks()
    {
        yield return null;

        _currentCoroutine = null;
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>())
        {
            Debug.Log("Bridge triggered by player.");
            IsTriggered = true;
        }
    }
}
