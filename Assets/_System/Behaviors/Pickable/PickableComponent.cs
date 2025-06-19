using UnityEngine;

public class PickableComponent : MonoBehaviour
{
    [SerializeField]
    private PickableSettings _settings = null;


    public int LightAmmoRetrived => _settings.LightAmmoRetrived;

    public bool Pickup(OrbContainerComponent container)
    {
        if (container == null)
            return false;

        container.CollectBullet(_settings.LightAmmoRetrived);
        //Destroy(gameObject); 
     /*   gameObject.SetActive(false); *///@todo Use object pooling instead of destroying

        return true;
    }

    public bool Release() { return false; }
}
