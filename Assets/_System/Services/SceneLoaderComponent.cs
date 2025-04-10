using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scenes
{
    public class SceneLoaderComponent : MonoBehaviour
    {

        #region Fields

        private SceneLoadingService _sceneLoadingService = null;

        private Coroutine _sceneProcessCoroutine = null;

        private bool _isProcessing;

        private Queue<string> _scenesToLoad = new Queue<string>();
        private Queue<string> _scenesToUnload = new Queue<string>();

        #endregion


        #region Lifecycle

        public static SceneLoaderComponent Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Évite d'avoir des doublons
                return;
            }

            Instance = this;
        }

        internal void Init(SceneLoadingService sceneLoadingService)
        {
            _sceneLoadingService = sceneLoadingService;
            Debug.Log("Init Loader comp" + "ref is null? " + (_sceneLoadingService == null));

            //DontDestroyOnLoad(gameObject);
            Debug.Log("Init Loader comp");
        }

        #endregion


        #region Public API

        public bool IsProcessing => _isProcessing;

        public void LoadScene(string scene)
        {
            if (string.IsNullOrEmpty(scene))
                return;

            _scenesToLoad.Enqueue(scene);
            ProcessScenes();
        }

        public void UnloadScene(string scene)
        {
            if (string.IsNullOrEmpty(scene))
                return;

            _scenesToUnload.Enqueue(scene);
            ProcessScenes();
        }

        #endregion


        #region Internal API

        private void ProcessScenes()
        {
            if (_sceneProcessCoroutine != null)
                return;

            if (_scenesToUnload.Count > 0)
            {
                string scene = _scenesToUnload.Dequeue();
                _sceneProcessCoroutine = StartCoroutine(InternalUnloadScene(scene));
            }
            else if (_scenesToLoad.Count > 0)
            {
                string scene = _scenesToLoad.Dequeue();
                _sceneProcessCoroutine = StartCoroutine(InternalLoadScene(scene));
            }
        }

        private IEnumerator InternalUnloadScene(string scene)
        {
            if (!SceneManager.GetSceneByName(scene).isLoaded || string.IsNullOrEmpty(scene))
            {
                Debug.LogWarning($"Scene {scene} is not loaded, or name is invalid. Cannot unload");
                yield break;
            }

            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);

            while (!op.isDone)
                yield return null;

            _sceneProcessCoroutine = null;
            ProcessScenes();
        }

        private IEnumerator InternalLoadScene(string scene)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

            while (!op.isDone)
                yield return null;

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));

            _sceneProcessCoroutine = null;
            ProcessScenes();
        }

        #endregion

    }
}