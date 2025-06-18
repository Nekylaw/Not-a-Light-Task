using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PacifyBehaviourComponent : MonoBehaviour
{
    #region Delegates

    public delegate void StartPacifyDelegate(CreatureController creature);
    public event StartPacifyDelegate OnPacifyStart;

    public delegate void EndPacifyDelegate(CreatureController creature);
    public event EndPacifyDelegate OnPacifyEnd;
    #endregion

    #region Fields  
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [Header("Detection")]
    [SerializeField] private LayerMask _creatureLayer;
    [SerializeField] private float _pacifyRadius = 5f;

    [Header("Pacify Settings")]
    [SerializeField] private float _pacifyDuration = 2f;
    [SerializeField] private float _floatHeight = 1.5f;
    [SerializeField] private float _floatDuration = 0.5f;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private AnimationCurve _pacifyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem _pacifyParticles;
    [SerializeField] private AudioClip _pacifyStartSound;
    [SerializeField] private AudioClip _pacifyCompleteSound;
    [SerializeField] private AudioClip _pacifyCancelSound;

    // State
    private bool _canPacify = false;
    private bool _isPacifyPerforming = false;
    private bool _canceled = false;
    private CreatureController _nearestCreature;
    private CreatureController _targetCreature;

    // Pacify progress
    private float _pacifyTimer = 0f;
    private Coroutine _pacifyCoroutine;

    // Original creature values for restoration
    private Vector3 _originalCreaturePosition;
    private Quaternion _originalCreatureRotation;
    private Rigidbody _creatureRigidbody;
    private bool _originalKinematicState;

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
        _nearestCreature = FindNearestCreature();
    }
    #endregion

    #region Public Methods
    public bool Pacify()
    {
        if (_isPacifyPerforming)
            return false;

        _canPacify = CanPacify();

        if (!_canPacify)
            return false;

        _isPacifyPerforming = true;
        _canceled = false;
        _targetCreature = _nearestCreature;

        if (_pacifyCoroutine != null)
            StopCoroutine(_pacifyCoroutine);

        _pacifyCoroutine = StartCoroutine(PacifyCoroutine(_targetCreature));
        OnPacifyStart?.Invoke(_targetCreature);

        return true;
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
        if (_targetCreature != null)
        {
            RestoreCreatureState();
        }

        // Play cancel sound
        PlaySound(_pacifyCancelSound);

        // Stop particles
        if (_pacifyParticles != null)
            _pacifyParticles.Stop();

        ResetPacifyState();
    }

    private IEnumerator PacifyCoroutine(CreatureController creature)
    {
        // Initialize
        _pacifyTimer = 0f;

        // Store original creature state
        StoreCreatureState();

        creature.ChangeState(CreatureController.ECreatureState.Pacifying);

        // Immobilize creature
        ImmobilizeCreature();

        // Play start sound
        PlaySound(_pacifyStartSound);

        // Start particles
        if (_pacifyParticles != null)
        {
            _pacifyParticles.transform.position = creature.transform.position;
            _pacifyParticles.Play();
        }

        // Start floating animation
        Sequence floatSequence = DOTween.Sequence();
        floatSequence.Append(creature.transform.DOMoveY(_originalCreaturePosition.y + _floatHeight, _floatDuration)
            .SetEase(Ease.OutQuad));

        // Rotation during pacify
        Tween rotationTween = creature.transform.DORotate(new Vector3(0, 360, 0), 60f / _rotationSpeed, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1);

        // Pacify progress
        while (_pacifyTimer < _pacifyDuration && !_canceled)
        {
            _pacifyTimer += Time.deltaTime;
            float progress = _pacifyTimer / _pacifyDuration;

            // Update progress
            UpdatePacifyProgress(progress);

            // Update particle position
            if (_pacifyParticles != null)
                _pacifyParticles.transform.position = creature.transform.position;

            yield return null;
        }

        // Kill rotation tween
        rotationTween.Kill();

        if (!_canceled)
        {
            // Pacify successful
            CompletePacify();
        }
        else
        {
            // Already handled in CancelPacify
        }

        _pacifyCoroutine = null;
    }

    private void CompletePacify()
    {
        if (_targetCreature == null)
        {
            ResetPacifyState();
            return;
        }

        // Play complete sound
        PlaySound(_pacifyCompleteSound);

        // Create completion effect
        Sequence completeSequence = DOTween.Sequence();

        // Quick spin and scale
        completeSequence.Append(_targetCreature.transform.DORotate(new Vector3(0, 720, 0), 0.5f, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));
        completeSequence.Join(_targetCreature.transform.DOScale(1.2f, 0.25f)
            .SetEase(Ease.OutQuad));
        completeSequence.Append(_targetCreature.transform.DOScale(1f, 0.25f)
            .SetEase(Ease.InQuad));

        // Return to ground
        completeSequence.Append(_targetCreature.transform.DOMoveY(_originalCreaturePosition.y, 0.5f)
            .SetEase(Ease.InQuad));

        completeSequence.OnComplete(() =>
        {
            // Restore physics
            RestoreCreaturePhysics();

            // Pacify
            _targetCreature.CompletePacify();

            OnPacifyEnd?.Invoke(_targetCreature);

            // Stop particles
            if (_pacifyParticles != null)
                _pacifyParticles.Stop();

            ResetPacifyState();
        });
    }

    private void StoreCreatureState()
    {
        if (_targetCreature == null)
            return;

        _originalCreaturePosition = _targetCreature.transform.position;
        _originalCreatureRotation = _targetCreature.transform.rotation;

        _creatureRigidbody = _targetCreature.GetComponent<Rigidbody>();
        if (_creatureRigidbody != null)
        {
            _originalKinematicState = _creatureRigidbody.isKinematic;
        }
    }

    private void ImmobilizeCreature()
    {
        if (_targetCreature == null)
            return;

        // Disable physics
        if (_creatureRigidbody != null)
        {
            _creatureRigidbody.isKinematic = true;
            _creatureRigidbody.linearVelocity = Vector3.zero;
            _creatureRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreCreatureState()
    {
        if (_targetCreature == null)
            return;

        // Kill any active tweens on the creature
        _targetCreature.transform.DOKill();

        // Restore position and rotation
        _targetCreature.transform.position = _originalCreaturePosition;
        _targetCreature.transform.rotation = _originalCreatureRotation;

        // Restore physics
        RestoreCreaturePhysics();

        // Let creature return to wandering
        _targetCreature.ChangeState(CreatureController.ECreatureState.Wandering);
    }

    private void RestoreCreaturePhysics()
    {
        if (_creatureRigidbody != null)
        {
            _creatureRigidbody.isKinematic = _originalKinematicState;
        }
    }

    private void ResetPacifyState()
    {
        _isPacifyPerforming = false;
        _canceled = false;
        _targetCreature = null;
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

    private bool CanPacify()
    {
        if (_isPacifyPerforming)
            return false;

        if (_nearestCreature == null)
            return false;

        return !_nearestCreature.IsPacified &&
               _nearestCreature.CurrentState != CreatureController.ECreatureState.Eating;
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
        if (!debugMode)
            return;

        // Draw pacify radius
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _pacifyRadius);

        // Highlight nearest creature
        if (_nearestCreature != null)
        {
            Gizmos.color = _nearestCreature.IsPacified ? Color.gray : Color.cyan;
            Gizmos.DrawLine(transform.position, _nearestCreature.transform.position);
            Gizmos.DrawWireCube(_nearestCreature.transform.position, Vector3.one * 0.5f);
        }

        // Show active pacify target
        if (_isPacifyPerforming && _targetCreature != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetCreature.transform.position, 1f);
        }
    }
    #endregion
}