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

    private PickableComponent _pickableInRange = null;
    private OrbContainerComponent _container = null;

    public delegate void PickableInRangeDelegate(PickableComponent pickableComponent);
    public delegate void PickableOutOfRangeDelegate();

    public event PickableInRangeDelegate OnPickableInRange = null;
    public event PickableOutOfRangeDelegate OnPickableOutOfRange = null;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogWarning($"{nameof(PickupSettings)} asset not assigned.");

        if (!TryGetComponent(out _container))
            Debug.LogWarning($"{nameof(OrbContainerComponent)} component not found.");
    }

    private void Update()
    {
        CheckForNearbyPickables();
    }

    private void CheckForNearbyPickables()
    {
        _pickableInRange = FindClosestPickableInRange();

        if (_pickableInRange != null)
            OnPickableInRange?.Invoke(_pickableInRange);
        else
            OnPickableOutOfRange?.Invoke();
    }

    /// <summary>
    /// Finds the closest PickableComponent within pickup range.
    /// </summary>
    private PickableComponent FindClosestPickableInRange()
    {
        var colliders = Physics.OverlapSphere(transform.position, _settings.PickupRange, _settings.PickableLayer);

        if (colliders.Length == 0)
            return null;

        float minDist = float.MaxValue;
        PickableComponent closest = null;

        foreach (var collider in colliders)
        {
            if (!collider.TryGetComponent(out PickableComponent p))
                continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        return closest;
    }

    /// <summary>
    /// Attempts to pick up the current nearby pickable object.
    /// </summary>
    public bool Pickup()
    {
        if (_pickableInRange == null)
            return false;

        Debug.Log("Pickup");

        if (_pickableInRange.Pickup(_container))
        {
            OnPickup?.Invoke(_pickableInRange);
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (_settings == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _settings.PickupRange);
    }
}
