using UnityEngine;

public class PickUpBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private PickupSettings _settings;

    private PickableComponent pickableInRange = null;

    private OrbContainerComponent _container = null;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogWarning($"{nameof(PickupSettings)} asset not found.");

        if (!TryGetComponent<OrbContainerComponent>(out _container))
            Debug.LogWarning($"{nameof(OrbContainerComponent)} component not found.");
    }



    private void Update()
    {
        pickableInRange = GetPickableInRange();
    }

    private PickableComponent GetPickableInRange()
    {
        var colliders = Physics.OverlapSphere(transform.position, _settings.PickupRange, _settings.PickableLayer);

        if (colliders.Length <= 0)
            return null; ;

        float minDist = float.MaxValue;
        PickableComponent closest = null;
        foreach (Collider collider in colliders)
        {
            if (!collider.TryGetComponent<PickableComponent>(out PickableComponent p))
                continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist >= minDist)
                continue;

            minDist = dist;
            closest = p;
        }

        return closest;
    }

    public bool Pickup()
    {
        if (pickableInRange == null)
            return false;
        Debug.Log("Pickup");

        return pickableInRange.Pickup(_container);
    }
}
