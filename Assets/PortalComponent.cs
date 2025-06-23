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

    private PortalsService portalService;

    private void Awake()
    {
        portalService = FindFirstObjectByType<PortalsService>();
    }

    public void OpenGateRotateAround()
    {
        transform.RotateAround(pivot.position, Vector3.up, 150f);
    }

    public void OpenGateProperly()
    {
        portalService.HandlePortalOpenedDelegate(this);

        panelDroit.DOLocalRotate(vecDroit, duration, RotateMode.LocalAxisAdd);
        panelGauche.DOLocalRotate(vecGauche, duration, RotateMode.LocalAxisAdd);
        charniereDroite.DOLocalRotate(vecDroit, duration, RotateMode.LocalAxisAdd);
        charniereGauche.DOLocalRotate(vecGauche, duration, RotateMode.LocalAxisAdd);

        portalService.HandlePortalOpenedDelegate(this);

    }
}
