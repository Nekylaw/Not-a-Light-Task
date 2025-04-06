using System;
using System.Collections;

using UnityEngine;

using Game.Services.LightSources;

namespace Game.Services.Fog
{

    public class FogService : IService, IDisposable
    {

        #region Singleton

        private static FogService _instance;

        public static FogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FogService();
                    Debug.Log("Creating Fog");
                }
                return _instance;
            }
        }

        #endregion


        #region Delegates

        public delegate void ServiceInitializedDelegate(IService service);

        public delegate void FogDissipationStartDelegate(LightSourceComponent light);

        public delegate void FogDissipationUpdateDelegate(LightSourceComponent light);

        public delegate void FogDissipationFinishDelegate(LightSourceComponent light);

        #endregion


        #region Fields

        public event ServiceInitializedDelegate OnServiceInitialized = null;

        public event FogDissipationStartDelegate OnFogDissipationStart = null;
        public event FogDissipationUpdateDelegate OnFogDissipationUpdate = null;
        public event FogDissipationFinishDelegate OnFogDissipationFinish = null;

        private LightSourcesService _lightService;
        private ClearZoneData[] _clearZonesDatas;

        private int _maxDissipationZones = 0;

        private bool _initialized = false;
        private bool _disposed = false;

        #endregion


        #region Private API

        private void Register()
        {
            _lightService.OnSwitchOnLight += HandleLightOn;
            _lightService.OnSwitchOffLight += HandleLightOff;
        }

        private void Unregister()
        {
            _lightService.OnSwitchOnLight -= HandleLightOn;
            _lightService.OnSwitchOffLight -= HandleLightOff;
        }

        private void HandleLightOn(LightSourceComponent light)
        {

            int index = Array.IndexOf(_lightService.LightSources, light);

            _clearZonesDatas[index] = new ClearZoneData(light.LightPoint, 0);

            OnFogDissipationStart?.Invoke(light);
        }

        private void HandleLightOnUpdate(LightSourceComponent light)
        {
            OnFogDissipationUpdate?.Invoke(light);
            // OnFogDissipationFinish?.Invoke(light); when updtate finished
        }

        private void HandleLightOff(LightSourceComponent light)
        {
        }

        #endregion


        #region Public API

        //public ClearZoneData[] ClearZonesDatas => _clearZonesDatas;
        public int MaxDissipationZones => _maxDissipationZones;

        public bool IsServiceInitialized
        {
            get => _initialized;
            set
            {
                _initialized = value;
                if (_initialized)
                    OnServiceInitialized?.Invoke(this);
                Register();
            }
        }

        public IEnumerator Init()
        {
            Debug.Log("GameManager Initialization : " + nameof(FogRenderer));
            IsServiceInitialized = true;


            _maxDissipationZones = _lightService.LightSourceCount;
            _clearZonesDatas = new ClearZoneData[_maxDissipationZones];

            yield break;
        }

        public void Tick(float delta)
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                Unregister();
                _clearZonesDatas = null;
                _disposed = true;
            }
        }

        #endregion

    }
}
