using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Game.Scenes;
using Game.Services;
using Game.Services.LightSources;
using Game.Services.Fog;

[DefaultExecutionOrder(-1001)]
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

        //DontDestroyOnLoad(this);
    }

    #endregion


    #region Sub-Class

    public enum EService
    {
        LightsService,
        FogService,
        SceneLoadingService
        //AIService,
        //AudioService
    }

    #endregion


    #region Delegates

    public delegate void ServiceInitializedDelegate(Service service);

    #endregion


    #region Fields

    public event ServiceInitializedDelegate OnServiceInitialized = null;

    [SerializeField, HideInInspector]
    private EService[] _servicesOrder = { };

    [SerializeField, HideInInspector]
    private bool _forceInitServices = false;

    /// <summary>
    /// Associates enum values to the related service.
    /// </summary>
    private Dictionary<EService, Type> _serviceValues = new Dictionary<EService, Type>();

    private Dictionary<Type, Service> _servicesInstances = new Dictionary<Type, Service>(); //@todo create struct to centralize EService, Type, Service instance

    #endregion


    #region Lifecycle

    private void Start()
    {
        BindServices();
        StartCoroutine(OrderedInitializationCoroutine());

        if (_forceInitServices)
            StartCoroutine(InitializeUnregisteredServices());
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        foreach (Service service in _servicesInstances.Values)
        {
            if (service == null)
                continue;

            service.Tick(delta);
        }
    }

    #endregion


    #region Private API

    private void BindServices()
    {
        _serviceValues[EService.LightsService] = typeof(LightSourcesService);
        _serviceValues[EService.FogService] = typeof(FogService);
        _serviceValues[EService.SceneLoadingService] = typeof(SceneLoadingService);

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
            if (!_serviceValues.TryGetValue(serviceIndex, out Type type) || !typeof(Service).IsAssignableFrom(type))
                continue;

            Service service = (Service)Activator.CreateInstance(type);
            _servicesInstances.Add(type, service);

            yield return StartCoroutine(InitializeServiceCoroutine(service));
        }

        yield return new WaitForSeconds(1);
        // If all services are initialized
        SceneLoadingService.Instance.LoadScene(SceneLoadingService.GameSceneName);
    }

    private IEnumerator InitializeServiceCoroutine(Service service)
    {
        if (service != null && service.IsServiceInitialized)
            Debug.Log("Service already init: " + service);

        if (service == null || service.IsServiceInitialized)
            yield break;

        //yield return service.Init();
        yield return service.Init();
        service.IsServiceInitialized = true;

        OnServiceInitialized?.Invoke(service);
    }

    //private void InitializeService(Service service)
    //{
    //    if (service != null && service.IsServiceInitialized)
    //        Debug.Log("Service already init: " + service);

    //    if (service == null || service.IsServiceInitialized)
    //        return;

    //    service.Init();
    //    service.IsServiceInitialized = true;

    //    OnServiceInitialized?.Invoke(service);
    //}

    /// <summary>
    /// Forces forgotten services to be initialized.
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeUnregisteredServices()
    {
        // Initialized services
        List<Type> initializedServices = new List<Type>(_serviceValues.Values);

        IEnumerable<Type> serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(Service).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

        foreach (Type serviceType in serviceTypes)
        {
            if (initializedServices.Contains(serviceType))
                continue;

            Service serviceInstance = (Service)Activator.CreateInstance(serviceType);
            yield return StartCoroutine(InitializeServiceCoroutine(serviceInstance));
        }
    }

    #endregion

}
