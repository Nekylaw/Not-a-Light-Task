using System.Net;
using UnityEngine;

public class ActivatorsService : MonoBehaviour
{

    #region Delegates 

    public delegate void TriggerPortalDelegate(PortalComponent portal);
    public event TriggerPortalDelegate OnPortalTriggered;

    public delegate void PortalOpenedDelegate(PortalComponent portal);
    public event PortalOpenedDelegate OnPortalOpened;

    public delegate void ActivateElevatorDelegate(ElevatorComponent elevator);
    public event ActivateElevatorDelegate OnElevatorActivated;

    public delegate void OnRevealStelaDelegate(int stelaIndex);
    public event OnRevealStelaDelegate OnRevealStela;

    #endregion


    #region Handlers

    public void HandleRevealStela(int stelaIndex)
    {
        OnRevealStela?.Invoke(stelaIndex);
    }

    public void HandleActivateElevator(ElevatorComponent elevator)
    {
        OnElevatorActivated?.Invoke(elevator);
    }


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
