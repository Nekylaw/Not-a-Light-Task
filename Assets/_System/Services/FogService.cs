using System;
using System.Collections;

using UnityEngine;

using Game.Services.LightSources;

namespace Game.Services.Fog
{

    public sealed class FogService : Service, IDisposable
    {

        #region Singleton

        private static FogService _instance;

        public static FogService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FogService();

                return _instance;
            }
        }

        public FogService() { }

        #endregion


        #region Delegates

        public delegate void FogDissipationStartDelegate(LightSourceComponent light, float baseRadius);

        public delegate void FogDissipationUpdateDelegate(LightSourceComponent light);

        public delegate void FogDissipationFinishDelegate(LightSourceComponent light);

        #endregion


        #region Fields

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
            Debug.Log("In registier  Initialization : " + nameof(FogService));

            if (_lightService == null)
            {
                Debug.Log($"{nameof(LightSourcesService)} Service not found ");
                return;
            }

            _lightService.OnSwitchOnLight += HandleLightOn;
            _lightService.OnSwitchOffLight += HandleLightOff;
        }

        private void Unregister()
        {
            if (_lightService == null)
                return;

            _lightService.OnSwitchOnLight -= HandleLightOn;
            _lightService.OnSwitchOffLight -= HandleLightOff;
        }

        private void HandleLightOn(LightSourceComponent light)
        {
            Debug.Log("FOg Handle Light before lenght");

            if (_lightService.LightSources.Length <= 0)
                return;

            Debug.Log("FOg Handle Light");

            int index = Array.IndexOf(_lightService.LightSources, light);
            _clearZonesDatas[index] = new ClearZoneData(light.LightPoint, 0);

            OnFogDissipationStart?.Invoke(light, _clearZonesDatas[index].Radius);
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

        public ClearZoneData[] ClearZonesDatas => _clearZonesDatas ?? new ClearZoneData[0];

        public int MaxDissipationZones => _maxDissipationZones;

        public override IEnumerator Init()
        {
            Debug.Log("GameManager Initialization : " + nameof(FogService));

            _lightService = LightSourcesService.Instance;

            if (_lightService == null)
                yield break;

            _maxDissipationZones = _lightService.LightSourceCount;
            _clearZonesDatas = new ClearZoneData[_maxDissipationZones];

            Register();
            IsServiceInitialized = true;

            yield break;
        }

        public override void Tick(float delta)
        {
            //@todo 
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Unregister();
            _clearZonesDatas = null;
            _disposed = true;
        }

        #endregion

    }
}
