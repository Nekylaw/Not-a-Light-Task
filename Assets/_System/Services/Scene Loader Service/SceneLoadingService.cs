using System;
using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Services;

namespace Game.Scenes
{
    public class SceneLoadingService : Service, IDisposable
    {

        #region Singleton

        private static SceneLoadingService _instance;

        public static SceneLoadingService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SceneLoadingService();

                return _instance;
            }
        }

        public SceneLoadingService() { }

        #endregion

        #region Delegates

        public delegate void StartLoadingSceneDelegate(string sceneToLoad, string[] scenesToUnload);
        public delegate void EndLoadingDelegate(string loadedScene, string[] unloadedScenes);

        #endregion


        #region Fields

        public const string PersistentSceneName = "SC_Persistent"; // _Scenes/SC_Persistent
        public const string GameSceneName = "SC_Game"; // _Scenes/SC_Game

        public event StartLoadingSceneDelegate OnBeginNavigation;
        public event EndLoadingDelegate OnEndNavigation;

        /// <summary>
        /// The component used to play scene operations coroutines.
        /// </summary>
        private SceneLoaderComponent _sceneLoaderComponent = null;

        private bool _isDisposed = false;

        #endregion


        #region Public API

        /// <inheritdoc cref="SceneLoaderComponent.IsProcessing"/>
        public bool IsProcessing => SceneLoader.IsProcessing;

        public void LoadScene(string scene)
        {
            if (string.IsNullOrEmpty(scene))
                return;

            Debug.Log("_sceneLoaderComponent in Service found? " + (SceneLoader != null));
            Debug.Log("Scene to load " + scene);
            SceneLoader.LoadScene(scene);
            OnBeginNavigation?.Invoke(scene, SceneManager.GetActiveScene().name == scene ? null : new string[] { SceneManager.GetActiveScene().name });

            Debug.Log("Load scene:" + scene);
        }

        public void UnloadScenes(params string[] scenes)
        {
            foreach (string scene in scenes)
                SceneLoader.UnloadScene(scene);
        }

        #endregion


        #region Internal API

        internal void NotifyBeginNavigation(string sceneToLoad, string[] scenesToUnload)
        {
            OnBeginNavigation?.Invoke(sceneToLoad, scenesToUnload);
        }

        internal void NotifyEndNavigation(string loadedScene, string[] unloadedScenes)
        {
            OnEndNavigation?.Invoke(loadedScene, unloadedScenes);
        }

        #endregion


        #region Public API

        public override void Tick(float delta)
        {
        }

        public override IEnumerator Init()
        {
            IsServiceInitialized = true;
            yield return null;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _instance = null;
        }

        #endregion


        #region Private API

        private SceneLoaderComponent SceneLoader
        {
            get
            {
                if (_sceneLoaderComponent == null)
                {
                    if (_sceneLoaderComponent == null)
                    {
                        GameObject obj = new GameObject("Scene Loader");
                        _sceneLoaderComponent = obj.AddComponent<SceneLoaderComponent>();
                    }

                    _sceneLoaderComponent.Init(this);
                }
                return _sceneLoaderComponent;
            }
        }

        #endregion

    }

}