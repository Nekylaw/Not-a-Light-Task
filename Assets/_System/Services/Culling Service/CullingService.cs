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
        private readonly List<ICullable> _rangeCullables = new();

        private Camera _camera;
        private Transform _player;

        private bool _initialized = false;
        private bool _disposed = false;

        private void Initialize()
        {
            _camera = Camera.main;
            if (_camera == null)
                return;


            _group = new CullingGroup();
            _group.targetCamera = _camera;
            _group.onStateChanged += OnFrustumStateChanged;

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

            UpdateFrustumCullables();
            UpdateRangeCullables();
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
            switch (cullable.Mode)
            {
                case ICullable.CullMode.Frustum:
                    if (_frustumCullables.Contains(cullable)) return;
                    cullable.CullableIndex = _frustumCullables.Count;
                    _frustumCullables.Add(cullable);
                    UpdateBoundingSpheres();
                    break;

                case ICullable.CullMode.Range:
                    if (_rangeCullables.Contains(cullable)) return;
                    _rangeCullables.Add(cullable);
                    break;
            }
        }

        public void Unregister(ICullable cullable)
        {
            switch (cullable.Mode)
            {
                case ICullable.CullMode.Frustum:
                    if (_frustumCullables.Remove(cullable))
                        UpdateBoundingSpheres();
                    break;

                case ICullable.CullMode.Range:
                    _rangeCullables.Remove(cullable);
                    break;
            }
        }

        private void UpdateBoundingSpheres()
        {
            _spheres = new BoundingSphere[_frustumCullables.Count];

            for (int i = 0; i < _frustumCullables.Count; i++)
            {
                var cullable = _frustumCullables[i];
                cullable.CullableIndex = i;
                _spheres[i] = new BoundingSphere(cullable.GetCullPosition(), cullable.GetCullRadius());
            }

            if (_group == null)
                return;

            _group.SetBoundingSpheres(_spheres);
            _group.SetBoundingSphereCount(_spheres.Length);
        }

        private void UpdateFrustumCullables()
        {
            if (_spheres == null || _spheres.Length != _frustumCullables.Count)
                return;

            for (int i = 0; i < _frustumCullables.Count; i++)
            {
                _spheres[i].position = _frustumCullables[i].GetCullPosition();
                _spheres[i].radius = _frustumCullables[i].GetCullRadius();
            }
        }

        private void UpdateRangeCullables()
        {
            if (_player == null)
                return;

            Vector3 playerPos = _player.position;

            foreach (var cullable in _rangeCullables)
            {
                float sqrDist = (playerPos - cullable.GetCullPosition()).sqrMagnitude;
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

            if (_frustumCullables[evt.index] == null)
                return;

            ICullable cullable = _frustumCullables[evt.index];

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
