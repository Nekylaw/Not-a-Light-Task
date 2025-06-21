using System;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Game.Services.LightSources;
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

    #region Subclasses

    [System.Serializable]
    public class LightGroup
    {
        public string groupName = "New Light Group";
        public List<LightSourceComponent> lightSources = new List<LightSourceComponent>();
        public EventReference ambientTrack;
        [Range(0f, 1f)] public float volume = 1f;
        public bool requireAllLights = true;

        [Header("Visual Feedback")]
        public bool showDebugInfo = true;
        public Color debugColor = Color.yellow;

        // Runtime
        private FMOD.Studio.EventInstance _instance;
        private bool _isPlaying = false;

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

        public void UpdateState()
        {
            bool shouldPlay = IsActive;

            if (shouldPlay && !_isPlaying && !ambientTrack.IsNull)
            {
                _instance = RuntimeManager.CreateInstance(ambientTrack);
                _instance.setVolume(volume);
                _instance.start();
                _isPlaying = true;
                Debug.Log($"[AudioService] Started ambient track for group: {groupName}");
            }
            else if (!shouldPlay && _isPlaying)
            {
                _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _instance.release();
                _isPlaying = false;
                Debug.Log($"[AudioService] Stopped ambient track for group: {groupName}");
            }
        }

        public void Stop()
        {
            if (_isPlaying)
            {
                _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _instance.release();
                _isPlaying = false;
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

    [Header("Gun Events")]
    [SerializeField] private EventSound _onShootSound = new EventSound { eventName = "On Shoot" };
    [SerializeField] private EventSound _onAimSound = new EventSound { eventName = "On Aim" };

    [Header("Lights Events")]
    [SerializeField] private EventSound _onPickupSound = new EventSound { eventName = "On Pickup" };
    [SerializeField] private EventSound _onLampToggleSound = new EventSound { eventName = "On Lamp Toggle" };

    [Header("Ability Events")]
    [SerializeField] private EventSound _onPacifyEndSound = new EventSound { eventName = "On Pacify End" };
    [SerializeField] private EventSound _onPetSound = new EventSound { eventName = "On Pet" };

    [Header("=== Custom Events ===")]
    [SerializeField] private List<EventSound> _customEventSounds = new List<EventSound>();

    // Components
    private MovementBehaviorComponent _movement;
    private ShootBehaviorComponent _shoot;
    private PickUpBehaviorComponent _pickUp;
    private PacifyBehaviorComponent _pacify;
    private PetBehaviorComponent _pet;
    private LightSourcesService _lightService;

    // For editor
    [HideInInspector] public bool ShowLightGroups = true;
    [HideInInspector] public bool ShowMovementEvents = true;
    [HideInInspector] public bool ShowGunEvents = true;
    [HideInInspector] public bool ShowAbilityEvents = true;
    [HideInInspector] public bool ShowOtherEvents = true;
    [HideInInspector] public bool ShowCustomEvents = true;
    #endregion


    #region Lifecycle

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Init();
    }

    private void Start()
    {
        SubscribeToEvents();
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

    private void Init()
    {
        _movement = FindFirstObjectByType<MovementBehaviorComponent>(FindObjectsInactive.Exclude);
        _shoot = FindFirstObjectByType<ShootBehaviorComponent>(FindObjectsInactive.Exclude);
        _pickUp = FindFirstObjectByType<PickUpBehaviorComponent>(FindObjectsInactive.Exclude);
        _pacify = FindFirstObjectByType<PacifyBehaviorComponent>(FindObjectsInactive.Exclude);
        _pet = FindFirstObjectByType<PetBehaviorComponent>(FindObjectsInactive.Exclude);
        _lightService = LightSourcesService.Instance;
    }

    private void SubscribeToEvents()
    {
        // Movement
        if (_movement != null)
            _movement.OnWalk += HandleWalk;

        // Gun
        if (_shoot != null)
        {
            _shoot.OnShoot += HandleShoot;
            _shoot.OnAim += HandleAim;
        }

        // Lights
        if (_pickUp != null)
            _pickUp.OnPickup += HandlePickup;

        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight += HandleLightSwitch;
            _lightService.OnSwitchOffLight += HandleLightSwitch;
        }

        // Ability
        if (_pacify != null)
            _pacify.OnPacifyEnd += HandlePacifyEnd;
 
        if (_pet != null)
            _pet.OnPetStart += HandlePetStart;

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

    #region Handlers
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

    private void HandlePetStart(CreatureController creature)
    {
        _onPetSound.Play(creature.transform.position);
    }

    private void HandleLightSwitch(LightSourceComponent light)
    {
        _onLampToggleSound.Play(light.LightPoint.position);
        UpdateLightGroups();
    }
    #endregion

    #region Light System

    private void UpdateLightGroups()
    {
        foreach (var group in _lightGroups)
        {
            group.UpdateState();
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

    #region Editor Menu

    [ContextMenu("Find All Light Sources")]
    private void FindAllLightSources()
    {
        var allLights = FindObjectsByType<LightSourceComponent>(FindObjectsSortMode.InstanceID);
        Debug.Log($"Found {allLights.Length} light sources in scene");
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

    #endregion
}