using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class GameManager : MonoBehaviour
{

    #region Singleton

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"Init {nameof(GameManager)}");
        BindServices();
        StartCoroutine(OrderedInitializationCoroutine());
    }

    #endregion


    #region Sub-Class

    //[Serializable]
    public enum EService
    {
        LightService,
        FogService,
        AIService,
        AudioService
    }

    #endregion


    #region Delegates

    public delegate void ServiceInitializedDelegate(IService service);

    #endregion


    #region Fields

    public event ServiceInitializedDelegate OnServiceInitialized = null;

    [SerializeField]
    private EService[] _servicesOrder = { };

    private LightSourcesService _lightService = null;

    [SerializeField]
    private FogRenderer _fogRenderer = null;

    //[SerializeField]
    //private EnemyAIManager _enemyAI;

    //[SerializeField]
    //private AudioManager _audioManager;

    private Dictionary<EService, IService> _serviceValues = new Dictionary<EService, IService>();

    #endregion


    #region Lifecycle

    private void Start()
    {
        //BindServices();
        //StartCoroutine(OrderedInitializationCoroutine());
    }

    #endregion


    #region Private API

    private void BindServices()
    {
        // @todo Light Service is MB but make it instance
        // @todo Split fog Service and fog renderer and make service as instance

        _serviceValues[EService.LightService] = LightSourcesService.Instance;
        _serviceValues[EService.FogService] = _fogRenderer;

        foreach (EService service in Enum.GetValues(typeof(EService)))
        {
            if (!_serviceValues.ContainsKey(service))
                Debug.LogWarning($"Warning! {service} key is not available from service values binding in {nameof(GameManager)}.", this);
        }
    }

    private IEnumerator OrderedInitializationCoroutine()
    {
        foreach (EService serviceIndex in _servicesOrder)
        {
            if (_serviceValues.TryGetValue(serviceIndex, out IService service))
                yield return StartCoroutine(InitializeServiceCoroutine(service));
        }
    }

    private IEnumerator InitializeServiceCoroutine(IService service)
    {
        if (service == null || service.IsServiceInitialized)
            yield break;

        yield return service.Init();
        service.IsServiceInitialized = true;
        OnServiceInitialized?.Invoke(service);
    }

    ///// <summary>
    ///// Forces forgotten services to be initialized.
    ///// </summary>
    ///// <returns></returns>
    //private IEnumerator InitializeUnregisteredServices()
    //{
    //    List<IService> initializedServices = new List<IService>(_serviceValues.Values);

    //    // Trouver dynamiquement tous les services existants
    //    Type interfaceType = typeof(IService);
    //    IEnumerable<Type> serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
    //        .SelectMany(a => a.GetTypes())
    //        .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

    //    foreach (Type serviceType in serviceTypes)
    //    {
    //        IService serviceInstance = (IService)Activator.CreateInstance(serviceType);

    //        if (!initializedServices.Contains(serviceInstance))
    //            yield return StartCoroutine(InitializeServiceCoroutine(serviceInstance));
    //    }
    //}

    #endregion

}
