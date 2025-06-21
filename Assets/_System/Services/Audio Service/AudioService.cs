using System;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Game.Services.LightSources;
using Unity.VisualScripting;
using UnityEngine;

public class AudioService : MonoBehaviour
{
    #region Singleton
    private static AudioService _instance;
    public static AudioService Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AudioService");
                _instance = go.AddComponent<AudioService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    #endregion

    #region Classes
    [System.Serializable]
    public class LightGroup
    {
        public string groupName = "New Light Group";
        public List<LightSourceComponent> lightSources = new List<LightSourceComponent>();
        public EventReference ambientTrack;
        [Range(0f, 1f)] public float targetVolume = 1f;
        [Range(0f, 10f)] public float fadeInTime = 2f;
        [Range(0f, 10f)] public float fadeOutTime = 1f;
        public bool requireAllLights = true;

        [Header("Progressive Settings")]
        public bool startOnAwake = true;
        public bool muteWhenInactive = true;

        [Header("Visual Feedback")]
        public bool showDebugInfo = true;
        public Color debugColor = Color.yellow;

        // Runtime
        private FMOD.Studio.EventInstance _instance;
        private bool _isInitialized = false;
        private float _currentVolume = 0f;
        private float _targetVolumeInternal = 0f;
        private Coroutine _fadeCoroutine;

        public bool IsActive
        {
            get
            {
                if (lightSources.Count == 0) return false;

                // Remove null references
                lightSources.RemoveAll(l => l == null);

                if (requireAllLights)
                    return lightSources.All(l => l.IsLightOn);
                else
                    return lightSources.Any(l => l.IsLightOn);
            }
        }

        public int ActiveCount => lightSources.Count(l => l != null && l.IsLightOn);
        public int TotalCount => lightSources.Count;
        public float CurrentVolume => _currentVolume;
        public bool IsInitialized => _isInitialized;

        public void Initialize(MonoBehaviour coroutineRunner)
        {
            if (_isInitialized || ambientTrack.IsNull) return;

            _instance = RuntimeManager.CreateInstance(ambientTrack);
            _instance.setVolume(0f);
            _instance.start();
            _isInitialized = true;
            _currentVolume = 0f;

            Debug.Log($"[AudioService] Initialized track for group: {groupName}");

            // Start fade if should be active
            if (startOnAwake || IsActive)
            {
                UpdateState(coroutineRunner);
            }
        }

        public void UpdateState(MonoBehaviour coroutineRunner)
        {
            if (!_isInitialized) return;

            float newTargetVolume = IsActive ? targetVolume : (muteWhenInactive ? 0f : 0.1f);

            if (Mathf.Abs(newTargetVolume - _targetVolumeInternal) > 0.01f)
            {
                _targetVolumeInternal = newTargetVolume;

                if (_fadeCoroutine != null)
                    coroutineRunner.StopCoroutine(_fadeCoroutine);

                _fadeCoroutine = coroutineRunner.StartCoroutine(FadeVolume(coroutineRunner, newTargetVolume));

                Debug.Log($"[AudioService] Fading group '{groupName}' to volume: {newTargetVolume}");
            }
        }

        private System.Collections.IEnumerator FadeVolume(MonoBehaviour coroutineRunner, float targetVol)
        {
            float startVolume = _currentVolume;
            float fadeTime = targetVol > startVolume ? fadeInTime : fadeOutTime;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;

                // Use a curve for smoother fades
                t = Mathf.SmoothStep(0f, 1f, t);

                _currentVolume = Mathf.Lerp(startVolume, targetVol, t);
                _instance.setVolume(_currentVolume);

                yield return null;
            }

            _currentVolume = targetVol;
            _instance.setVolume(_currentVolume);
            _fadeCoroutine = null;
        }

        public void Stop()
        {
            if (_isInitialized)
            {
                _instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _instance.release();
                _isInitialized = false;
                _currentVolume = 0f;
            }
        }

        public void SetVolume(float volume)
        {
            if (_isInitialized)
            {
                _currentVolume = volume;
                _instance.setVolume(volume);
            }
        }
    }

    [System.Serializable]
    public class EventSound
    {
        public string eventName = "New Event";
        public EventReference soundToPlay;
        [Range(0f, 1f)] public float volume = 1f;
        public bool playAtPosition = true;
        [TextArea(2, 3)]
        public string notes = "";

        // For organization in editor
        public bool isExpanded = true;

        public void Play(Vector3 position = default)
        {
            if (!soundToPlay.IsNull)
            {
                if (playAtPosition)
                    RuntimeManager.PlayOneShot(soundToPlay, position);
                else
                    RuntimeManager.PlayOneShot(soundToPlay);

                Debug.Log($"[AudioService] Played sound for event: {eventName}");
            }
        }
    }
    #endregion

    #region Fields
    [Header("Light Groups")]
    [SerializeField] private List<LightGroup> _lightGroups = new List<LightGroup>();

    [Header("Movement Events")]
    [SerializeField] private EventSound _onWalkSound = new EventSound { eventName = "On Walk" };

    [Header("Combat Events")]
    [SerializeField] private EventSound _onShootSound = new EventSound { eventName = "On Shoot" };
    [SerializeField] private EventSound _onAimSound = new EventSound { eventName = "On Aim" };

    [Header("Interaction Events")]
    [SerializeField] private EventSound _onPickupSound = new EventSound { eventName = "On Pickup" };
    [SerializeField] private EventSound _onLampToggleSound = new EventSound { eventName = "On Lamp Toggle" };

    [Header("Ability Events")]
    [SerializeField] private EventSound _onPacifyEndSound = new EventSound { eventName = "On Pacify End" };

    [Header("Custom Events")]
    [SerializeField] private List<EventSound> _customEventSounds = new List<EventSound>();

    // Components
    private MovementBehaviorComponent _movement;
    private ShootBehaviorComponent _shoot;
    private PickUpBehaviorComponent _pickUp;
    private PacifyBehaviorComponent _pacify;
    private LightSourcesService _lightService;

    // For editor
    [HideInInspector] public bool showLightGroups = true;
    [HideInInspector] public bool showMovementEvents = true;
    [HideInInspector] public bool showCombatEvents = true;
    [HideInInspector] public bool showInteractionEvents = true;
    [HideInInspector] public bool showOtherEvents = true;
    [HideInInspector] public bool showCustomEvents = true;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitEmitters();
    }

    private void Start()
    {
        SubscribeToEvents();
        InitializeAllLightGroups();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        StopAllAmbients();
    }

    private void OnValidate()
    {
        // Clean up null references in editor
        foreach (var group in _lightGroups)
        {
            group.lightSources.RemoveAll(l => l == null);
        }
    }
    #endregion

    #region Initialization
    private void InitEmitters()
    {
        _movement = FindFirstObjectByType<MovementBehaviorComponent>(FindObjectsInactive.Exclude);
        _shoot = FindFirstObjectByType<ShootBehaviorComponent>(FindObjectsInactive.Exclude);
        _pickUp = FindFirstObjectByType<PickUpBehaviorComponent>(FindObjectsInactive.Exclude);
        _pacify = FindFirstObjectByType<PacifyBehaviorComponent>(FindObjectsInactive.Exclude);
        _lightService = LightSourcesService.Instance;
    }

    private void SubscribeToEvents()
    {
        // Movement
        if (_movement != null)
            _movement.OnWalk += HandleWalk;

        // Combat
        if (_shoot != null)
        {
            _shoot.OnShoot += HandleShoot;
            _shoot.OnAim += HandleAim;
        }

        // Interaction
        if (_pickUp != null)
            _pickUp.OnPickup += HandlePickup;

        // Other
        if (_pacify != null)
            _pacify.OnPacifyEnd += HandlePacifyEnd;

        // Lights
        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight += HandleLightSwitch;
            _lightService.OnSwitchOffLight += HandleLightSwitch;
        }

        Debug.Log("[AudioService] Subscribed to all events");
    }

    private void UnsubscribeFromEvents()
    {
        if (_movement != null)
            _movement.OnWalk -= HandleWalk;

        if (_shoot != null)
        {
            _shoot.OnShoot -= HandleShoot;
            _shoot.OnAim -= HandleAim;
        }

        if (_pickUp != null)
            _pickUp.OnPickup -= HandlePickup;

        if (_pacify != null)
            _pacify.OnPacifyEnd -= HandlePacifyEnd;

        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight -= HandleLightSwitch;
            _lightService.OnSwitchOffLight -= HandleLightSwitch;
        }
    }
    #endregion

    #region Event Handlers
    private void HandleWalk(Vector3 direction, float speed)
    {
        if (speed > 0.1f) // Only play when actually moving
        {
            _onWalkSound.Play(transform.position);
        }
    }

    private void HandleShoot(Ray aimRay, bool isAiming)
    {
        _onShootSound.Play(transform.position);
    }

    private void HandleAim()
    {
        _onAimSound.Play(transform.position);
    }

    private void HandlePickup(PickableComponent pickable)
    {
        _onPickupSound.Play(pickable.transform.position);
    }

    private void HandlePacifyEnd()
    {
        _onPacifyEndSound.Play(transform.position);
    }

    private void HandleLightSwitch(LightSourceComponent light)
    {
        _onLampToggleSound.Play(light.LightPoint.position);
        UpdateLightGroups();
    }
    #endregion

    #region Light System
    private void InitializeAllLightGroups()
    {
        Debug.Log($"[AudioService] Initializing {_lightGroups.Count} light groups...");

        foreach (var group in _lightGroups)
        {
            group.Initialize(this);
        }

        // Initial update to set correct volumes
        UpdateLightGroups();
    }

    private void UpdateLightGroups()
    {
        foreach (var group in _lightGroups)
        {
            group.UpdateState(this);
        }
    }

    private void StopAllAmbients()
    {
        foreach (var group in _lightGroups)
        {
            group.Stop();
        }
    }
    #endregion

    #region Public API
    // For custom events added at runtime
    public void RegisterCustomEvent(string eventName, Action eventAction, EventReference sound)
    {
        var customEvent = new EventSound
        {
            eventName = eventName,
            soundToPlay = sound
        };
        _customEventSounds.Add(customEvent);

        eventAction += () => customEvent.Play();
    }

    public void PlayCustomEvent(string eventName, Vector3 position = default)
    {
        var customEvent = _customEventSounds.FirstOrDefault(e => e.eventName == eventName);
        customEvent?.Play(position);
    }

    // Direct play methods
    public void PlaySound(EventReference eventRef, Vector3 position = default)
    {
        if (!eventRef.IsNull)
            RuntimeManager.PlayOneShot(eventRef, position);
    }

    public void PlaySound(string eventPath, Vector3 position = default)
    {
        RuntimeManager.PlayOneShot(eventPath, position);
    }
    #endregion

    #region Editor Helpers
    [ContextMenu("Find All Light Sources")]
    private void FindAllLightSources()
    {
        var allLights = FindObjectsByType<LightSourceComponent>(FindObjectsSortMode.InstanceID);
        Debug.Log($"Found {allLights.Length} light sources in scene");
    }

    [ContextMenu("Initialize All Groups (Editor)")]
    private void InitializeAllGroupsEditor()
    {
        if (Application.isPlaying)
        {
            InitializeAllLightGroups();
        }
        else
        {
            Debug.LogError("Can only initialize groups in Play mode!");
        }
    }

    [ContextMenu("Validate Configuration")]
    private void ValidateConfiguration()
    {
        int warnings = 0;

        // Check light groups
        foreach (var group in _lightGroups)
        {
            if (group.ambientTrack.IsNull)
            {
                Debug.LogWarning($"Light group '{group.groupName}' has no ambient track!");
                warnings++;
            }

            if (group.lightSources.Count == 0)
            {
                Debug.LogWarning($"Light group '{group.groupName}' has no light sources!");
                warnings++;
            }
        }

        // Check event sounds
        if (_onShootSound.soundToPlay.IsNull)
            Debug.LogWarning("Shoot sound not configured!");

        if (warnings == 0)
            Debug.Log("Audio configuration valid!");
    }

    [ContextMenu("Test All Sounds")]
    private void TestAllSounds()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Can only test sounds in Play mode!");
            return;
        }

        Debug.Log("Testing all configured sounds...");

        _onWalkSound.Play();
        _onShootSound.Play();
        _onAimSound.Play();
        _onPickupSound.Play();
        _onPacifyEndSound.Play();
        _onLampToggleSound.Play();

        foreach (var customSound in _customEventSounds)
        {
            customSound.Play();
        }
    }

    [ContextMenu("Debug: Set All Groups Active")]
    private void DebugSetAllGroupsActive()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Only works in Play mode!");
            return;
        }

        foreach (var group in _lightGroups)
        {
            group.SetVolume(group.targetVolume);
        }
    }

    [ContextMenu("Debug: Mute All Groups")]
    private void DebugMuteAllGroups()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Only works in Play mode!");
            return;
        }

        foreach (var group in _lightGroups)
        {
            group.SetVolume(0f);
        }
    }
    #endregion
}