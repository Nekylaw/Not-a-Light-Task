using DG.Tweening;
using UnityEngine;

public class PortalComponent : MonoBehaviour
{
    [SerializeField] private Transform pivot;

    [SerializeField] private Transform panelDroit;
    [SerializeField] private Transform panelGauche;
    [SerializeField] private Transform charniereDroite;
    [SerializeField] private Transform charniereGauche;

    [SerializeField] private Vector3 vecDroit = new Vector3(0, 0, 70);
    [SerializeField] private Vector3 vecGauche = new Vector3(0, 0, -70);

    [SerializeField] private float duration;

    private ActivatorsService portalService;

    private void Awake()
    {
        portalService = FindFirstObjectByType<ActivatorsService>();
    }


    public void OpenGateProperly()
    {
        portalService.HandlePortalTriggered(this);

        panelDroit.DOLocalRotate(vecDroit, duration, RotateMode.LocalAxisAdd);
        panelGauche.DOLocalRotate(vecGauche, duration, RotateMode.LocalAxisAdd);
        charniereDroite.DOLocalRotate(vecDroit, duration, RotateMode.LocalAxisAdd);
        charniereGauche.DOLocalRotate(vecGauche, duration, RotateMode.LocalAxisAdd);
    }
}
