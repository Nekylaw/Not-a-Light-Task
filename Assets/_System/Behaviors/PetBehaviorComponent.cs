using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PetBehaviorComponent : MonoBehaviour
{
    #region Delegates

    public delegate void StartPetDelegate(CreatureController creature);
    public event StartPetDelegate OnPetStart;

    public delegate void UpdatePetDelegate();
    public event UpdatePetDelegate OnPetUpdate;

    public delegate void EndPetDelegate(CreatureController creature, bool success);
    public event EndPetDelegate OnPetEnd;

    public delegate void PettableAssignedDelegate(CreatureController creature);
    public event PettableAssignedDelegate OnPettableAssigned;

    #endregion

    #region Fields

    [Header("Detection")]
    [SerializeField] private float _radius = 5f;
    [SerializeField] private LayerMask _creatureLayer = ~0;

    [Header("Pet (pas le prout)")]
    [SerializeField] private float _petDuration = 2f;
    [SerializeField] private AnimationCurve _petCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Pettable System")]
    [SerializeField] private float _pettableCheckInterval = 15f;
    [SerializeField] private int _minPacifiedCreaturesRequired = 2;
    [SerializeField] private float _pettableSearchRadius = 20f;

    [Header("Visual")]
    [SerializeField] private float _petAnimationScale = 1.2f;
    [SerializeField] private float _petAnimationSpeed = 3f;
    [SerializeField] private ParticleSystem _petParticles;
    [SerializeField] private GameObject _pettableIndicatorPrefab;

    [Header("Debug")]
    [SerializeField] private bool _debugMode = false;
    [SerializeField] private bool _showGizmos = true;

    #endregion

    // State
    private CreatureController _nearest;
    private CreatureController _targetCreature;
    private CreatureController _currentPettableCreature;
    private Coroutine _petCoroutine;
    private bool _isPetting;
    private float _petTimer;

    // Pettable system
    private float _nextPettableCheckTime;
    private GameObject _pettableIndicator;

    // Base state for animation
    private Vector3 _baseScale;
    private Vector3 _basePosition;

    // Anim
    private Sequence _petSequence;

    public float PetDuration => _petDuration;
    public float PetTimer => _petTimer;
    public bool IsPetting => _isPetting;
    public CreatureController Nearest => _nearest;
    public CreatureController CurrentPettableCreature => _currentPettableCreature;

    #region Lifecycle

    private void Awake()
    {
        _nextPettableCheckTime = Time.time + _pettableCheckInterval;
    }

    private void Update()
    {
        _nearest = FindNearestPettableCreature();

        // Check for assign a pettable creature
        if (Time.time >= _nextPettableCheckTime)
        {
            CheckAndAssignPettableCreature();
            _nextPettableCheckTime = Time.time + _pettableCheckInterval;
        }

        // Update pettable indicator position
        UpdatePettableIndicator();
    }

    private void OnDestroy()
    {
        if (_pettableIndicator != null)
            Destroy(_pettableIndicator);
    }

    #endregion

    #region Public API

    public bool Pet()
    {
        if (_debugMode)
            Debug.Log($"Attempting to pet. IsPetting: {_isPetting}, Nearest: {_nearest?.name}, IsPettable: {_nearest?.IsPettable}");

        if (_isPetting || _petCoroutine != null)
            return false;

        if (_nearest == null || !CanPet(_nearest))
            return false;

        _targetCreature = _nearest;
        _isPetting = true;
        _petTimer = 0f;

        _petCoroutine = StartCoroutine(nameof(PerformPetCoroutine));
        return true;
    }

    public void StopPet()
    {
        if (!_isPetting || _petCoroutine == null)
            return;

        CancelPet();
    }

    #endregion

    #region Private API

    private void CheckAndAssignPettableCreature()
    {
        // If there's already a pettable creature in range, skip
        if (_currentPettableCreature != null && IsCreatureInRange(_currentPettableCreature, _radius))
            return;

        // Find all pacified creatures in the larger search radius
        List<CreatureController> pacifiedCreatures = FindPacifiedCreaturesInRadius(_pettableSearchRadius);

        if (_debugMode)
            Debug.Log($"Found {pacifiedCreatures.Count} pacified creatures for petting");

        // Check if we have enough pacified creatures
        if (pacifiedCreatures.Count < _minPacifiedCreaturesRequired)
        {
            ClearCurrentPettable();
            return;
        }

        // Exclude creature that was last pettable
        List<CreatureController> eligibleCreatures = pacifiedCreatures.Where(c => !c.WasLastPettable).ToList();


        // If no eligible creatures, we might need to reset all WasLastPettable flags
        if (eligibleCreatures.Count == 0 && pacifiedCreatures.Count >= _minPacifiedCreaturesRequired)
        {
            if (_debugMode)
                Debug.Log("No eligible creatures found, resetting all WasLastPettable flags");

            foreach (var creature in pacifiedCreatures)
            {
                creature.SetWasLastPettable(false);
            }

            eligibleCreatures = pacifiedCreatures;
        }

        // Select one to be pettable
        CreatureController newPettable = SelectPettableCreature(eligibleCreatures);

        if (newPettable != null)
        {
            AssignPettableCreature(newPettable);
        }
    }

    private CreatureController SelectPettableCreature(List<CreatureController> candidates)
    {
        // Remove current pettable from candidates if it exists
        if (_currentPettableCreature != null)
            candidates.Remove(_currentPettableCreature);

        if (candidates.Count == 0)
            return null;

        // Select the closest one
        //CreatureController selected = candidates.OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).First();

        // Select randomly
        CreatureController selected = candidates[Random.Range(0, candidates.Count)];

        return selected;
    }

    public void AssignPettableCreature(CreatureController creature)
    {
        // Clear previous pettable
        ClearCurrentPettable();

        // Assign new pettable
        _currentPettableCreature = creature;
        _currentPettableCreature.SetPettable(true);

        // Create visual indicator
        if (_pettableIndicatorPrefab != null && _pettableIndicator == null)
        {
            _pettableIndicator = Instantiate(_pettableIndicatorPrefab);
        }

        if (_debugMode)
            Debug.Log($"Assigned {creature.name} as pettable creature");

        OnPettableAssigned?.Invoke(creature);
    }

    private void ClearCurrentPettable()
    {
        if (_currentPettableCreature != null)
        {
            _currentPettableCreature.SetPettable(false);
            _currentPettableCreature.SetWasLastPettable(true);
            _currentPettableCreature = null;
        }

        if (_pettableIndicator != null)
        {
            Destroy(_pettableIndicator);
            _pettableIndicator = null;
        }
    }

    private void UpdatePettableIndicator()
    {
        if (_pettableIndicator != null && _currentPettableCreature != null)
        {
            // Position indicator above pettable creature
            Vector3 indicatorPos = _currentPettableCreature.transform.position + Vector3.up * 2f;
            _pettableIndicator.transform.position = indicatorPos;

            // Optional: Make it bob or rotate
            float bobOffset = Mathf.Sin(Time.time * 2f) * 0.1f;
            _pettableIndicator.transform.position += Vector3.up * bobOffset;
            _pettableIndicator.transform.Rotate(Vector3.up * 50f * Time.deltaTime);
        }
    }

    private List<CreatureController> FindPacifiedCreaturesInRadius(float radius)
    {
        List<CreatureController> pacifiedCreatures = new List<CreatureController>();

        Collider[] creaturesInRange = Physics.OverlapSphere(transform.position, radius, _creatureLayer);

        foreach (Collider col in creaturesInRange)
        {
            CreatureController creature = col.GetComponent<CreatureController>();
            if (creature != null && creature.IsPacified)
            {
                pacifiedCreatures.Add(creature);
            }
        }

        return pacifiedCreatures;
    }

    private bool IsCreatureInRange(CreatureController creature, float range)
    {
        if (creature == null)
            return false;

        return Vector3.Distance(transform.position, creature.transform.position) <= range;
    }

    private void CancelPet()
    {
        if (_petCoroutine != null)
        {
            StopCoroutine(_petCoroutine);
            _petCoroutine = null;
        }

        if (_targetCreature != null)
        {
            RestoreCreatureState();
        }

        if (_petParticles != null)
            _petParticles.Stop();

        if (_targetCreature != null)
            OnPetEnd?.Invoke(_targetCreature, false);

        ResetPetState();
    }

    private void ResetPetState()
    {
        _isPetting = false;
        _targetCreature = null;
        _petTimer = 0f;
        _petCoroutine = null;
    }

    private bool CanPet(CreatureController creature)
    {
        // Can only pet the designated pettable creature
        return creature != null &&
               creature == _currentPettableCreature &&
               creature.IsPettable &&
               creature.CurrentState != CreatureController.ECreatureState.Petting;
    }

    private IEnumerator PerformPetCoroutine()
    {
        if (_targetCreature == null)
        {
            ResetPetState();
            yield break;
        }

        _basePosition = _targetCreature.transform.position;
        _baseScale = _targetCreature.transform.localScale;

        // Change creature state
        _targetCreature.ChangeState(CreatureController.ECreatureState.Petting);
        OnPetStart?.Invoke(_targetCreature);

        // Particle System
        if (_petParticles != null)
        {
            _petParticles.transform.position = _targetCreature.transform.position;
            _petParticles.Play();
        }

        // Animation
        _petSequence = DOTween.Sequence();

        _petSequence.Append(_targetCreature.transform.DOScale(_baseScale * _petAnimationScale, 0.3f)
            .SetEase(Ease.OutQuad));
        _petSequence.SetLoops(-1, LoopType.Yoyo);

        float startY = _basePosition.y;
        _targetCreature.transform.DOMoveY(startY + 0.2f, 1f / _petAnimationSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Pet progress
        while (_petTimer < _petDuration && _isPetting)
        {
            _petTimer += Time.deltaTime;
            float progress = _petTimer / _petDuration;

            UpdatePetProgress(progress);

            if (_petParticles != null && _targetCreature != null)
                _petParticles.transform.position = _targetCreature.transform.position;

            OnPetUpdate?.Invoke();
            yield return null;
        }

        _petSequence.Kill();
        _targetCreature.transform.DOKill();

        if (_isPetting && _petTimer >= _petDuration)
            CompletePet();

        _petCoroutine = null;
    }

    private void CompletePet()
    {
        if (_targetCreature == null)
        {
            ResetPetState();
            return;
        }

        _targetCreature.SetWasLastPettable(true);

        // Animation 
        _petSequence = DOTween.Sequence();
        _petSequence.Append(_targetCreature.transform.DOShakeRotation(0.5f, 10f, 10, 90f));
        _petSequence.Join(_targetCreature.transform.DOScale(_baseScale * 1.3f, 0.2f)
            .SetEase(Ease.OutQuad));
        _petSequence.Append(_targetCreature.transform.DOScale(_baseScale, 0.3f)
            .SetEase(Ease.InBack));

        _petSequence.OnComplete(() =>
        {
            _targetCreature.CompletePet();

            // Stop particles
            if (_petParticles != null)
                _petParticles.Stop();

            // Fire complete event
            OnPetEnd?.Invoke(_targetCreature, true);

            ResetPetState();
        });
    }

    private void RestoreCreatureState()
    {
        if (_targetCreature == null)
            return;

        _targetCreature.transform.DOKill();

        _targetCreature.transform.localScale = _baseScale;
        _targetCreature.transform.position = _basePosition;

        _targetCreature.ChangeState(CreatureController.ECreatureState.Pacified);
    }

    private void UpdatePetProgress(float progress)
    {
        float t = _petCurve.Evaluate(progress);
    }

    private CreatureController FindNearestPettableCreature()
    {
        // Only look for the designated pettable creature in range
        if (_currentPettableCreature == null || !IsCreatureInRange(_currentPettableCreature, _radius))
            return null;

        if (CanPet(_currentPettableCreature))
            return _currentPettableCreature;

        return null;
    }

    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!_debugMode || !_showGizmos)
            return;

        // Draw pet radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
        Gizmos.DrawWireSphere(transform.position, _radius);

        // Draw larger search radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f); // Lighter orange
        Gizmos.DrawWireSphere(transform.position, _pettableSearchRadius);

        // Highlight current pettable creature
        if (_currentPettableCreature != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, _currentPettableCreature.transform.position);
            Gizmos.DrawWireSphere(_currentPettableCreature.transform.position, 1.5f);
        }

        // Highlight nearest pettable creature
        if (_nearest != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_nearest.transform.position, Vector3.one * 0.8f);
        }

        // Show active pet target
        if (_isPetting && _targetCreature != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_targetCreature.transform.position, 1.2f);
        }
    }
    #endregion
}