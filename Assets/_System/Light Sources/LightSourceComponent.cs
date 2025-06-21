using _System.Game_Manager;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Services.LightSources
{
    [System.Serializable]
    public class LightSourceComponent : MonoBehaviour, ICullable
    {
        #region Fields

        private LightSourcesService _lightService = null;

        [SerializeField]
        private LightSourceSettings _settings = null;

        [SerializeField]
        private Transform _lightPoint = null;

        private int _orbSlot = 0;
        private bool _isLightOn = false;

        public int LightGroupId;

        private VisualEffect ownParticlesVFX;
        private ParticleSystem _particleSystem = null;

        private float _detectionTimer = 0f;
        private const float DETECTION_INTERVAL = 0.2f;

        // Cache pour les buildings
        private bool _buildingsDetected = false;

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
                _particleSystem.Stop();
            if (ownParticlesVFX != null)
                ownParticlesVFX.enabled = false;
        }

        private void Update()
        {
            if (isCulled || _isLightOn)
                return;

            _detectionTimer += Time.deltaTime;
            if (_detectionTimer >= DETECTION_INTERVAL)
            {
                _detectionTimer = 0f;
                DetectOrb();
            }
        }

        private void OnEnable()
        {
            _lightService = LightSourcesService.Instance;
            Register();

            if (CullingService.CullingService.Instance == null)
                return;

            CullMode = ICullable.ECullMode.Frustum;
            CullRange = Mathf.Max(_settings.AttractRange, _settings.BrightnessRange) + 2f;

            CullingService.CullingService.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (CullingService.CullingService.Instance != null)
                CullingService.CullingService.Instance.Unregister(this);

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

            if (!isCulled)
            {
                if (ownParticlesVFX != null)
                    ownParticlesVFX.enabled = true;
                if (_particleSystem != null)
                    _particleSystem.Play();
            }

            if (!_buildingsDetected)
            {
                DetectBuildingsToLights();
                _buildingsDetected = true;
            }

            return true;
        }

        internal bool SwitchOff()
        {
            if (!_isLightOn)
                return false;

            _isLightOn = false;
            _buildingsDetected = false;

            if (ownParticlesVFX != null)
                ownParticlesVFX.enabled = false;
            if (_particleSystem != null)
                _particleSystem.Stop();

            return true;
        }

        #endregion

        #region Private API

        private bool DetectOrb()
        {
            if (_isLightOn)
                return false;

            Collider[] colliders = new Collider[10]; // 10 is rly engough
            int count = Physics.OverlapSphereNonAlloc(_lightPoint.position, _settings.AttractRange, colliders, _settings.OrbLayer);

            if (count <= 0)
                return false;

            bool orbDetected = false;
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].TryGetComponent(out OrbComponent orb))
                {
                    orb.AttractTo(_lightPoint.position, this);
                    orbDetected = true;
                }
            }

            return orbDetected;
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
            var buildings = Physics.OverlapSphere(transform.position, _settings.BrightnessRange, _settings.BuildingLayer);

            foreach (var building in buildings)
            {
                if (building.TryGetComponent<BuildingLightsComponent>(out var buildingLights))
                {
                    buildingLights.LightBuilding();
                }
                else if (building.TryGetComponent<EnlightTower>(out var tower))
                {
                    tower.LightTower();
                }
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (_lightPoint == null || _settings == null)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_lightPoint.position, _settings.AttractRange);

            Gizmos.color = Settings.DebugColor;
            Gizmos.DrawWireSphere(_lightPoint.position, _settings.BrightnessRange);
        }

        #endregion

        #region CULL

        public ICullable.ECullMode CullMode { get; set; }
        public float CullRange { get; set; }
        public int CullableIndex { get; set; }

        private bool isCulled;

        public void OnBecomeVisible()
        {
            isCulled = false;

            if (_isLightOn)
            {
                if (ownParticlesVFX != null)
                    ownParticlesVFX.enabled = true;
                if (_particleSystem != null && !_particleSystem.isPlaying)
                    _particleSystem.Play();
            }
        }

        public void OnBecomeInvisible()
        {
            isCulled = true;

            if (ownParticlesVFX != null)
                ownParticlesVFX.enabled = false;
            if (_particleSystem != null)
                _particleSystem.Pause();
        }

        public Vector3 GetCullPosition() => transform.position;
        public float GetCullRadius() => CullRange;

        #endregion
    }
}