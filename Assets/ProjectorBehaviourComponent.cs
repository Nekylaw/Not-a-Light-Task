using Game.Services.LightSources;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ProjectorBehaviourComponent : MonoBehaviour
{
    private void OnEnable()
    {
        LightSourcesService.Instance.OnSwitchOnLight += HandleDecalOn;
    }

    private void OnDisable()
    {
        LightSourcesService.Instance.OnSwitchOffLight -= HandleDecalOn;
    }

    private void HandleDecalOn(LightSourceComponent s)
    {
        s.GetComponentInChildren<DecalProjector>().enabled = true;
    }
}