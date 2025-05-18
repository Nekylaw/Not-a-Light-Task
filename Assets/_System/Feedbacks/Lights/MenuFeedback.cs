using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   private float baseSize;

   private void Start()
   {
      baseSize = GetComponent<TextMeshProUGUI>().fontSize;
   }

   public void OnPointerEnter(PointerEventData eventData)
   {
      Debug.Log("OnPointerEnter");
      //this.GetComponent<TextMeshProUGUI>().transform.DOScale(1.3f, 0.2f);
      
      gameObject.transform.DOScale(1.3f, 0.2f);
   }

   public void OnPointerExit(PointerEventData eventData)
   {
      gameObject.transform.DOScale(1f, 0.2f);
   }
}
