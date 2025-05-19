using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuFeedback : MonoBehaviour, ISelectHandler, IDeselectHandler//, IPointerEnterHandler, IPointerExitHandler
{
   [SerializeField] private GameObject player;

   private void Start()
   {
   }

  /* public void OnPointerEnter(PointerEventData eventData)
   {
      Debug.Log("OnPointerEnter");
      //this.GetComponent<TextMeshProUGUI>().transform.DOScale(1.3f, 0.2f);
      
      gameObject.transform.DOScale(1.3f, 0.2f);
   }

   public void OnPointerExit(PointerEventData eventData)
   {
      gameObject.transform.DOScale(1f, 0.2f);
   }*/

   public void ActivateManually()
   {
      gameObject.transform.DOScale(1.3f, 0.2f);
   }

   public void DeactivateManually()
   {
      gameObject.transform.DOScale(1f, 0.2f);
   }

   public void OnSelect(BaseEventData eventData)
   {
      ActivateManually();
   }

   public void OnDeselect(BaseEventData eventData)
   {
      DeactivateManually();
   }
   
}
