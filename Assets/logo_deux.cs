using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class logo_deux : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var targetImage = gameObject.GetComponent<Image>(); 
        Color color = targetImage.color;
        color.a = 1f;
        targetImage.color = color;
        
        targetImage.DOFade(0f, 2.1f);
    }
}
