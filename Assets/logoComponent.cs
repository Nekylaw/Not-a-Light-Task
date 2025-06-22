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
        gameObject.transform.DOScale(new Vector3(35f, 35f, 35f), 1f);
    }

    private void Update()
    {
        if (gameObject.transform.localScale.x >= 1.2f)
        {
            gameObject.transform.DOScale(new Vector3(20f,20f,20f) , 2f);
        }
        
        
        var targetImage = gameObject.GetComponent<Image>(); 
        Color color = targetImage.color;
        targetImage.color = color;

        targetImage.DOFade(0f, 3.1f);
        StartCoroutine(kill());
    }

    private IEnumerator kill()
    {
        yield return new WaitForSecondsRealtime(3f);
        canvas.SetActive(false);
    }
}
