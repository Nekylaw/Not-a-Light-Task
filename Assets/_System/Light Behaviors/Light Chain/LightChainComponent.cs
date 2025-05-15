using System.Linq;

using UnityEngine;

namespace Game.Services.LightSources
{
    public class LightChainComponent : MonoBehaviour
    {
        [SerializeField]
        private LightSourceComponent[] _lights = null;

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
            if (!_lights.Contains(triggeredLight))
                return;

            foreach (LightSourceComponent light in _lights)
            {
                if (light.IsLightOn)
                    continue;

                LightSourcesService.Instance.SwitchOn(light);
            }
        }
    }
}
