using _System.Game_Manager;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Services.LightSources
{
    [System.Serializable]
    public class LightSourceComponent : MonoBehaviour
    {

        #region Fields

        private LightSourcesService _lightService = null;

        [SerializeField]
        private LightSourceSettings _settings = null;

        [SerializeField]
        private Transform _lightPoint = null;

        //[SerializeField]
        //private bool _isAllowedToLight = false;

        private int _orbSlot = 0;
        private bool _isLightOn = false;

        [SerializeField] public int LightGroupId;

        private VisualEffect ownParticlesVFX;
        private ParticleSystem _particleSystem = null;

        #endregion


        #region Lifecycle

        private void Awake()
        {
            if (_settings == null)
                Debug.LogError($"{nameof(LightSourceSettings)} component not found.");

            ownParticlesVFX = GetComponentInChildren<VisualEffect>();
            _particleSystem = GetComponentInChildren<ParticleSystem>();
        }

        private void Start()
        {
            _orbSlot = 0;
            if (_particleSystem != null)
                _particleSystem.gameObject.SetActive(false);
            if (ownParticlesVFX != null)
                ownParticlesVFX.enabled = false;
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
        public int OrbSlot => _orbSlot;

        public Transform LightPoint => _lightPoint;

        public LightSourceSettings Settings => _settings;

        internal bool SwitchOn()
        {
            SetOrbSlots(1);

            if (!CanLightOn())
                return false;

            _isLightOn = true;

            EndLevelManager.instance.CheckLightSources(this);

            if (ownParticlesVFX != null)
                ownParticlesVFX.enabled = true;
            if (_particleSystem != null)
                _particleSystem.gameObject.SetActive(true);
            DetectBuildingsToLights();
            return true;
        }

        internal bool SwitchOff()
        {
            if (!_isLightOn)
                return false;

            _isLightOn = false;

            if (ownParticlesVFX != null)
                ownParticlesVFX.enabled = false;
            if (_particleSystem != null)
                _particleSystem.gameObject.SetActive(false);
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

        //public void AllowLight(bool allow)
        //{
        //    _isAllowedToLight = allow;
        //}

        private bool CanLightOn()
        {
            if (_isLightOn /*|| !_isAllowedToLight*/)
                return false;

            return _orbSlot >= _settings.RequiredOrbs;
        }

        public void SetOrbSlots(int amount)
        {
            _orbSlot += amount;
        }

        public void DrainLight()
        {
            if (!_isLightOn)
                return;

            _orbSlot = 0;
            _lightService.SwitchOff(this);
        }

        private void DetectBuildingsToLights()
        {
            var buildings = Physics.OverlapSphere(transform.position, 40f);
            foreach (var building in buildings)
            {
                var script = building.gameObject.GetComponent<BuildingLightsComponent>();
                if (script != null)
                {
                    script.LightBuilding();
                }
                
                var script2 = building.gameObject.GetComponent<EnlightTower>();
                if (script2 != null)
                {
                    script2.LightTower();
                }
                
            }
        }

        #endregion


        #region Debug

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_lightPoint.position, _settings.AttractRange);

            Gizmos.color = Settings.DebugColor;
            Gizmos.DrawWireSphere(_lightPoint.position, _settings.BrightnessRange);
        }

        #endregion

    }
}