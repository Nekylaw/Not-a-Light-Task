using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using DG.Tweening;
using FMODUnity;

using Game.Services.LightSources;

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
                _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _instance.release();
                _isInitialized = false;
                _currentVolume = 0f;

                Debug.Log("Stoppppppppppp");
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

        public void FadeAndStop()
        {
            if (_fadeCoroutine != null)
                Instance.StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = Instance.StartCoroutine(FadeVolumeAndStop());
        }

        private System.Collections.IEnumerator FadeVolumeAndStop()
        {
            float startVolume = _currentVolume;
            float targetVol = 0f;
            float fadeTime = fadeOutTime;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                t = Mathf.SmoothStep(0f, 1f, t);

                _currentVolume = Mathf.Lerp(startVolume, targetVol, t);
                _instance.setVolume(_currentVolume);

                yield return null;
            }

            _currentVolume = 0f;
            _instance.setVolume(0f);

            Stop();

            _fadeCoroutine = null;

            //Debug.Log($"[LightGroup] Fade and stop completed for group: {groupName}");
        }

    }

    [System.Serializable]
    public class EventSound
    {
        public string eventName = "New Event";
        public EventReference soundToPlay;
        public bool isLooped = false;
        [Range(0f, 1f)] public float volume = 1f;
        public bool playAtPosition = true;
        [TextArea(2, 3)]
        public string notes = "";

        // For editor
        public bool isExpanded = true;

        private FMOD.Studio.EventInstance _instance; // sound ref

        public void Play(Vector3 position = default)
        {
            if (soundToPlay.IsNull) return;

            if (isLooped)
            {
                StartLoop(position);
            }
            else
            {
                if (playAtPosition)
                    RuntimeManager.PlayOneShot(soundToPlay, position);
                else
                    RuntimeManager.PlayOneShot(soundToPlay);

                Debug.Log($"[AudioService] Played one-shot sound for event: {eventName}");
            }
        }

        public void StartLoop(Vector3 position = default)
        {
            if (_instance.isValid())
                return;


            _instance = RuntimeManager.CreateInstance(soundToPlay);

            if (playAtPosition)
                _instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));

            _instance.setVolume(volume);
            _instance.start();

            //Debug.Log($"[AudioService] Started looping sound for event: {eventName}");
        }

        public void StartLoop(Transform transform)
        {
            if (_instance.isValid())
                return;


            _instance = RuntimeManager.CreateInstance(soundToPlay);

            if (playAtPosition)
                _instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform));

            _instance.setVolume(volume);
            _instance.start();

            //Debug.Log($"[AudioService] Started looping sound for event: {eventName}");
        }

        public void Stop()
        {
            if (_instance.isValid())
            {
                _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _instance.release();
                _instance.clearHandle();
            }
        }

        public bool IsPlaying()
        {
            if (!_instance.isValid()) return false;

            _instance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);
            return state == FMOD.Studio.PLAYBACK_STATE.PLAYING ||
                   state == FMOD.Studio.PLAYBACK_STATE.STARTING;
        }
    }

    #endregion

    #region Fields

    [Header("Light Groups")]
    [SerializeField] private List<LightGroup> _lightGroups = new List<LightGroup>();

    [Header("UI Events")]
    [SerializeField] private EventSound _playSound = new EventSound { eventName = "Play" };
    [SerializeField] private EventSound _pauseSound = new EventSound { eventName = "Pause" };
    [SerializeField] private EventSound _switchUISound = new EventSound { eventName = "Switch" };

    [Header("Ambiant Event")]
    [SerializeField] private EventSound _fogSound = new EventSound { eventName = "Fog" };
    [SerializeField] private EventSound _windSound = new EventSound { eventName = "Wind" };

    [Header("Movement Events")]
    [SerializeField] private EventSound _onWalkSound = new EventSound { eventName = "On Walk" };

    [Header("Shoot Events")]
    [SerializeField] private EventSound _onShootSound = new EventSound { eventName = "On Shoot" };
    [SerializeField] private EventSound _onShootNoAmmoSound = new EventSound { eventName = "On Shoot No Ammo" };
    [SerializeField] private EventSound _onAimSound = new EventSound { eventName = "On Aim" };
    [SerializeField] private EventSound _orbSound = new EventSound { eventName = "Orb" };

    [Header("Interaction Events")]
    [SerializeField] private EventSound _onPickupSound = new EventSound { eventName = "On Pickup" };
    [SerializeField] private EventSound _onLampToggleSound = new EventSound { eventName = "On Lamp Toggle" };

    [Header("Ability Events")]
    [SerializeField] private EventSound _onPacifyStartSound = new EventSound { eventName = "On Pacify Start" };
    [SerializeField] private EventSound _onPacifyEndSound = new EventSound { eventName = "On Pacify End" };

    [Header("Creature Events")]
    // States
    [SerializeField] private EventSound _onCreatureIdleSound = new EventSound { eventName = "Creature Idle" };
    [SerializeField] private EventSound _onCreatureEatSound = new EventSound { eventName = "Creature Eat Start" };
    [SerializeField] private EventSound _onCreatureEatEndSound = new EventSound { eventName = "Creature Eat End" };
    [SerializeField] private EventSound _onCreatureMoveSound = new EventSound { eventName = "Creature Move" };
    [SerializeField] private EventSound _onCreatureSeekStartSound = new EventSound { eventName = "Creature Seek Start" };
    [SerializeField] private EventSound _onCreatureStopSeekSound = new EventSound { eventName = "Creature Stop Seek" };

    // Pacify
    [SerializeField] private EventSound _onCreaturePacifyStartSound = new EventSound { eventName = "Creature Pacify Start" };
    [SerializeField] private EventSound _onCreaturePacifyEndSound = new EventSound { eventName = "Creature Pacify End" };

    // Drain
    [SerializeField] private EventSound _onCreatureDrainStartSound = new EventSound { eventName = "Creature Drain Start" };
    [SerializeField] private EventSound _onCreatureDrainEndSound = new EventSound { eventName = "Creature Drain End" };

    // Pet
    [SerializeField] private EventSound _onCreaturePetStartSound = new EventSound { eventName = "Creature Pet Start" };
    [SerializeField] private EventSound _onCreaturePetEndSound = new EventSound { eventName = "Creature Pet End" };

    [Header("Activators")]
    [SerializeField] private EventSound _onPortalTriggeredSound = new EventSound { eventName = "Portal Triggered" };
    [SerializeField] private EventSound _onPortalOpennedSound = new EventSound { eventName = "Portal Openned" };
    [SerializeField] private EventSound _onActivateElevatorSound = new EventSound { eventName = "Elevator Activated" };

    [SerializeField] private EventSound _onRevealStelaSoundFirst = new EventSound { eventName = "Stela Revealated(1)" };
    [SerializeField] private EventSound _onRevealStelaSoundSec = new EventSound { eventName = "Stela Revealated(2)" };
    [SerializeField] private EventSound _onRevealStelaSoundThird = new EventSound { eventName = "Stela Revealated(3)" };
    [SerializeField] private EventSound _onRevealStelaSoundFourth = new EventSound { eventName = "Stela Revealated(4)" };

    [Header("Custom Events")]
    [SerializeField] private List<EventSound> _customEventSounds = new List<EventSound>();

    // Refs 
    private MovementBehaviorComponent _movement;
    private ShootBehaviorComponent _shoot;
    private PickUpBehaviorComponent _pickUp;
    private PacifyBehaviorComponent _pacify;
    private PetBehaviorComponent _pet;
    private LightSourcesService _lightService;
    private CreatureService _creatureService;
    private ActivatorsService _activator;

    // For editor
    [HideInInspector] public bool showLightGroups = true;
    [HideInInspector] public bool showMovementEvents = true;
    [HideInInspector] public bool showCombatEvents = true;
    [HideInInspector] public bool showInteractionEvents = true;
    [HideInInspector] public bool showOtherEvents = true;
    [HideInInspector] public bool showCreatureEvents = true;
    [HideInInspector] public bool showCustomEvents = true;

    // Creature move sound cooldowns
    private float _lastCreatureMoveSoundTime = 0f;
    private float _creatureMoveSoundCooldown = 0.5f;

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

        InitEmitters();
    }

    private void Start()
    {
        SubscribeToEvents();
        InitializeAllLightGroups();

        PlaySound(_fogSound.soundToPlay);
        PlaySound(_windSound.soundToPlay);
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
        _pet = FindFirstObjectByType<PetBehaviorComponent>(FindObjectsInactive.Exclude);

        _creatureService = FindFirstObjectByType<CreatureService>(FindObjectsInactive.Exclude);
        _activator = FindFirstObjectByType<ActivatorsService>(FindObjectsInactive.Exclude);

        _lightService = LightSourcesService.Instance;
    }

    private void SubscribeToEvents()
    {
        // Movement
        if (_movement != null)
            _movement.OnWalk += HandleWalk;

        // Shoot
        if (_shoot != null)
        {
            _shoot.OnShoot += HandleShoot;
            _shoot.OnShootNoAmmo += HandleShootNoAmmo;
            _shoot.OnAim += HandleAim;
        }

        // Pickup
        if (_pickUp != null)
        {
            _pickUp.OnPickup += HandlePickup;
            _pickUp.OnReleasePickup += HandlePickupRelease;
        }

        // Pacify
        if (_pacify != null)
        {
            _pacify.OnPacifyStart += HandlePacifyStart;
            _pacify.OnPacifyEnd += HandlePacifyEnd;
        }

        // Pet 
        if (_pet != null)
        {
            _pet.OnPetStart += HandlePlayerPetStart;
            _pet.OnPetEnd += HandlePlayerPetEnd;
        }

        // Lights
        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight += HandleLightSwitch;
            _lightService.OnSwitchOffLight += HandleLightSwitch;
        }

        if (_activator != null)
        {
            // Portals
            _activator.OnPortalTriggered += HandlePortalTriggered;
            _activator.OnPortalOpened += HandlePortalOpened;

            // Elevator
            _activator.OnElevatorActivated += HandleElevatorActivated;

            // Stela 
            _activator.OnRevealStela += HandleRevealStela1;
            _activator.OnRevealStela += HandleRevealStela2;
            _activator.OnRevealStela += HandleRevealStela3;
            _activator.OnRevealStela += HandleRevealStela4;
        }

        // Creatures
        if (_creatureService != null)
        {
            _creatureService.OnCreatureIdle += HandleCreatureIdle;

            _creatureService.OnCreatureBeginEat += HandleCreatureEat;
            _creatureService.OnCreatureEatEnd += HandleCreatureEatEnd;

            _creatureService.OnCreatureMoving += HandleCreatureMove;

            _creatureService.OnCreatureSeek += HandleCreatureSeek;
            _creatureService.OnCreatureStopSeek += HandleCreatureStopSeek;

            _creatureService.OnPacifyStart += HandleCreaturePacifyStart;
            _creatureService.OnPacifyEnd += HandleCreaturePacifyEnd;

            _creatureService.OnPetStart += HandleCreaturePetStart;
            _creatureService.OnPetEnd += HandleCreaturePetEnd;

            _creatureService.OnCreatureDrainingStart += HandleCreatureDrainStart;
            _creatureService.OnCreatureDrainingEnd += HandleCreatureDrainEnd;
        }

        // GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlay += HandlePlayGame;
            GameManager.Instance.OnPause += HandlePauseGame;
        }

        //Debug.Log("[AudioService] Subscribed to all events");
    }

    private void UnsubscribeFromEvents()
    {
        if (_movement != null)
            _movement.OnWalk -= HandleWalk;

        if (_shoot != null)
        {
            _shoot.OnShoot -= HandleShoot;
            _shoot.OnShootNoAmmo -= HandleShootNoAmmo;
            _shoot.OnAim -= HandleAim;
        }

        if (_pickUp != null)
        {
            _pickUp.OnPickup -= HandlePickup;
            _pickUp.OnReleasePickup -= HandlePickupRelease;
        }

        if (_pacify != null)
        {
            _pacify.OnPacifyStart -= HandlePacifyStart;
            _pacify.OnPacifyEnd -= HandlePacifyEnd;
        }

        if (_pet != null)
        {
            _pet.OnPetStart -= HandlePlayerPetStart;
            _pet.OnPetEnd -= HandlePlayerPetEnd;
        }

        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight -= HandleLightSwitch;
            _lightService.OnSwitchOffLight -= HandleLightSwitch;
        }

        if (_activator != null)
        {
            _activator.OnPortalTriggered -= HandlePortalTriggered;
            _activator.OnPortalOpened -= HandlePortalOpened;
        }

        if (_creatureService != null)
        {
            _creatureService.OnCreatureIdle -= HandleCreatureIdle;
            _creatureService.OnCreatureBeginEat -= HandleCreatureEat;
            _creatureService.OnCreatureEatEnd -= HandleCreatureEatEnd;
            _creatureService.OnCreatureMoving -= HandleCreatureMove;
            _creatureService.OnCreatureSeek -= HandleCreatureSeek;
            _creatureService.OnCreatureStopSeek -= HandleCreatureStopSeek;
            _creatureService.OnPacifyStart -= HandleCreaturePacifyStart;
            _creatureService.OnPacifyEnd -= HandleCreaturePacifyEnd;
            _creatureService.OnPetStart -= HandleCreaturePetStart;
            _creatureService.OnPetEnd -= HandleCreaturePetEnd;
            _creatureService.OnCreatureDrainingStart -= HandleCreatureDrainStart;
            _creatureService.OnCreatureDrainingEnd -= HandleCreatureDrainEnd;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlay -= HandlePlayGame;
            GameManager.Instance.OnPause -= HandlePauseGame;
        }
    }

    #endregion

    #region Event Handlers

    private void HandleWalk(Vector3 direction, float speed)
    {
        if (speed > 0.1f)
        {
            _onWalkSound.Play(transform.position);
        }
    }

    private void HandleShoot(OrbComponent orb, Ray aimRay, bool isAiming)
    {
        _onShootSound.Play(_shoot.transform.position);

        if (_orbSound != null && !_orbSound.soundToPlay.IsNull)
        {
            if (_orbSound.isLooped)
                _orbSound.StartLoop(orb.transform);
            else
                _orbSound.Play(orb.transform.position);
        }
    }

    private void HandleShootNoAmmo(Ray aimRay, bool isAiming)
    {
        _onShootNoAmmoSound.Play(_shoot.transform.position);
    }

    private void HandleAim()
    {
        _onAimSound.Play(_movement.transform.position);
    }

    private void HandlePickup()
    {
        if (_onPickupSound.isLooped)
            _onPickupSound.StartLoop(_movement.transform.position);
        else
            _onPickupSound.Play(_movement.transform.position);
    }

    private void HandlePickupRelease()
    {
        if (_onPickupSound.isLooped)
            _onPickupSound.Stop();
    }

    private void HandlePacifyStart()
    {
        if (_onPacifyStartSound.isLooped)
            _onPacifyStartSound.StartLoop(_movement.transform.position);
        else
            _onPacifyStartSound.Play(_movement.transform.position);
    }

    private void HandlePacifyEnd()
    {

        if (_onPacifyStartSound.isLooped)
            _onPacifyStartSound.Stop();

        _onPacifyEndSound.Play(_movement.transform.position);

        Debug.Log("[AudioService] Pacify ended, playing end sound.");
    }

    private void HandlePlayerPetStart(CreatureController creature)
    {
        //@todo
        Debug.Log($"[AudioService] Player started petting creature {creature.name}");
    }

    private void HandlePlayerPetEnd(CreatureController creature, bool success)
    {
        //@todo
        Debug.Log($"[AudioService] Player finished petting creature {creature.name} - Success: {success}");
    }

    private void HandleLightSwitch(LightSourceComponent light)
    {
        _onLampToggleSound.Play(light.LightPoint.position);
        UpdateLightGroups();
    }

    public void HandlePlayGame()
    {
        Debug.Log("Play game audio event");

        float currentHPF;
        FMODUnity.RuntimeManager.StudioSystem.getParameterByName("HPF", out currentHPF);

        DOTween.To(() => currentHPF, x =>
        {
            currentHPF = x;
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("HPF", currentHPF);
        }, 0f, 0.2f);

        PlaySound(_playSound.soundToPlay);
    }

    public void HandlePauseGame()
    {
        Debug.Log("Pause game audio event");

        float currentHPF;
        FMODUnity.RuntimeManager.StudioSystem.getParameterByName("HPF", out currentHPF);

        DOTween.To(() => currentHPF, x =>
        {
            currentHPF = x;
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("HPF", currentHPF);
        }, 1f, 0.2f);

        PlaySound(_pauseSound.soundToPlay);
    }

    public void HandleSwitchMenu()
    {
        PlaySound(_switchUISound.soundToPlay);
    }

    #endregion

    #region Creature Event 

    private void HandleCreatureIdle(CreatureController creature)
    {
        if (_onCreatureIdleSound != null && !_onCreatureIdleSound.soundToPlay.IsNull)
        {
            _onCreatureIdleSound.Play(creature.transform.position);
        }
        Debug.Log($"[AudioService] Creature {creature.name} is now idle");
    }

    private void HandleCreatureEat(CreatureController creature)
    {
        if (_onCreatureEatSound != null && !_onCreatureEatSound.soundToPlay.IsNull)
        {
            _onCreatureEatSound.Play(creature.transform.position);
        }
        Debug.Log($"[AudioService] Creature {creature.name} started eating");
    }

    private void HandleCreatureEatEnd(CreatureController creature)
    {
        if (_onCreatureEatEndSound != null && !_onCreatureEatEndSound.soundToPlay.IsNull)
        {
            _onCreatureEatEndSound.Play(creature.transform.position);
        }
        Debug.Log($"[AudioService] Creature {creature.name} finished eating");
    }

    private void HandleCreatureMove(CreatureController creature, float speed)
    {
        if (speed > 0.1f && _onCreatureMoveSound != null && !_onCreatureMoveSound.soundToPlay.IsNull)
        {
            if (_onCreatureMoveSound.isLooped)
            {
                if (!_onCreatureMoveSound.IsPlaying())
                    _onCreatureMoveSound.StartLoop(creature.transform.position);
            }
            else
            {
                if (Time.time - _lastCreatureMoveSoundTime > _creatureMoveSoundCooldown)
                {
                    _onCreatureMoveSound.Play(creature.transform.position);
                    _lastCreatureMoveSoundTime = Time.time;
                }
            }
        }
        else if (speed <= 0.1f && _onCreatureMoveSound != null && _onCreatureMoveSound.isLooped)
        {
            _onCreatureMoveSound.Stop();
        }
    }

    private void HandleCreatureSeek(CreatureController creature, Vector3 position)
    {
        if (_onCreatureSeekStartSound != null && !_onCreatureSeekStartSound.soundToPlay.IsNull)
        {
            _onCreatureSeekStartSound.Play(creature.transform.position);
        }
        Debug.Log($"[AudioService] Creature {creature.name} started seeking at {position}");
    }

    private void HandleCreatureStopSeek(CreatureController creature)
    {
        if (_onCreatureStopSeekSound != null && !_onCreatureStopSeekSound.soundToPlay.IsNull)
            _onCreatureStopSeekSound.Play(creature.transform.position);

        Debug.Log($"[AudioService] Creature {creature.name} stopped seeking");
    }

    private void HandleCreaturePacifyStart(CreatureController creature)
    {
        if (_onCreaturePacifyStartSound != null && !_onCreaturePacifyStartSound.soundToPlay.IsNull)
        {
            if (_onCreaturePacifyStartSound.isLooped)
                _onCreaturePacifyStartSound.StartLoop(creature.transform.position);
            else
            {
                _onCreaturePacifyStartSound.Play(creature.transform.position);
            }
        }

        Debug.Log($"[AudioService] Creature {creature.name} is being pacified");
        Debug.Log($"HHHHHHHHHHHHHHHHHHHHHHHAAAAAAAAAAAAAAAAAAAAAAAAAA");
    }

    private void HandleCreaturePacifyEnd(CreatureController creature, bool isCancelled)
    {
        if (_onCreaturePacifyEndSound != null && !_onCreaturePacifyEndSound.soundToPlay.IsNull)
            _onCreaturePacifyEndSound.Play(creature.transform.position);


        if (_onCreaturePacifyEndSound != null && _onCreaturePacifyEndSound.isLooped)
            _onCreaturePacifyEndSound.Stop();

        if (_onCreaturePacifyStartSound != null && _onCreaturePacifyStartSound.isLooped)

            _onCreaturePacifyStartSound.Stop();


        Debug.Log($"[AudioService] Creature {creature.name} pacify ended - Cancelled: {isCancelled}");
        Debug.Log($"Hbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

    }

    private void HandleCreatureDrainStart(CreatureController creature)
    {
        if (_onCreatureDrainStartSound != null && !_onCreatureDrainStartSound.soundToPlay.IsNull)
        {
            if (_onCreatureDrainStartSound.isLooped)
                _onCreatureDrainStartSound.StartLoop(creature.transform.position);
            else
                _onCreatureDrainStartSound.Play(creature.transform.position);
        }
        Debug.Log($"[AudioService] Creature {creature.name} started draining");
    }

    private void HandleCreatureDrainEnd(CreatureController creature)
    {
        if (_onCreatureDrainEndSound != null && !_onCreatureDrainEndSound.soundToPlay.IsNull)
        {
            _onCreatureDrainEndSound.Play(creature.transform.position);
        }

        if (_onCreatureDrainStartSound != null && _onCreatureDrainStartSound.isLooped)
        {
            _onCreatureDrainStartSound.Stop();
        }

        Debug.Log($"[AudioService] Creature {creature.name} stopped draining");
    }

    private void HandleCreaturePetStart(CreatureController creature)
    {
        if (_onCreaturePetStartSound != null && !_onCreaturePetStartSound.soundToPlay.IsNull)
        {
            _onCreaturePetStartSound.Play(creature.transform.position);
        }
        Debug.Log($"[AudioService] Creature {creature.name} started being petted");
    }

    private void HandleCreaturePetEnd(CreatureController creature, bool success)
    {
        if (_onCreaturePetEndSound != null && !_onCreaturePetEndSound.soundToPlay.IsNull)
        {
            _onCreaturePetEndSound.Play(creature.transform.position);
        }
        Debug.Log($"[AudioService] Creature {creature.name} finished being petted - Success: {success}");
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

    #region Pportals

    private void HandlePortalTriggered(PortalComponent portal)
    {
        if (_onPortalTriggeredSound != null && !_onPortalTriggeredSound.soundToPlay.IsNull)
        {
            if (_onPortalTriggeredSound.isLooped)
                _onPortalTriggeredSound.StartLoop(portal.transform.position);
            else
                _onPortalTriggeredSound.Play(portal.transform.position);
        }
        Debug.Log($"[AudioService] Portal {portal.name} triggered.");
    }

    private void HandlePortalOpened(PortalComponent portal)
    {
        if (_onPortalOpennedSound != null && !_onPortalOpennedSound.soundToPlay.IsNull)
        {
            if (_onPortalOpennedSound.isLooped)
                _onPortalOpennedSound.StartLoop(portal.transform.position);
            else
                _onPortalOpennedSound.Play(portal.transform.position);
        }
        Debug.Log($"[AudioService] Portal {portal.name} openned.");
    }

    private void HandleElevatorActivated(ElevatorComponent elevator)
    {
        if (_onActivateElevatorSound != null && !_onActivateElevatorSound.soundToPlay.IsNull)
            _onActivateElevatorSound.Play();
    }

    public void HandleRevealStela1(int stelaIndex)
    {
        if (stelaIndex != 1)
            return;

        if (_onRevealStelaSoundFirst != null && !_onRevealStelaSoundFirst.soundToPlay.IsNull)
            _onRevealStelaSoundFirst.Play();
    }

    public void HandleRevealStela2(int stelaIndex)
    {
        if (stelaIndex != 2)
            return;

        if (_onRevealStelaSoundSec != null && !_onRevealStelaSoundSec.soundToPlay.IsNull)
            _onRevealStelaSoundSec.Play();
    }

    public void HandleRevealStela3(int stelaIndex)
    {
        if (stelaIndex != 3)
            return;

        if (_onRevealStelaSoundThird != null && !_onRevealStelaSoundThird.soundToPlay.IsNull)
            _onRevealStelaSoundThird.Play();
    }

    public void HandleRevealStela4(int stelaIndex)
    {
        if (stelaIndex != 4)
            return;

        if (_onRevealStelaSoundFourth != null && !_onRevealStelaSoundFourth.soundToPlay.IsNull)
            _onRevealStelaSoundFourth.Play();
    }

    #endregion 

    #region Public API
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

    public void FadeAndStopStem(LightGroup group)
    {
        if (group == null || !group.IsInitialized)
            return;

        group.FadeAndStop();
        Debug.Log($"[AudioService] Fading and stopping group: {group.groupName}");
    }

    /// <summary>
    /// Fades and stops all light groups except the ones in the excluded list.
    /// </summary>
    /// <param name="excludedDroups"></param>
    public void FadeAndStopAllGroups(List<LightGroup> excludedGroups = null)
    {
        if (excludedGroups == null)
            excludedGroups = new List<LightGroup>();

        List<LightGroup> filteredGroups = _lightGroups.Where(g => !excludedGroups.Contains(g)).ToList();

        foreach (var group in filteredGroups)
        {
            if (!group.IsInitialized)
                continue;

            FadeAndStopStem(group);
            Debug.Log($"[AudioService] Fading and stopping group: {group.groupName}");
        }
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