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
                _instance = new AudioService();

            return _instance;
        }
    }

    public AudioService() { }

    #endregion


    #region Fields





    // Behaviors
    private MovementBehaviorComponent _movement = null;
    private ShootBehaviorComponent _shoot = null;
    private PickUpBehaviorComponent _pickUp = null;
    private PacifyBehaviourComponent _pacify = null;

    // Lights
    private LightSourcesService _lightService = null;

    // Manager
    private GameManager _gameManager = null;

    #endregion


    private void Awake()
    {
        _movement = FindFirstObjectByType<MovementBehaviorComponent>(FindObjectsInactive.Exclude);
        _shoot = FindFirstObjectByType<ShootBehaviorComponent>(FindObjectsInactive.Exclude);
        _pickUp = FindFirstObjectByType<PickUpBehaviorComponent>(FindObjectsInactive.Exclude);
        _pacify = FindFirstObjectByType<PacifyBehaviourComponent>(FindObjectsInactive.Exclude);

        _lightService = LightSourcesService.Instance;
        _gameManager = GameManager.Instance;
    }

    private void Start()
    {
        // Subscribe to events

        _movement.OnWalk += HandleWalk;
        _shoot.OnShoot += HandleShoot;
        _shoot.OnAim += HandleAim;
        _pickUp.OnPickup += HandlePickup;
        _pacify.OnPacify += HandlePacify;


        _lightService.OnSwitchOnLight += HandleSwitchOffLight;
        _lightService.OnSwitchOffLight += HandleSwitchOffLight;

        Debug.Log("AudioService enabled and event handlers registered.");
    }

    private void OnDisable()
    {
        // Unsubscribe from events

        _movement.OnWalk -= HandleWalk;
        _shoot.OnShoot -= HandleShoot;
        _shoot.OnAim -= HandleAim;
        _pickUp.OnPickup -= HandlePickup;
        _pacify.OnPacify -= HandlePacify;

        _lightService.OnSwitchOnLight -= HandleSwitchOffLight;
        _lightService.OnSwitchOffLight -= HandleSwitchOffLight;
    }

    private void HandleAim()
    {
        Debug.LogWarning("Aim");

    }

    private void HandlePacify(GameObject creature)
    {
        Debug.LogWarning("Pacify: " + creature.name);
    }

    private void HandlePickup(PickableComponent pickable)
    {
        Debug.Log("Pickup");
    }

    private void HandleShoot(Ray aimRay, bool isAiming)
    {
        Debug.LogWarning("HandleShoot");
    }

    private void HandleWalk(Vector3 direction, float speed)
    {
        Debug.LogWarning("Walk");

    }

    private void HandleSwitchOffLight(LightSourceComponent light)
    {
        Debug.Log("Switchoff");
    }


}