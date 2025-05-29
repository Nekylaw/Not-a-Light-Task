using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Services.LightSources
{
    public class LightChargerComponent : MonoBehaviour
    {
        [SerializeField]
        private List<LightSourceComponent> _lights = new();

        private HashSet<LightSourceComponent> _triggeredLights = new();
        private bool _hasActivated = false;

        private void Awake()
        {
            if (_lights == null || _lights.Count == 0)
                _lights = GetComponentsInChildren<LightSourceComponent>().ToList();

            foreach (var light in _lights)
            {
                //light.AllowLight(false);
            }
        }

        private void OnEnable()
        {
            LightSourcesService.Instance.OnTriggerLight += HandleTriggerLight;
        }

        private void OnDisable()
        {
            LightSourcesService.Instance.OnTriggerLight -= HandleTriggerLight;
        }

        private void HandleTriggerLight(LightSourceComponent triggered)
        {
            if (_hasActivated)
                return;

            if (!_lights.Contains(triggered))
                return;

            _triggeredLights.Add(triggered);

            if (_triggeredLights.Count >= _lights.Count)
                ActivateAllLights();
        }

        private void ActivateAllLights()
        {
            _hasActivated = true;

            foreach (var light in _lights)
            {
                //light.AllowLight(true);

                if (!light.IsLightOn)
                    LightSourcesService.Instance.SwitchOn(light);
            }
        }
    }
}
