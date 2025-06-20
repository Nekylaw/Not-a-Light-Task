using DG.Tweening;
using FMOD;
using System.Collections;
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

    #endregion

    #region Fields

    [Header("Detection")]
    [SerializeField] private float _radius = 5f;
    [SerializeField] private LayerMask _creatureLayer = ~0;

    [Header("Pet (pas le prout)")]
    [SerializeField] private float _petDuration = 2f;
    [SerializeField] private AnimationCurve _petCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual")]
    [SerializeField] private float _petAnimationScale = 1.2f;
    [SerializeField] private float _petAnimationSpeed = 3f;
    [SerializeField] private ParticleSystem _petParticles;

    [Header("Debug")]
    [SerializeField] private bool _debugMode = false;
    [SerializeField] private bool _showGizmos = true;

    #endregion

    //State
    private CreatureController _nearest;
    private CreatureController _targetCreature;
    private Coroutine _petCoroutine;
    private bool _isPetting;
    private float _petTimer;

    // Base state for animation
    private Vector3 _baseScale;
    private Vector3 _basePosition;

    // Anim
    private Sequence _petSequence;

    public float PetDuration => _petDuration;
    public float PetTimer => _petTimer;
    public bool IsPetting => _isPetting;
    public CreatureController Nearest => _nearest;

    #region Lifecycle

    private void Update()
    {
        _nearest = FindNearestCreature();
    }

    #endregion

    #region Public API

    public bool Pet()
    {
        UnityEngine.Debug.LogWarning($"@todo use detector for Pet & Pacify");

        if (_isPetting || _petCoroutine != null)
            return false;

        if (_nearest == null || !CanPet(_nearest))
            return false;

        _targetCreature = _nearest;
        _isPetting = true;
        _petTimer = 0f;

        _petCoroutine = StartCoroutine(nameof(PeformPetCoroutine));
        return true;
    }

    public void StopPet()
    {
        if (_petCoroutine == null || _petCoroutine == null)
            return;

        CancelPet();
    }

    #endregion

    #region Private API

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
        _nearest = null;
        _petTimer = 0f;
        _petCoroutine = null;
    }

    private bool CanPet(CreatureController creature)
        => creature != null && creature.IsPacified && creature.CurrentState != CreatureController.ECreatureState.Petting;

    private IEnumerator PeformPetCoroutine()
    {
        if (_nearest == null)
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

    private CreatureController FindNearestCreature()
    {
        Collider[] creaturesInRange = Physics.OverlapSphere(transform.position, _radius, _creatureLayer);
        CreatureController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in creaturesInRange)
        {
            CreatureController creature = col.GetComponent<CreatureController>();
            if (creature == null || !creature.IsPacified)
                continue;

            float distance = Vector3.Distance(transform.position, creature.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = creature;
            }
        }

        return nearest;
    }

    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!_debugMode)
            return;

        // Draw pet radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
        Gizmos.DrawWireSphere(transform.position, _radius);

        // Highlight nearest pettable creature
        if (_nearest != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _nearest.transform.position);
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