using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using static PacifyBehaviourComponent;
using System;
using System.Diagnostics;
using UnityEngine.UIElements;

public class PacifyBehaviourComponent : MonoBehaviour
{
    #region Subclasses

    public enum EPacifyMode
    {
        Nearest,
        Zone
    }

    private class CreatureBaseState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsKinematic;
        public Rigidbody Rigidbody;
        public bool HasStartedFloating;
    }

    #endregion

    #region Delegates

    public delegate void StartPacifyDelegate();
    public event StartPacifyDelegate OnPacifyStart;

    public delegate void EndPacifyDelegate();
    public event EndPacifyDelegate OnPacifyEnd;
    #endregion

    #region Fields  
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [Header("Detection")]
    [SerializeField] private LayerMask _creatureLayer;
    [SerializeField] private float _pacifyRadius = 5f;

    [Header("Core Pacify Settings")]
    [SerializeField] private float _pacifyDuration = 2f;
    [SerializeField] private float _floatHeight = 1.5f;
    [SerializeField] private float _floatDuration = 0.5f;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private AnimationCurve _pacifyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Zone Pacify Settings")]
    [SerializeField] private EPacifyMode _pacifyMode = EPacifyMode.Zone;
    [SerializeField] private float _zoneAngle = 10f;
    [SerializeField] private float _zoneRadius = 8f;
    [SerializeField] private int _maxPacifiedCreature = 3;


    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem _pacifyParticles;
    [SerializeField] private AudioClip _pacifyStartSound;
    [SerializeField] private AudioClip _pacifyCompleteSound;
    [SerializeField] private AudioClip _pacifyCancelSound;

    // State
    private bool _isPacifyPerforming = false;
    private bool _canPacify = false;
    private bool _canceled = false;
    private List<CreatureController> _creaturesInRange = new();
    private List<CreatureController> _targetCreatures = new();

    // Pacify progress
    private float _pacifyTimer = 0f;
    private Coroutine _pacifyCoroutine;

    // Original creatures states
    private Dictionary<CreatureController, CreatureBaseState> _originalStates = new Dictionary<CreatureController, CreatureBaseState>();

    // Audio
    private AudioSource _audioSource;
    #endregion

    public float PacifyDuration => _pacifyDuration;


    #region Unity Lifecycle
    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (_pacifyMode == EPacifyMode.Zone)
        {
            _creaturesInRange = FindCreaturesInZone();
        }
        else
        {
            var nearest = FindNearestCreature();
            _creaturesInRange.Clear();
            if (nearest != null)
                _creaturesInRange.Add(nearest);
        }
    }

    #endregion

    #region Public Methods

    public bool Pacify()
    {
        if (_isPacifyPerforming)
            return false;

        var validTargets = GetValidTargets();
        if (validTargets.Count <= 0)
            return false;

        _isPacifyPerforming = true;
        _canceled = false;
        _targetCreatures = validTargets;

        if (_pacifyCoroutine != null)
            StopCoroutine(_pacifyCoroutine);

        _pacifyCoroutine = StartCoroutine(PacifyCoroutine());

        OnPacifyStart?.Invoke();
        return true;
    }

    private List<CreatureController> GetValidTargets()
    {
        var validTargets = new List<CreatureController>();

        foreach (var creature in _creaturesInRange)
        {
            if (creature != null && !creature.IsPacified && !creature.IsBeingPacified && creature.CurrentState != CreatureController.ECreatureState.Eating)
            {
                validTargets.Add(creature);
            }
        }

        switch (_pacifyMode)
        {
            case EPacifyMode.Zone:
                if (validTargets.Count > _maxPacifiedCreature)
                    validTargets.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position).CompareTo(Vector3.Distance(transform.position, b.transform.position)));
                validTargets = validTargets.GetRange(0, Mathf.Min(validTargets.Count, _maxPacifiedCreature));
                break;
            case EPacifyMode.Nearest:
                if (validTargets.Count > 1)
                    validTargets = new List<CreatureController> { validTargets[0] };
                break;
            default:
                break;
        }

        return validTargets;
    }

    public bool StopPacify()
    {
        if (!_isPacifyPerforming)
            return false;

        CancelPacify();
        return true;
    }
    #endregion

    #region Private Methods

    private void CancelPacify()
    {
        _canceled = true;

        if (_pacifyCoroutine != null)
        {
            StopCoroutine(_pacifyCoroutine);
            _pacifyCoroutine = null;
        }

        // Restore creature state
        foreach (var creature in _targetCreatures)
        {
            if (creature != null)
            {
                RestoreCreatureState(creature);
            }
        }

        // Play cancel sound
        PlaySound(_pacifyCancelSound);

        // Stop particles
        if (_pacifyParticles != null)
            _pacifyParticles.Stop();

        ResetPacifyState();
    }

    private IEnumerator PacifyCoroutine()
    {
        // Initialize
        _pacifyTimer = 0f;

        // Store original creature state

        foreach (var creature in _targetCreatures)
        {
            StoreCreatureState(creature);
            creature.ChangeState(CreatureController.ECreatureState.Pacifying);
            ImmobilizeCreature(creature);
        }

        // Play start sound
        PlaySound(_pacifyStartSound);

        foreach (var creature in _targetCreatures)
        {

            if (creature == null || !_originalStates.ContainsKey(creature))
                continue;

            var state = _originalStates[creature];
            state.HasStartedFloating = true;

            // Start floating animation
            creature.transform.DOMoveY(state.Position.y + _floatHeight, _floatDuration)
                .SetEase(Ease.OutQuad);

            // Rotation during pacify
            creature.transform.DORotate(new Vector3(0, 360, 0), 60f / _rotationSpeed, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1);
        }

        // Pacify progress
        while (_pacifyTimer < _pacifyDuration && !_canceled)
        {
            _pacifyTimer += Time.deltaTime;
            float progress = _pacifyTimer / _pacifyDuration;

            // Update progress
            UpdatePacifyProgress(progress);

            // Update particle position
            if (_pacifyParticles != null && _targetCreatures.Count > 0)
            {
                Vector3 centerPos = Vector3.zero;
                int validCount = 0;
                foreach (var creature in _targetCreatures)
                {
                    if (creature != null)
                    {
                        centerPos += creature.transform.position;
                        validCount++;
                    }
                }
                if (validCount > 0)
                    _pacifyParticles.transform.position = centerPos / validCount;
            }

            yield return null;
        }

        // Kill tweens
        foreach (var creature in _targetCreatures)
        {
            if (creature != null)
                creature.transform.DOKill();
        }

        if (!_canceled)
        {
            // Pacify successful
            CompletePacify();
        }

        _pacifyCoroutine = null;
    }

    private void CompletePacify()
    {
        // Play complete sound
        PlaySound(_pacifyCompleteSound);

        int completedCount = 0;

        // Complete pacify for each creature
        foreach (var creature in _targetCreatures)
        {
            if (creature == null) continue;

            // Create completion effect
            Sequence completeSequence = DOTween.Sequence();

            // Quick spin and scale
            completeSequence.Append(creature.transform.DORotate(new Vector3(0, 720, 0), 0.5f, RotateMode.LocalAxisAdd)
                .SetEase(Ease.OutQuad));
            completeSequence.Join(creature.transform.DOScale(1.2f, 0.25f)
                .SetEase(Ease.OutQuad));
            completeSequence.Append(creature.transform.DOScale(1f, 0.25f)
                .SetEase(Ease.InQuad));

            // Return to ground - use the stored original position
            if (_originalStates.ContainsKey(creature))
            {
                var originalPos = _originalStates[creature].Position;
                completeSequence.Append(creature.transform.DOMoveY(originalPos.y, 0.5f)
                    .SetEase(Ease.InQuad));
            }

            completeSequence.OnComplete(() =>
            {
                // Restore physics
                RestoreCreaturePhysics(creature);

                // Pacify
                creature.CompletePacify();

                OnPacifyEnd?.Invoke();

                completedCount++;
                if (completedCount >= _targetCreatures.Count)
                {
                    // All creatures completed
                    if (_pacifyParticles != null)
                        _pacifyParticles.Stop();

                    ResetPacifyState();
                }
            });
        }
    }

    private void StoreCreatureState(CreatureController creature)
    {
        if (creature == null) return;

        var state = new CreatureBaseState
        {
            Position = creature.transform.position,
            Rotation = creature.transform.rotation,
            Rigidbody = creature.GetComponent<Rigidbody>(),
            HasStartedFloating = false
        };

        if (state.Rigidbody != null)
        {
            state.IsKinematic = state.Rigidbody.isKinematic;
        }

        _originalStates[creature] = state;

    }

    private void ImmobilizeCreature(CreatureController creature)
    {
        if (creature == null || !_originalStates.ContainsKey(creature))
            return;

        var state = _originalStates[creature];
        if (state.Rigidbody != null)
        {
            state.Rigidbody.isKinematic = true;
            state.Rigidbody.linearVelocity = Vector3.zero;
            state.Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreCreatureState(CreatureController creature)
    {
        if (creature == null || _originalStates.ContainsKey(creature))
            return;

        // Kill any active tweens on the creature
        creature.transform.DOKill();

        var state = _originalStates[creature];

        // Restore position and rotation to original ground position
        creature.transform.position = state.Position;
        creature.transform.rotation = state.Rotation;

        // Restore physics
        RestoreCreaturePhysics(creature);

        // Creature return to wandering
        creature.ChangeState(CreatureController.ECreatureState.Wandering);
    }

    private void RestoreCreaturePhysics(CreatureController creature)
    {
        if (creature == null || !_originalStates.ContainsKey(creature))
            return;

        var state = _originalStates[creature];
        if (state.Rigidbody != null)
            state.Rigidbody.isKinematic = state.IsKinematic;
    }

    private void ResetPacifyState()
    {
        _isPacifyPerforming = false;
        _canceled = false;
        _targetCreatures.Clear();
        _originalStates.Clear();
        _pacifyTimer = 0f;
    }

    private CreatureController FindNearestCreature()
    {
        Collider[] creaturesInRange = Physics.OverlapSphere(transform.position, _pacifyRadius, _creatureLayer);
        CreatureController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in creaturesInRange)
        {
            CreatureController creature = col.GetComponent<CreatureController>();
            if (creature != null && !creature.IsPacified && creature.CurrentState != CreatureController.ECreatureState.Eating)
            {
                float distance = Vector3.Distance(transform.position, creature.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = creature;
                }
            }
        }

        return nearest;
    }

    private List<CreatureController> FindCreaturesInZone()
    {
        List<CreatureController> creaturesInZone = new List<CreatureController>();
        Collider[] creaturesInRange = Physics.OverlapSphere(transform.position, _zoneRadius, _creatureLayer);

        foreach (var col in creaturesInRange)
        {
            CreatureController creature = col.GetComponent<CreatureController>();
            if (creature == null || creature.IsPacified || creature.CurrentState == CreatureController.ECreatureState.Eating)
                continue;

            // Check if creature is within the zone angle
            Vector3 directionToCreature = (creature.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToCreature);

            if (angle <= _zoneAngle * 0.5) // Half angle cauz forward
            {
                creaturesInZone.Add(creature);
            }
        }

        return creaturesInZone;
    }

    private void UpdatePacifyProgress(float progress)
    {
        float curvedProgress = _pacifyCurve.Evaluate(progress);
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        if (!debugMode) return;

        if (_pacifyMode == EPacifyMode.Zone)
        {
            // Draw zone cone
            Gizmos.color = new Color(0, 1, 1, 0.3f);

            // Draw arc representing the zone
            int segments = 20;
            float angleStep = _zoneAngle / segments;
            float startAngle = -_zoneAngle * 0.5f;

            Vector3 previousPoint = transform.position + Quaternion.Euler(0, startAngle, 0) * transform.forward * _zoneRadius;

            for (int i = 1; i <= segments; i++)
            {
                float currentAngle = startAngle + angleStep * i;
                Vector3 currentPoint = transform.position + Quaternion.Euler(0, currentAngle, 0) * transform.forward * _zoneRadius;

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            // Draw zone boundaries
            Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, -_zoneAngle * 0.5f, 0) * transform.forward * _zoneRadius);
            Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, _zoneAngle * 0.5f, 0) * transform.forward * _zoneRadius);
        }
        else
        {
            // Draw radius
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _pacifyRadius);
        }

        // Highlight creatures in range
        foreach (var creature in _creaturesInRange)
        {
            if (creature != null)
            {
                Gizmos.color = creature.IsPacified ? Color.gray : Color.cyan;
                Gizmos.DrawLine(transform.position, creature.transform.position);
                Gizmos.DrawWireCube(creature.transform.position, Vector3.one * 0.5f);
            }
        }

        // Show active pacify targets
        if (_isPacifyPerforming)
        {
            Gizmos.color = Color.yellow;
            foreach (var creature in _targetCreatures)
            {
                if (creature != null)
                    Gizmos.DrawWireSphere(creature.transform.position, 1f);
            }
        }
    }

    #endregion
}