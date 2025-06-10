using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles detection and interaction with nearby PickableComponents.
/// </summary>
public class PickUpBehaviorComponent : MonoBehaviour
{
    #region Subclasses

    class OrbAttraction
    {
        public PickableComponent Orb;
        public Vector3 StartPos;
        public float Elapsed;
        public float Duration;
    }

    #endregion

    #region Events

    public delegate void PickupDelegate(PickableComponent pickableComponent);
    public event PickupDelegate OnPickup = null;

    public delegate void HoldPickupDelegate();
    public event HoldPickupDelegate OnHoldPickup = null;

    public delegate void ReleasePickupDelegate();
    public event ReleasePickupDelegate OnReleasePickup = null;

    public delegate void PickableInRangeDelegate(PickableComponent pickableComponent);
    public delegate void PickableOutOfRangeDelegate();

    public event PickableInRangeDelegate OnPickableInRange = null;
    public event PickableOutOfRangeDelegate OnPickableOutOfRange = null;

    #endregion

    #region Serialized Fields

    [SerializeField] private PickupSettings _settings;
    [SerializeField] private Transform _orbAttractionPoint;

    #endregion

    #region Private Fields

    private OrbContainerComponent _container;
    private readonly List<OrbAttraction> _activeAttractions = new();


    private bool _isHoldingPickup = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_settings == null)
            Debug.LogWarning($"{nameof(PickupSettings)} asset not assigned.");

        if (!TryGetComponent(out _container))
            Debug.LogWarning($"{nameof(OrbContainerComponent)} component not found.");
    }

    private void Update()
    {
        if (_isHoldingPickup)
        {
            DetectOrbs();
            UpdateOrbAttractions(Time.deltaTime);
            OnHoldPickup?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_settings == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _settings.PickupRange);
    }

    #endregion

    #region Attraction Logic

    public void StartAttracting()
    {
        _isHoldingPickup = true;
    }

    public void StopAttracting()
    {
        _isHoldingPickup = false;
        foreach (var attracttion in _activeAttractions)
        {
            if (attracttion.Orb.TryGetComponent<Rigidbody>(out var rb))
                rb.linearVelocity = Vector3.zero;
        }
        _activeAttractions.Clear(); // stop all mid-animation

        OnReleasePickup?.Invoke();
    }

    private void DetectOrbs()
    {
        var colliders = Physics.OverlapSphere(transform.position, _settings.PickupRange, _settings.PickableLayer);

        foreach (var collider in colliders)
        {
            if (!collider.TryGetComponent(out PickableComponent pickable)) continue;
            if (_activeAttractions.Exists(x => x.Orb == pickable)) continue;

            _activeAttractions.Add(new OrbAttraction
            {
                Orb = pickable,
                StartPos = pickable.transform.position,
                Duration = _settings.Duration,
                Elapsed = 0f
            });
        }
    }

    private void UpdateOrbAttractions(float deltaTime)
    {
        for (int i = _activeAttractions.Count - 1; i >= 0; i--)
        {
            OrbAttraction attraction = _activeAttractions[i];
            if (attraction.Orb == null)
            {
                _activeAttractions.RemoveAt(i);
                continue;
            }

            attraction.Elapsed += deltaTime;
            float t = Mathf.Clamp01(attraction.Elapsed / attraction.Duration);
            float progress = _settings.AttractCurve.Evaluate(t);

            attraction.Orb.transform.position = Vector3.LerpUnclamped(attraction.StartPos, _orbAttractionPoint.position, progress);

            if (t >= 0.9f)
            {
                if (attraction.Orb.Pickup(_container))
                {
                    OnPickup?.Invoke(attraction.Orb);
                    Destroy(attraction.Orb.gameObject);
                }
                _activeAttractions.RemoveAt(i);
            }
        }
    }

    #endregion
}
