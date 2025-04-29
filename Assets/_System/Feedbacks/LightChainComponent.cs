using System.Linq;

using UnityEngine;

namespace Game.Services.LightSources
{
    public class LightChainComponent : MonoBehaviour
    {
        [SerializeField]
        private LightSourceComponent[] _linkedLights = null;

        private void OnEnable()
        {
            LightSourcesService.Instance.OnSwitchOnLight += HandleLightSwitchedOn;
        }

        private void OnDisable()
        {
            LightSourcesService.Instance.OnSwitchOnLight -= HandleLightSwitchedOn;
        }

        private void HandleLightSwitchedOn(LightSourceComponent triggeredLight)
        {
            if (!_linkedLights.Contains(triggeredLight))
                return;

            foreach (LightSourceComponent light in _linkedLights)
            {
                if (light.IsLightOn)
                    continue;

                LightSourcesService.Instance.SwitchOn(light);
            }
        }
    }
}
