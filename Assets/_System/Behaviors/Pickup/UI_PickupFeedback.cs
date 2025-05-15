using UnityEngine;
using UnityEngine.UI;

public class UI_PickupFeedback : MonoBehaviour
{

    [SerializeField]
    private Image _pickUpImage = null;

    private PickUpBehaviorComponent _pickupBehavior = null;

    void Awake()
    {
        _pickupBehavior = GetComponentInParent<PickUpBehaviorComponent>();
        _pickUpImage.enabled = false;
    }

    private void OnEnable()
    {
        _pickupBehavior.OnPickableInRange += ShowFeedback;
        _pickupBehavior.OnPickableOutOfRange += HideFeedback;
    }


    private void OnDisable()
    {
        if (_pickupBehavior == null)
            return;
        _pickupBehavior.OnPickableInRange -= ShowFeedback;
        _pickupBehavior.OnPickableOutOfRange -= HideFeedback;

    }
    private void ShowFeedback(PickableComponent pickable)
    {
        _pickUpImage.enabled = true;
    }

    private void HideFeedback()
    {
        _pickUpImage.enabled = false;
    }
}
