using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditsAppearer : MonoBehaviour
{
    [SerializeField] private logoComponent logoScript;
    [SerializeField] private Image targetImage;
    [SerializeField] private Image image2;
    [SerializeField] private TextMeshProUGUI targetText; 
    [SerializeField] private float fadeDuration = 2.1f;
    [SerializeField] private GameObject canvas;

    public void ResetLogo()
    {
        canvas.SetActive(true);

        if (targetImage != null)
        {
            targetImage.DOFade(.7f, fadeDuration);
            
            image2.DOFade(1f, fadeDuration);
        }

        if (targetText != null)
        {
            targetText.DOFade(1f, fadeDuration);
        }
    }
}
