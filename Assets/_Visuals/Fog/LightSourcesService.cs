using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Services.LightSources
{
    public sealed class LightSourcesService : IService, IDisposable
    {

        #region Singleton

        private static LightSourcesService _instance;

        public static LightSourcesService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LightSourcesService();
                    Debug.Log("Creating Light Service");
                }
                return _instance;
            }
        }

        #endregion


        #region Delegates

        public delegate void ServiceInitializedDelegate(IService service);

        public delegate void SwitchOnLightDelegate(LightSourceComponent light);

        public delegate void SwitchOffLightDelegate(LightSourceComponent light);

        #endregion


        #region Fields

        public event ServiceInitializedDelegate OnServiceInitialized = null;
        public event SwitchOnLightDelegate OnSwitchOnLight = null;
        public event SwitchOffLightDelegate OnSwitchOffLight = null;

        private List<LightSourceComponent> _lightSourceList = new List<LightSourceComponent>();

        private bool _isDisposed = false;

        //@todo delete Test
        [SerializeField]
        private LightSourceComponent _lightTest = null;

        #endregion


        #region Lifecycle

        private LightSourcesService() { }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _lightSourceList.Clear();
            OnSwitchOnLight = null;
            OnSwitchOffLight = null;

            _isDisposed = true;
            _instance = null;
        }

        #endregion


        #region Public API

        public LightSourceComponent[] LightSources => _lightSourceList.ToArray(); //@todo IReadOnlyList<LightSourceComponent> ??

        public int LightSourceCount => _lightSourceList.Count;

        private bool _initialized = false;

        public bool IsServiceInitialized
        {
            get => _initialized; set
            {
                _initialized = value;
                OnServiceInitialized?.Invoke(this);
            }
        }

        public bool SwitchOn(LightSourceComponent light)
        {
            if (!light.SwitchOn())
                return false;

            Debug.Log("Light ON");

            OnSwitchOnLight?.Invoke(light);
            return true;
        }

        public bool SwitchOff(LightSourceComponent light)
        {
            if (!light.SwitchOff())
                return false;

            OnSwitchOffLight?.Invoke(light);
            return true;
        }

        public bool RegisterLightSource(LightSourceComponent source)
        {
            if (_lightSourceList.Contains(source))
                return false;

            _lightSourceList.Add(source);
            Debug.Log($"Init {nameof(LightSourceComponent)} register ");

            Debug.Log("Ligth service light count: " + _lightSourceList.Count);
            return true;
        }

        public bool UnregisterLightSource(LightSourceComponent source)
        {
            if (!_lightSourceList.Contains(source))
                return false;

            _lightSourceList.Remove(source);
            return true;
        }

        public IEnumerator Init()
        {
            Debug.Log("GameManager Initialization : " + nameof(LightSourcesService));

            IsServiceInitialized = true;
            OnServiceInitialized?.Invoke(this);

            yield break;
        }

        public void Tick(float delta)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}