using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.InputSystem.UI;

public class SliderKeyboardInputs : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private Slider slider;
    private bool isSelected = false;

    private Vector3 baseScale;
    private Vector3 selectedScale = new Vector3(1.3f, 1.3f, 1.3f);

    private GameInputs inputActions;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        baseScale = slider.transform.localScale;
        inputActions = new GameInputs();

        inputActions.UI.Navigate.performed += OnNavigate;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (!isSelected) return;
        Vector2 direction = context.ReadValue<Vector2>();

        if (direction.x > 0 && slider.value < slider.maxValue)
        {
            if (context.started)
            {
                slider.value++;
            }
        }
        else if (direction.x < 0 && slider.value > slider.minValue)
        {
            if (context.started)
            {
                slider.value--;
            }
        }
    }



    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        transform.localScale = selectedScale;
    }
    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        transform.localScale = baseScale;
    }
}
