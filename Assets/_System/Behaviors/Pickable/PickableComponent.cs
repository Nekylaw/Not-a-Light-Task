using Services.Behaviors;
using System.Collections;
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
        Debug.Log($"PickableComponent: {gameObject.name}");
        BehaviorsService.Pickup(this);
        //Destroy(gameObject); 
     /*   gameObject.SetActive(false); *///@todo Use object pooling instead of destroying

        return true;
    }

    public IEnumerator AnimatePickup(Transform target, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t);

            transform.position = Vector3.Lerp(startPos, target.position, t);
            //transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t); // smoothly scale down to zero

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target.position;
    }


    public bool Release() { return false; }

    internal void DisplayFeedback()
    {
    }
}
