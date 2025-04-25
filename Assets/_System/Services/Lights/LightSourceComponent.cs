using System.Data.Common;
using UnityEngine;

namespace Game.Services.LightSources
{
    public class LightSourceComponent : MonoBehaviour
    {

        #region Fields

        private LightSourcesService _lightService = null;

        [SerializeField]
        private LightSourceSettings _settings = null;

        [SerializeField]
        private Transform _lightPoint = null;

        private int _orbSlot = 0;
        private bool _isLightOn = false;

        private bool _isRegistered = false;

        #endregion


        #region Lifecycle

        private void Awake()
        {
            if (_settings == null)
                Debug.LogError($"{nameof(LightSourceSettings)} component not found.");
        }

        private void Start()
        {
            _orbSlot = 0;
            Debug.Log("base Light slots:" + _orbSlot);

        }

        private void Update()
        {
            DetectOrb();
        }

        private void OnEnable()
        {
            _lightService = LightSourcesService.Instance;
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private bool Register()
        {
            if (_lightService == null)
                return false;

            Debug.Log("Light component registered to Light Service ");

            return _lightService.RegisterLightSource(this);
        }

        private bool Unregister()
        {
            if (_lightService == null)
                return false;

            return _lightService.UnregisterLightSource(this);
        }

        #endregion


        #region Public API

        public bool IsLightOn => _isLightOn;

        public Vector3 LightPoint => _lightPoint.position;

        public LightSourceSettings Settings => _settings;

        internal bool SwitchOn()
        {
            if (_isLightOn)
                return false;

            SetOrbSlots(1);

            if (!CanLightOn())
                return false;

            _isLightOn = true;
            return true;
        }

        internal bool SwitchOff()
        {
            if (!_isLightOn)
                return false;

            _isLightOn = false;
            return true;
        }

        #endregion


        #region Private API

        private bool DetectOrb()
        {
            if (_isLightOn)
                return false;

            Collider[] colliders = Physics.OverlapSphere(_lightPoint.position, _settings.AttractRange, _settings.OrbLayer);

            if (colliders.Length <= 0)
                return false;

            foreach (Collider collider in colliders)
            {
                if (!collider.TryGetComponent(out OrbComponent orb))
                    continue;

                orb.AttractTo(_lightPoint.position, this);
            }
            return true;
        }

        private bool CanLightOn()
        {
            if (_isLightOn)
                return false;

            return _orbSlot >= _settings.RequiredOrbs;
        }

        public void SetOrbSlots(int amount)
        {
            _orbSlot += amount;
            Debug.Log("Light slots:" + _orbSlot);
            Debug.Log("Light slots req :" + _settings.RequiredOrbs);
        }

        #endregion


        #region Debug

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lightPoint.position, _settings.AttractRange);
        }

        #endregion

    }
}