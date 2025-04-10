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

        private Coroutine _sceneProcessRoutine = null;

        private bool _isProcessing;

        private Queue<string> _scenesToLoad = new Queue<string>();
        private Queue<string> _scenesToUnload = new Queue<string>();

        #endregion


        #region Lifecycle
        private void Awake()
        {
            DontDestroyOnLoad(this);
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

        internal void Init(SceneLoadingService sceneLoadingService)
        {
            Debug.Log("Init Loader comp");
            _sceneLoadingService = sceneLoadingService;
        }

        private void ProcessScenes()
        {
            if (_sceneProcessRoutine != null)
                return;

            if (_scenesToUnload.Count > 0)
            {
                string scene = _scenesToUnload.Dequeue();
                _sceneProcessRoutine = StartCoroutine(InternalUnloadScene(scene));
            }
            else if (_scenesToLoad.Count > 0)
            {
                string scene = _scenesToLoad.Dequeue();
                _sceneProcessRoutine = StartCoroutine(InternalLoadScene(scene));
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

            _sceneProcessRoutine = null;
            ProcessScenes();
        }

        private IEnumerator InternalLoadScene(string scene)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive); 
            
            while (!op.isDone)
                        yield return null;

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));

            _sceneProcessRoutine = null;
            ProcessScenes();
        }

        #endregion

    }
}