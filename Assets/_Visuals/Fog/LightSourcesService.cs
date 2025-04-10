using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Services.LightSources
{
    public sealed class LightSourcesService : Service, IDisposable
    {

        #region Singleton

        private static LightSourcesService _instance;

        public static LightSourcesService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LightSourcesService();

                return _instance;
            }
        }

        public LightSourcesService() { }

        #endregion


        #region Delegates

        public delegate void SwitchOnLightDelegate(LightSourceComponent light);

        public delegate void SwitchOffLightDelegate(LightSourceComponent light);

        #endregion


        #region Fields

        public event SwitchOnLightDelegate OnSwitchOnLight = null;
        public event SwitchOffLightDelegate OnSwitchOffLight = null;

        private List<LightSourceComponent> _lightSourceList = new List<LightSourceComponent>();

        private bool _initialized = false;
        private bool _isDisposed = false;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="IService.Init"/>
        public override IEnumerator Init()
        {
            Debug.Log("GameManager Initialization : " + nameof(LightSourcesService));

            IsServiceInitialized = true;
            yield break;
        }

        public bool RegisterLightSource(LightSourceComponent source)
        {
            if (_lightSourceList.Contains(source))
                return false;

            _lightSourceList.Add(source);
            //Debug.Log($"Init {nameof(LightSourceComponent)} register ");

            //Debug.Log("Ligth service light count: ");
            return true;
        }

        public bool UnregisterLightSource(LightSourceComponent source)
        {
            if (!_lightSourceList.Contains(source))
                return false;

            _lightSourceList.Remove(source);
            return true;
        }

        /// <inheritdoc cref="IService.Tick(float)"/>
        public override void Tick(float delta)
        {
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
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

        public bool SwitchOn(LightSourceComponent light)
        {
            if (!light.SwitchOn())
                return false;

            Debug.Log(message: "Light Light invoke");

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

        #endregion

    }
}