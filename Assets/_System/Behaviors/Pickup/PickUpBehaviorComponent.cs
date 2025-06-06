using System.Collections;
using UnityEngine;

/// <summary>
/// Handles detection and interaction with nearby PickableComponents.
/// </summary>
public class PickUpBehaviorComponent : MonoBehaviour
{

    public delegate void PickupDelegate(PickableComponent pickableComponent);
    public event PickupDelegate OnPickup = null;

    [SerializeField]
    private PickupSettings _settings;

    [SerializeField]
    private Transform _orbAttractionPoint;

    private PickableComponent _pickableInRange = null;
    private OrbContainerComponent _container = null;

    public delegate void PickableInRangeDelegate(PickableComponent pickableComponent);
    public delegate void PickableOutOfRangeDelegate();

    public event PickableInRangeDelegate OnPickableInRange = null;
    public event PickableOutOfRangeDelegate OnPickableOutOfRange = null;

    private float _lastPickupTime = 0f;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogWarning($"{nameof(PickupSettings)} asset not assigned.");

        if (!TryGetComponent(out _container))
            Debug.LogWarning($"{nameof(OrbContainerComponent)} component not found.");
    }

    public void AttractOrbs()
    {
        var colliders = Physics.OverlapSphere(transform.position, _settings.PickupRange, _settings.PickableLayer);

        foreach (var collider in colliders)
        {
            if (!collider.TryGetComponent(out PickableComponent pickable))
                continue;

            StartCoroutine(AttractAndPickupCoroutine(pickable));
        }
    }

    private IEnumerator AttractAndPickupCoroutine(PickableComponent pickable)
    {
        yield return pickable.AnimatePickup(_orbAttractionPoint, _settings.Duration);

        if (pickable.Pickup(_container))
            OnPickup?.Invoke(pickable);
    }

    private void OnDrawGizmosSelected()
    {
        if (_settings == null) 
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _settings.PickupRange);
    }
}
