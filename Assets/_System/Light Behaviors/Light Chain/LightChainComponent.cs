using System.Collections;
using System.Linq;
using UnityEngine;

namespace Game.Services.LightSources
{
    public class LightChainComponent : MonoBehaviour
    {
        public enum LightDelayMode
        {
            Simultaneous,
            Sequential,
            Random
        }

        [Header("Light Chain Settings")]
        [SerializeField]
        private LightSourceComponent[] _lights = null;

        [SerializeField]
        private float _delayBetweenLights = 0.3f;

        [SerializeField]
        private LightDelayMode _lightMode = LightDelayMode.Sequential;

        private Coroutine _chainRoutine;

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

            if (_chainRoutine != null)
                return;

            _chainRoutine = StartCoroutine(ChainLightSequence(triggeredLight));
        }

        private IEnumerator ChainLightSequence(LightSourceComponent triggeredLight)
        {
            var chainList = _lights
                .Where(l => l != null && l != triggeredLight && !l.IsLightOn)
                .ToList();

            chainList.Insert(0, triggeredLight);

            switch (_lightMode)
            {
                case LightDelayMode.Simultaneous:
                    foreach (var light in chainList)
                    {
                        LightSourcesService.Instance.SwitchOn(light);
                    }
                    break;

                case LightDelayMode.Sequential:
                    foreach (var VARIABLE in _lights)
                    {
                        LightSourcesService.Instance.SwitchOn(VARIABLE);
                        yield return new WaitForSeconds(_delayBetweenLights);
                    }
                    break;

                case LightDelayMode.Random:
                    var randomList = chainList.OrderBy(_ => Random.value).ToList();

                    foreach (var light in randomList)
                    {
                        LightSourcesService.Instance.SwitchOn(light);
                        yield return new WaitForSeconds(_delayBetweenLights);
                    }
                    break;
            }

            _chainRoutine = null;
        }

    }
}
