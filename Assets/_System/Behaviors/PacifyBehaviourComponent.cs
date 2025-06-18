using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PacifyBehaviourComponent : MonoBehaviour
{
    #region Delegates
    public delegate void PacifyDelegate(CreatureController creature);
    public event PacifyDelegate OnPacify;
    #endregion

    #region Fields  
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [Header("UI")]
    [SerializeField] private GameObject pacifyUI;

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

    // Base values
    private Vector3 _originalCreaturePosition;
    private Quaternion _originalCreatureRotation;
    private Rigidbody _creatureRb;
    private bool _originalKinematicState;

    #endregion

    #region Lifecycle

    void Awake()
    {
        //Init Audio
    }

    void Update()
    {
        // Update UI position if pacifying
        if (_isPacifyPerforming && pacifyUI != null && _targetCreature != null)
        {
            UpdatePacifyUI();
        }

        // Debug visualization
        if (debugMode)
        {
            _nearestCreature = FindNearestCreature();
        }
    }

    #endregion

    #region Public API

    public bool Pacify()
    {
        Debug.LogWarning("Pacify creature...");

        if (_isPacifyPerforming)
            return false;

        _nearestCreature = FindNearestCreature();
        _canPacify = CanPacify();

        if (!_canPacify)
            return false;

        StartPacify();
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
    private void StartPacify()
    {
        _isPacifyPerforming = true;
        _canceled = false;
        _targetCreature = _nearestCreature;

        if (_pacifyCoroutine != null)
            StopCoroutine(_pacifyCoroutine);

        _pacifyCoroutine = StartCoroutine(PacifyCoroutine());
    }

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
        PlaySound();

        // Hide UI
        if (pacifyUI != null)
            pacifyUI.SetActive(false);

        // Stop particles
        if (_pacifyParticles != null)
            _pacifyParticles.Stop();

        ResetState();
    }

    private IEnumerator PacifyCoroutine()
    {
        // Initialize
        _pacifyTimer = 0f;

        // Store original creature state
        StoreCreatureState();

        // Immobilize creature
        ImmobilizeCreature();

        // Show UI
        if (pacifyUI != null)
            pacifyUI.SetActive(true);

        // Play start sound
        PlaySound();

        // Start particles
        if (_pacifyParticles != null)
        {
            _pacifyParticles.transform.position = _targetCreature.transform.position;
            _pacifyParticles.Play();
        }

        // Start floating animation
        Sequence floatSequence = DOTween.Sequence();
        floatSequence.Append(_targetCreature.transform.DOMoveY(_originalCreaturePosition.y + _floatHeight, _floatDuration)
            .SetEase(Ease.OutQuad));

        // Rotation during pacify
        Tween rotationTween = _targetCreature.transform.DORotate(new Vector3(0, 360, 0), 60f / _rotationSpeed, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1);

        // Set creature to pacifying state
        _targetCreature.ChangeState(CreatureController.ECreatureState.Pacified);
        _targetCreature.StartPacify();

        // Pacify progress
        while (_pacifyTimer < _pacifyDuration && !_canceled)
        {
            _pacifyTimer += Time.deltaTime;
            float progress = _pacifyTimer / _pacifyDuration;

            // Update UI progress
            UpdatePacifyProgress(progress);

            // Update particle position
            if (_pacifyParticles != null)
                _pacifyParticles.transform.position = _targetCreature.transform.position;

            yield return null;
        }

        // Kill rotation tween
        rotationTween.Kill();

        if (!_canceled)
        {
            // Pacify successful
            CompletePacify();
        }

        _pacifyCoroutine = null;
    }

    private void CompletePacify()
    {
        if (_targetCreature == null)
        {
            ResetState();
            return;
        }

        _targetCreature.ChangeState(CreatureController.ECreatureState.Pacified);

        // Play complete sound
        PlaySound();

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
            // Actually pacify the creature
            _targetCreature.ApplyPacifyState();

            // Fire event
            OnPacify?.Invoke(_targetCreature);

            // Hide UI
            if (pacifyUI != null)
                pacifyUI.SetActive(false);

            // Stop particles
            if (_pacifyParticles != null)
                _pacifyParticles.Stop();

            ResetState();
        });
    }

    private void StoreCreatureState()
    {
        if (_targetCreature == null)
            return;

        _originalCreaturePosition = _targetCreature.transform.position;
        _originalCreatureRotation = _targetCreature.transform.rotation;

        _creatureRb = _targetCreature.GetComponent<Rigidbody>();
    }

    private void ImmobilizeCreature()
    {
        if (_targetCreature == null)
            return;

        // Force creature to idle state
        _targetCreature.ChangeState(CreatureController.ECreatureState.Idle);

        // Disable physics
        if (_creatureRb != null)
        {
            _creatureRb.linearVelocity = Vector3.zero;
            _creatureRb.angularVelocity = Vector3.zero;
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

        // Let creature return to wandering
        _targetCreature.ChangeState(CreatureController.ECreatureState.Wandering);
    }


    private void ResetState()
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

    private void UpdatePacifyUI()
    {
        if (pacifyUI == null || _targetCreature == null)
            return;

        // Position UI above creature
        Vector3 uiPosition = _targetCreature.transform.position + Vector3.up * 3f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(uiPosition);

        if (screenPos.z > 0)
        {
            pacifyUI.transform.position = screenPos;
        }
    }

    private void UpdatePacifyProgress(float progress)
    {
        // Apply curve to progress for non-linear feel
        float curvedProgress = _pacifyCurve.Evaluate(progress);

        // @todo update pacify feedback 
    }

    private void PlaySound(/* sound event */)
    {

    }

    #endregion

    #region Debug

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