using Services.Behaviors;
using UnityEngine;

public class PickableComponent : MonoBehaviour
{
    //@todo pickable settings
    public int lightAmmoRetrived = 0;


    public int LightAmmoRetrived => lightAmmoRetrived;

    public bool Pickup(OrbContainerComponent container)
    {
        if (container == null)
            return false;

        container.CollectBullet(lightAmmoRetrived);
        Destroy(gameObject); //@todo pooling

        BehaviorsService.Pickup(this);
        Debug.Log("Pickup success");

        return true;
    }

    public bool Release() { return false; }

}
