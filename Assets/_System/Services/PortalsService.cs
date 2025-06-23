using System.Net;
using UnityEngine;

public class PortalsService : MonoBehaviour
{

    #region Delegates 

    public delegate void TriggerPortalDelegate(PortalComponent portal);
    public TriggerPortalDelegate OnPortalTriggered;


    public delegate void PortalOpenedDelegate(PortalComponent portal);
    public PortalOpenedDelegate OnPortalOpened;

    #endregion


    #region Handlers

    public void HandlePortalTriggered(PortalComponent portal)
    {
        OnPortalTriggered?.Invoke(portal);
    }

    public void HandlePortalOpenedDelegate(PortalComponent portal)
    {
        OnPortalOpened?.Invoke(portal);
    }

    #endregion
}
