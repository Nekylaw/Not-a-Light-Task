using UnityEngine;

namespace Game.Services.LightSources
{
    public class LightSourceComponent : MonoBehaviour
    {

        #region Fields

        private LightSourcesService _lightService = null;

        [SerializeField]
        private Transform _lightPoint = null;

        private bool _isLightOn = false;

        // public orb required to ligth on setting
        public float AttractRange = 0f;
        public float BrightnessRange = 0f;


        #endregion


        #region Lifecycle

        private void Start()
        {
            Debug.Log("@todo light settings asset");
            AttractRange = 2;
            BrightnessRange = 20;
        }

        private void Update()
        {
            DetectBullet();
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

        internal bool SwitchOn()
        {
            if (_isLightOn)
                return false;

            _isLightOn = CanLightOn();

            if (_isLightOn)
                Debug.Log("Light Component ON");

            return _isLightOn;
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

        private bool DetectBullet()
        {
            if (_isLightOn)
                return false;

            Collider[] colliders = Physics.OverlapSphere(_lightPoint.position, AttractRange); //@todo bullet layer

            if (colliders.Length <= 0)
                return false;

            foreach (Collider collider in colliders)
            {
                if (!collider.TryGetComponent(out BulletComponent bullet))
                    continue;

                //Debug.Log("Hittttttt");
                bullet.AttractTo(_lightPoint.position, this);
            }
            return true;
        }

        private bool CanLightOn()
        {
            //@todo check for light on
            return true;
        }

        #endregion


        #region Debug

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lightPoint.position, AttractRange);
        }

        #endregion

    }
}