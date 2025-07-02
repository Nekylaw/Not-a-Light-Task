using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class logoComponent : MonoBehaviour
{
    [SerializeField] private GameObject canvas;

    
    private void Start()
    {
        transform.DOScale(new Vector3(35f, 35f, 35f), 1f).OnComplete(() =>
        {
            transform.DOScale(new Vector3(20f, 20f, 20f), 2f);
        });
        
        var targetImage = gameObject.GetComponent<Image>(); 
        
        if (targetImage != null)
        {
            targetImage.DOFade(0f, 3.1f).OnComplete(() =>
            {
                canvas.SetActive(false);
            });
        }
    }
}
