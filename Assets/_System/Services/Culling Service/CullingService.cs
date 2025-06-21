using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.CullingService
{
    [DefaultExecutionOrder(-10)]
    public class CullingService : MonoBehaviour, IDisposable
    {
        #region Singleton

        public static CullingService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            //DontDestroyOnLoad(gameObject);

            Initialize();
        }

        #endregion

        private CullingGroup _group;
        private BoundingSphere[] _spheres;
        private readonly List<ICullable> _frustumCullables = new();
        private readonly HashSet<ICullable> _frustumCullablesSet = new();
        private readonly List<ICullable> _rangeCullables = new();
        private readonly HashSet<ICullable> _rangeCullablesSet = new();

        private readonly Queue<int> _freeIndexQueue = new();
        private Vector3 _cachedPlayerPos;
        private int _frameCount;

        private Camera _camera;
        private Transform _player;

        private bool _initialized = false;
        private bool _disposed = false;
        private bool _boundingSpheresNeedUpdate = false;

        private const int RANGE_UPDATE_INTERVAL = 3; 
        private const int INITIAL_SPHERE_CAPACITY = 150;

        private void Initialize()
        {
            _camera = Camera.main;
            if (_camera == null)
                return;

            _group = new CullingGroup();
            _group.targetCamera = _camera;
            _group.onStateChanged += OnFrustumStateChanged;

            _spheres = new BoundingSphere[INITIAL_SPHERE_CAPACITY];
            _group.SetBoundingSpheres(_spheres);
            _group.SetBoundingSphereCount(0);

            _initialized = true;

            Debug.Log("[CullingService] Initialized.");

            _player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include).transform;
        }

        private void Start()
        {
            if (!_initialized)
                Initialize();
        }

        private void Update()
        {
            if (!_initialized)
                return;

            _frameCount++;

            if (_boundingSpheresNeedUpdate)
            {
                UpdateFrustumCullables();
            }

            if (_frameCount % RANGE_UPDATE_INTERVAL == 0 && _rangeCullables.Count > 0)
            {
                UpdateRangeCullables();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_group != null)
            {
                _group.onStateChanged -= OnFrustumStateChanged;
                _group.Dispose();
            }

            _disposed = true;
        }

        public void Register(ICullable cullable)
        {
            if (cullable == null) return;

            switch (cullable.CullMode)
            {
                case ICullable.ECullMode.Frustum:
                    if (_frustumCullablesSet.Contains(cullable)) return;

                    int index;
                    if (_freeIndexQueue.Count > 0)
                    {
                        index = _freeIndexQueue.Dequeue();
                        _frustumCullables[index] = cullable;
                    }
                    else
                    {
                        index = _frustumCullables.Count;
                        _frustumCullables.Add(cullable);

                        if (index >= _spheres.Length)
                        {
                            Array.Resize(ref _spheres, _spheres.Length * 2);
                            _group.SetBoundingSpheres(_spheres);
                        }
                    }

                    cullable.CullableIndex = index;
                    _frustumCullablesSet.Add(cullable);

                    _spheres[index] = new BoundingSphere(cullable.GetCullPosition(), cullable.GetCullRadius());

                    UpdateBoundingSphereCount();
                    break;

                case ICullable.ECullMode.Range:
                    if (_rangeCullablesSet.Contains(cullable)) return;
                    _rangeCullables.Add(cullable);
                    _rangeCullablesSet.Add(cullable);
                    break;
            }
        }

        public void Unregister(ICullable cullable)
        {
            if (cullable == null) return;

            switch (cullable.CullMode)
            {
                case ICullable.ECullMode.Frustum:
                    if (!_frustumCullablesSet.Remove(cullable)) return;

                    int index = cullable.CullableIndex;
                    if (index >= 0 && index < _frustumCullables.Count && _frustumCullables[index] == cullable)
                    {
                        _frustumCullables[index] = null;
                        _freeIndexQueue.Enqueue(index);

                        _spheres[index].radius = 0;

                        UpdateBoundingSphereCount();
                    }
                    break;

                case ICullable.ECullMode.Range:
                    if (_rangeCullablesSet.Remove(cullable))
                    {
                        _rangeCullables.Remove(cullable);
                    }
                    break;
            }
        }

        private void UpdateBoundingSphereCount()
        {
            int activeCount = 0;
            for (int i = 0; i < _frustumCullables.Count; i++)
            {
                if (_frustumCullables[i] != null)
                    activeCount++;
            }

            _group.SetBoundingSphereCount(activeCount);
        }

        private void UpdateFrustumCullables()
        {
            for (int i = 0; i < _frustumCullables.Count; i++)
            {
                var cullable = _frustumCullables[i];
                if (cullable == null) continue;

                _spheres[i].position = cullable.GetCullPosition();
                _spheres[i].radius = cullable.GetCullRadius();
            }

            _boundingSpheresNeedUpdate = false;
        }

        private void UpdateRangeCullables()
        {
            if (_player == null) return;

            _cachedPlayerPos = _player.position;

            foreach (var cullable in _rangeCullables)
            {
                if (cullable == null) continue;

                float sqrDist = (_cachedPlayerPos - cullable.GetCullPosition()).sqrMagnitude;
                float range = cullable.CullRange;

                if (sqrDist <= range * range)
                    cullable.OnBecomeVisible();
                else
                    cullable.OnBecomeInvisible();
            }
        }

        private void OnFrustumStateChanged(CullingGroupEvent evt)
        {
            if (evt.index < 0 || evt.index >= _frustumCullables.Count)
                return;

            var cullable = _frustumCullables[evt.index];
            if (cullable == null) return;

            if (evt.hasBecomeVisible)
                cullable.OnBecomeVisible();
            else if (evt.hasBecomeInvisible)
                cullable.OnBecomeInvisible();
        }

        public void SetPlayer(Transform player)
        {
            _player = player;
        }

    }
}