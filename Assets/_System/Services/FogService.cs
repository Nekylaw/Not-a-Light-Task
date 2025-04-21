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

        //private ClearZoneData[] _clearZonesDatas;

        //private int _maxDissipationZones = 0;

        private bool _disposed = false;

        #endregion


        #region Private API

        private void Register()
        {
            if (_lightService == null)
            {
                Debug.Log($"{nameof(LightSourcesService)} Service not found ");
                return;
            }

            Debug.Log("Fog Register Light");

            _lightService.OnSwitchOnLight += HandleLightOn;
            _lightService.OnSwitchOffLight += HandleLightOff;


            OnFogDissipationStart += (l, r) => Debug.Log("Test dissip");
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
            if (_lightService.LightSources.Length <= 0)
                return;

            //if (_clearZonesDatas == null || _clearZonesDatas.Length <= 0)
            //    _clearZonesDatas = new ClearZoneData[MaxDissipationZones];

            //int index = Array.IndexOf(_lightService.LightSources, light);
            //_clearZonesDatas[index] = new ClearZoneData(light.LightPoint, 10);
            //OnFogDissipationStart?.Invoke(light, _clearZonesDatas[index].Radius);
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

        public int MaxDissipationZones => LightSourcesService.Instance.TotalLightSources;
        //public ClearZoneData[] ClearZonesDatas => _clearZonesDatas;
        public override IEnumerator Init()
        {
            Debug.Log("GameManager Initialization : " + nameof(FogService));

            _lightService = LightSourcesService.Instance;

            if (_lightService == null)
                yield break;

            Register();

            IsServiceInitialized = true;
        }

        public override void Tick(float delta)
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Unregister();
            //_clearZonesDatas = null;
            _disposed = true;
        }

        #endregion

    }
}
