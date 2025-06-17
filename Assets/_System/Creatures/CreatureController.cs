using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Services.LightSources;

[RequireComponent(typeof(Rigidbody))]
public class CreatureController : MonoBehaviour
{
    #region Enums and States

    public enum ECreatureState
    {
        Idle,
        Wandering,
        Seeking,
        Eating,
        Draining,
        Pacified
    }

    [Header("=== Current State ===")]
    [SerializeField] private ECreatureState currentState = ECreatureState.Wandering;
    public ECreatureState CurrentState => currentState;

    [Header("=== Debug ===")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool showGizmos = true;
    #endregion

    #region Core Components

    // Components
    private Rigidbody rb;
    private AudioSource audioSource;
    private Renderer creatureRenderer;
    private MaterialPropertyBlock propBlock;

    // State tracking
    private float stateTimer;
    private Transform currentTarget;
    private Vector3 startPosition;
    private Vector3 wanderTarget;
    private Vector3 velocity;
    private float currentSpeed;

    // Animation
    private Vector3 originalScale;
    private float bobTimer;
    private Vector3 smoothDampVelocity;

    // Flocking
    private List<CreatureController> nearbyCreatures = new List<CreatureController>();

    // Randomness
    private float noiseOffset;
    private float behaviorTimer;

    // Stuck detection
    private Vector3 lastPositionCheck;
    private float creatureStuckTimer;

    // TRacking timeout
    private float currentTargetTrackingTime;
    private Vector3 lastTargetPosition;
    private float stuckOnTargetTimer;
    private List<Transform> orbsBlackList = new();
    private Dictionary<Transform, float> blackListedTimers = new();

    // Draining
    private Transform currentLightTarget;
    private float drainTimer;
    private LightSourceComponent targetLightSource;
    private float drainCooldownTimer = 0f;

    private Animator _animator;

    #endregion

    #region Serialized Settings

    [Header("=== Detection Settings ===")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float groupAwarenessRadius = 5f;
    [SerializeField] private float eatDistance = 1.5f;
    [SerializeField] private LayerMask orbLayer;
    [SerializeField] private LayerMask creatureLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("=== Movement Settings ===")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("=== Obstacle Avoidance ===")]
    [SerializeField] private bool enableObstacleAvoidance = true;
    [SerializeField] private float avoidanceRadius = 2f;
    [SerializeField] private float avoidanceStrength = 3f;
    [SerializeField] private int avoidanceRayCount = 5;
    [SerializeField] private float avoidanceAngle = 45f;

    [Header("=== Animation Settings ===")]
    [SerializeField] private float bobAmount = 0.08f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float tiltAmount = 10f;
    [SerializeField] private float squashStretchAmount = 0.15f;

    [Header("=== Flocking Behavior ===")]
    [SerializeField] private float separationDistance = 1.5f;
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    [Header("=== Natural Behavior ===")]
    [SerializeField] private float idleChance = 0.3f;
    [SerializeField] private float idleDuration = 2f;
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float noiseFrequency = 1f;
    [SerializeField] private float eatDuration = 2f;

    [Header("=== Draining ===")]
    [SerializeField] private bool enableDrain = false;
    [SerializeField] private float lightSourceDetectionRadius = 6f;
    [SerializeField] private float drainDuration = 2;
    [SerializeField] private float drainDistance = 4f;
    [SerializeField] private LayerMask lightSourceLayer;
    [SerializeField] private float excitmentOnLightSourceDetection = 3f;
    [SerializeField] private float postDrainCooldown = 5f;

    [Header("=== Draining Anim ===")]
    [SerializeField] private float drainPusle = 10f;
    [SerializeField] private float drainScaleAmount = 0.2f;
    [SerializeField] private ParticleSystem drainParticles;


    [Header("=== Excitement System ===")]
    [SerializeField] private float excitementLevel = 0f;
    [SerializeField] private float calmRate = 1f;
    [SerializeField] private float excitementOnOrbSight = 5f;
    [SerializeField] private float maxExcitement = 10f;
    [SerializeField] private float excitementReducerOnGiveUp = 3f;

    [Header("=== Tracking Settings ===")]
    [SerializeField] private float maxTrackingTime = 10f;
    [SerializeField] private float targetStuckTreshold = 0.5f; // Distance limit before a target is considered stuck
    [SerializeField] private float maxStuckOnTargetDuration = 3f;
    [SerializeField] private float blackListDuration = 30f;
    [SerializeField] private bool enableSmartGiveUp = true; // Stop tracking smartly
    [SerializeField][Range(0, 1)] private float homeInfluence = 0.7f;

    [Header("=== Visual Settings ===")]
    [SerializeField] private CreatureVisualProfileSO visualProfile;

    [Header("=== Audio Settings ===")]
    [SerializeField] private AudioClip[] idleSounds;
    [SerializeField] private AudioClip[] excitedSounds;
    [SerializeField] private AudioClip eatSound;
    [SerializeField] private AudioClip pacifySound;
    [SerializeField] private float soundCooldown = 3f;
    private float currentSoundCooldown;

    [Header("=== Effects (Optional) ===")]
    [SerializeField] private ParticleSystem excitementParticles;
    [SerializeField] private ParticleSystem eatingParticles;
    [SerializeField] private ParticleSystem pacifyParticles;
    #endregion

    #region Properties
    public bool IsPacified => currentState == ECreatureState.Pacified;
    public bool IsEating => currentState == ECreatureState.Eating;
    public float ExcitementLevel => excitementLevel;
    public Vector3 Velocity => velocity;

    #endregion

    #region Lifecycle
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        audioSource = GetComponent<AudioSource>();
        creatureRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        originalScale = transform.localScale;
        noiseOffset = Random.Range(0f, 100f);
    }

    void Start()
    {
        startPosition = transform.position;
        lastPositionCheck = transform.position;
        SetNewWanderTarget();
        ChangeState(ECreatureState.Wandering);
    }

    void Update()
    {
        float delta = Time.deltaTime;

        if (drainCooldownTimer > 0)
            drainCooldownTimer -= delta;

        UpdateNearbyCreatures();
        UpdateExcitement(delta);
        UpdateBlacklists(delta);
        UpdateState(delta);
        UpdateAnimation();
        UpdateVisuals();
        UpdateAudio();
        UpdateParticles();
        CheckIfStuck();
    }

    void FixedUpdate()
    {
        if (velocity.magnitude <= 0.01f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 movement = velocity * Time.fixedDeltaTime;
        Vector3 newPosition = rb.position + movement;

        newPosition.y = startPosition.y;
        rb.MovePosition(newPosition);

        // Handle movement
        if (velocity.magnitude > 0.01f)
        {
            _animator.SetBool("isMoving", true);
            _animator.SetFloat("speed", currentSpeed);

        }
        else
        {
            _animator.SetBool("isMoving", false);
        }
    }

    #endregion

    #region State Management
    public void ChangeState(ECreatureState newState)
    {
        if (debugMode)
            Debug.Log($"{name}: {currentState} -> {newState}");

        OnExitState(currentState);
        currentState = newState;
        stateTimer = 0f;
        OnEnterState(newState);
    }

    void OnEnterState(ECreatureState state)
    {
        switch (state)
        {
            case ECreatureState.Idle:
                currentSpeed = 0f;
                velocity = Vector3.zero;
                if (Random.value < 0.5f)
                    PlayRandomSound(idleSounds);
                break;

            case ECreatureState.Wandering:
                SetNewWanderTarget();
                behaviorTimer = 0f;
                break;

            case ECreatureState.Seeking:
                excitementLevel = Mathf.Min(maxExcitement, excitementLevel + 2f);
                AlertNearbyCreatures();

                currentTargetTrackingTime = 0f;
                stuckOnTargetTimer = 0f;
                lastTargetPosition = transform.position;
                break;

            case ECreatureState.Eating:
                currentSpeed = 0f;
                velocity = Vector3.zero;
                PlaySound(eatSound);
                excitementLevel = maxExcitement;

                if (currentTarget != null)
                {
                    OrbComponent orb = currentTarget.GetComponent<OrbComponent>();
                    orb?.StartBeingEaten();
                }

                //eat animation 
                if (_animator != null)
                {
                    _animator.SetTrigger("trPickUp");
                }

                if (eatingParticles != null)
                    eatingParticles.Play();
                break;

            case ECreatureState.Draining:
                drainTimer = 0f;
                currentSpeed = 0;
                excitementLevel = Mathf.Min(maxExcitement, excitementLevel + excitmentOnLightSourceDetection);

                //if (targetLightSource != null)
                //{
                //    PlaySound(targetLightSource.DrainSound);
                //    if (drainParticle != null)
                //        drainParticle.Play();
                //}

                AlertNearbyCreatures(); ;
                break;


            case ECreatureState.Pacified:
                excitementLevel = 0f;
                PlaySound(pacifySound);
                StartCoroutine(PacifyEffect());

                if (pacifyParticles != null)
                    pacifyParticles.Play();
                break;
        }
    }

    void OnExitState(ECreatureState state)
    {
        switch (state)
        {
            case ECreatureState.Eating:
                transform.localScale = originalScale;
                currentTarget = null;
                break;

            case ECreatureState.Draining:
                //if (drainParticle != null)
                //{
                //    drainParticle.Stop();
                //    drainParticle.Clear();
                //}

                currentLightTarget = null;
                targetLightSource = null;
                drainTimer = 0f;
                drainCooldownTimer = postDrainCooldown;

                if (targetLightSource != null)
                {
                    targetLightSource.DrainLight();
                    targetLightSource = null;
                }
                break;
        }
    }

    void UpdateState(float delta)
    {
        stateTimer += delta;

        switch (currentState)
        {
            case ECreatureState.Idle:
                UpdateIdleState();
                break;
            case ECreatureState.Wandering:
                UpdateWanderingState(delta);
                break;
            case ECreatureState.Seeking:
                UpdateSeekingState(delta);
                break;
            case ECreatureState.Eating:
                UpdateEatingState(delta);
                break;
            case ECreatureState.Draining:
                UpdateDrainingState(delta);
                break;
            case ECreatureState.Pacified:
                UpdatePacifiedState(delta);
                break;
        }
    }

    private void UpdateDrainingState(float delta)
    {
        if (currentLightTarget == null || targetLightSource == null || !targetLightSource.IsLightOn)
        {
            ChangeState(ECreatureState.Wandering);
            return;
        }

        float distanceToLight = Vector3.Distance(transform.position, targetLightSource.LightPoint.position);

        // Get nearby light sources
        if (distanceToLight > drainDistance)
        {
            MoveTowardsTarget(targetLightSource.LightPoint.position, true, delta);
            return;
        }

        // Disturb light orbs if close enough
        AnimateLightOrbFeedbackComponent feedback = targetLightSource.GetComponent<AnimateLightOrbFeedbackComponent>();
        if (feedback != null && distanceToLight < 5f)
        {
            float intensity = 1f - (distanceToLight / 5f);
            feedback.DisturbOrbs(intensity);
        }

        // Drain
        drainTimer += delta;

        // Animate drain
        float drainAnimation = 1 + Mathf.Sin(drainTimer * drainPusle) * drainScaleAmount;
        transform.localScale = originalScale * drainAnimation;

        // Look at the light source
        Vector3 lookDirection = (targetLightSource.LightPoint.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), rotationSpeed * delta);
        }

        // Particle effect
        if (drainParticles != null)
        {
            var shape = drainParticles.shape;
            shape.position = targetLightSource.LightPoint.position - transform.position;
        }

        if (debugMode && Time.frameCount % 30 == 0)
        {
            Debug.Log($"{name} draining light: {drainTimer:F1}/{drainDuration:F1}");
        }

        // animate drain 
        if (_animator != null)
        {
            _animator.SetBool("isSucking", true);
        }

        // End drain
        if (drainTimer >= drainDuration)
        {
            if (_animator != null)
            {
                _animator.SetBool("isSucking", false);
            }

            targetLightSource.DrainLight();
            excitementLevel = maxExcitement;
            StartCoroutine(CompleteDrainAnimationCoroutine());
            ChangeState(ECreatureState.Wandering);
        }
    }


    IEnumerator CompleteDrainAnimationCoroutine()
    {
        float duration = 0.5f;
        float timer = 0f;

        while (timer < duration)
        {
            float scale = 1f + Mathf.Sin((timer / duration) * Mathf.PI) * 0.5f;
            transform.localScale = originalScale * scale;
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    #endregion

    #region State Behaviors

    void UpdateIdleState()
    {
        // Idle animation
        transform.rotation *= Quaternion.Euler(0, Mathf.Sin(Time.time * 2f) * 0.5f, 0);

        if (stateTimer >= idleDuration)
        {
            ChangeState(ECreatureState.Wandering);
        }
    }

    void UpdateWanderingState(float delta)
    {

        // Search for light sources if can drain
        if (!IsPacified && enableDrain && drainCooldownTimer <= 0f)
        {
            LightSourceComponent nearestLight = FindNearestLightSource();
            if (nearestLight != null)
            {
                currentLightTarget = nearestLight.transform;
                targetLightSource = nearestLight;
                ChangeState(ECreatureState.Draining);
                return;
            }
        }

        // Then search for orbs if not pacified
        if (!IsPacified)
        {
            GameObject nearestOrb = FindNearestOrb();
            if (nearestOrb != null)
            {
                currentTarget = nearestOrb.transform;
                ChangeState(ECreatureState.Seeking);
                return;
            }
        }

        behaviorTimer += delta;

        if (behaviorTimer > 5f && Random.value < idleChance * 0.5f)
        {
            ChangeState(ECreatureState.Idle);
            behaviorTimer = 0f;
        }
        else
        {
            MoveTowardsTarget(wanderTarget, false, delta);

            float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);

            if (distanceToTarget < 2f || behaviorTimer > 10f)
            {
                SetNewWanderTarget();
                behaviorTimer = 0f;
            }
        }
    }

    private LightSourceComponent FindNearestLightSource()
    {
        Collider[] lightsInRange = Physics.OverlapSphere(transform.position, lightSourceDetectionRadius, lightSourceLayer);

        LightSourceComponent nearestLight = null;
        float nearestDistance = float.MaxValue;
        foreach (Collider col in lightsInRange)
        {
            LightSourceComponent lightSource = col.GetComponent<LightSourceComponent>();
            if (lightSource != null && lightSource.IsLightOn)
            {
                float distance = Vector3.Distance(transform.position, lightSource.transform.position);

                // Check if the light source is being drained by another creature
                bool isBeingDrained = nearbyCreatures.Any(c => c.currentState == ECreatureState.Draining && c.targetLightSource == lightSource);

                if (!isBeingDrained && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestLight = lightSource;
                }
            }
        }

        return nearestLight;
    }

    void UpdateSeekingState(float delta)
    {
        if (currentTarget == null)
        {
            ChangeState(ECreatureState.Wandering);
            return;
        }

        OrbComponent orb = currentTarget.GetComponent<OrbComponent>();
        if (orb == null || !orb.IsActive)
        {
            currentTarget = null;
            ChangeState(ECreatureState.Wandering);
            return;
        }

        if (orbsBlackList.Contains(currentTarget))
        {
            if (debugMode)
                Debug.Log($"{name} is blacklisted from {currentTarget.name} and will not seek it.");

            currentTarget = null;
            ChangeState(ECreatureState.Wandering);
            return;
        }

        currentTargetTrackingTime += delta;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        float progressMade = Vector3.Distance(lastTargetPosition, transform.position);
        if (progressMade < targetStuckTreshold * delta)
        {
            stuckOnTargetTimer += delta;
        }
        else
        {
            stuckOnTargetTimer = 0f;
            lastTargetPosition = transform.position;
        }

        // Handle give up on target
        bool shouldGiveUp =
               currentTargetTrackingTime > maxTrackingTime ||
               stuckOnTargetTimer > maxStuckOnTargetDuration ||
              (enableSmartGiveUp && IsPathBlocked(currentTarget.position));

        if (shouldGiveUp)
        {
            OnGiveUpTarget();
            return;
        }

        excitementLevel = Mathf.Min(maxExcitement, excitementLevel + excitementOnOrbSight * delta);
        MoveTowardsTarget(currentTarget.position, true, delta);

        if (distanceToTarget <= eatDistance)
        {
            if (CanEatOrb())
            {
                ChangeState(ECreatureState.Eating);
            }
        }
    }

    private void OnGiveUpTarget()
    {
        if (currentTarget != null && !orbsBlackList.Contains(currentTarget))
        {
            orbsBlackList.Add(currentTarget);
            blackListedTimers[currentTarget] = blackListDuration;
        }

        // Reduce excitement level when giveup
        excitementLevel = Mathf.Max(0, excitementLevel - excitementReducerOnGiveUp);

        if (debugMode)
            Debug.Log($"{name} gave up on target {currentTarget?.name ?? "null"} after {stuckOnTargetTimer:F2}s sa mere");

        // Wander
        currentTarget = null;

        // Back to home if too far
        float distanceFromHome = Vector3.Distance(transform.position, startPosition);
        if (distanceFromHome > wanderRadius * 2f)
        {
            wanderTarget = startPosition + Random.insideUnitSphere * (wanderRadius * 0.5f);
            wanderTarget.y = transform.position.y;

            if (debugMode)
                Debug.Log($"{name} is far from home ({distanceFromHome:F1}m), heading back");
        }

        ChangeState(ECreatureState.Wandering);
    }

    /// <summary>
    /// Is the path to the target position blocked by an obstacle?
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    bool IsPathBlocked(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPos);

        RaycastHit hit;
        if (Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            direction,
            out hit,
            distance - 0.5f,
            obstacleLayer))
        {
            return true;
        }

        if (Physics.SphereCast(
            transform.position + Vector3.up * 0.5f,
            0.3f,
            direction,
            out hit,
            distance - 0.5f,
            obstacleLayer))
        {
            return true;
        }

        return false;
    }

    private void UpdateBlacklists(float delta)
    {
        if (blackListedTimers.Count == 0) return;

        List<Transform> toRemove = new List<Transform>();

        var entries = blackListedTimers.ToList();

        foreach (var kvp in entries)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            float newTime = kvp.Value - delta;
            blackListedTimers[kvp.Key] = newTime;

            if (newTime <= 0)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var target in toRemove)
        {
            blackListedTimers.Remove(target);
            orbsBlackList.Remove(target);
        }

        orbsBlackList.RemoveAll(t => t == null);
    }

    void UpdateEatingState(float delta)
    {
        // Eating animation
        float eatAnimation = Mathf.Sin(stateTimer * 20f) * 0.15f + 1f;
        transform.localScale = originalScale * eatAnimation;
        transform.Rotate(Vector3.up * 100f * delta);

        if (stateTimer >= eatDuration)
        {
            if (currentTarget != null)
            {
                OrbComponent orb = currentTarget.GetComponent<OrbComponent>();
                orb?.BeEaten();
            }

            excitementLevel = maxExcitement * 0.5f;
            ChangeState(ECreatureState.Wandering);
        }
    }

    void UpdatePacifiedState(float delta)
    {
        if (Random.value < 0.01f)
        {
            ChangeState(ECreatureState.Idle);
        }
        else
        {
            behaviorTimer += delta;

            if (behaviorTimer > 3f || Vector3.Distance(transform.position, wanderTarget) < 2f)
            {
                SetNewWanderTarget();
                behaviorTimer = 0f;
            }

            MoveTowardsTarget(wanderTarget, false, delta);
        }
    }

    #endregion

    #region Movement
    void MoveTowardsTarget(Vector3 targetPosition, bool isUrgent, float delta)
    {
        // Calculate direction to target
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0;

        if (toTarget.magnitude < 0.1f)
        {
            velocity = Vector3.zero;
            currentSpeed = 0;
            return;
        }

        Vector3 targetDirection = toTarget.normalized;
        Vector3 desiredDirection = targetDirection;

        if (enableObstacleAvoidance)
        {
            Vector3 avoidance = GetObstacleAvoidance();
            if (avoidance.magnitude > 0.1f)
            {
                float avoidanceWeight = isUrgent ? 0.5f : 0.7f;
                desiredDirection = (targetDirection + avoidance * avoidanceWeight).normalized;
            }
        }

        // Handle flocking behavior
        Vector3 flockingForce = CalculateFlockingForce();
        float urgencyMultiplier = isUrgent ? 3f : 1f;
        desiredDirection = (desiredDirection * urgencyMultiplier + flockingForce).normalized;
        desiredDirection.y = 0;


        // Speed calculation
        float targetSpeed = isUrgent ? maxSpeed : moveSpeed;
        if (excitementLevel > 0)
        {
            targetSpeed = Mathf.Lerp(moveSpeed, maxSpeed, excitementLevel / maxExcitement);
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * delta);

        // Rotation
        if (desiredDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * delta);
        }

        // Movement with noise
        float noiseAmount = isUrgent ? 0.2f : 0.5f;
        float noise = Mathf.PerlinNoise(Time.time * noiseFrequency + noiseOffset, 0) - 0.5f;
        Vector3 movement = desiredDirection * currentSpeed + transform.right * noise * noiseAmount;

        // Smooth movement
        velocity = Vector3.SmoothDamp(velocity, movement, ref smoothDampVelocity, 0.1f);
    }

    Vector3 GetObstacleAvoidance()
    {
        Vector3 avoidanceForce = Vector3.zero;

        for (int i = 0; i < avoidanceRayCount; i++)
        {
            float angle = -avoidanceAngle + (avoidanceAngle * 2f * i / (avoidanceRayCount - 1));
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                direction,
                out hit,
                avoidanceRadius,
                obstacleLayer))
            {
                float strength = 1f - (hit.distance / avoidanceRadius);
                Vector3 avoidDirection = Vector3.zero;

                Vector3 hitNormal = hit.normal;
                hitNormal.y = 0;

                Vector3 desiredDirection = velocity.normalized;
                Vector3 avoidanceDirection = Vector3.ProjectOnPlane(desiredDirection, hitNormal).normalized;

                avoidanceForce += avoidanceDirection * strength;

                if (debugMode)
                {
                    Debug.DrawLine(transform.position + Vector3.up * 0.5f, hit.point, Color.red);
                    Debug.DrawRay(hit.point, avoidDirection * strength, Color.yellow);
                }
            }
        }

        if (avoidanceForce.magnitude > 0.01f)
        {
            return avoidanceForce.normalized * avoidanceStrength;
        }
        return Vector3.zero;
    }


    #endregion

    #region Flocking
    void UpdateNearbyCreatures()
    {
        if (currentState == ECreatureState.Eating) return;

        nearbyCreatures.Clear();
        Collider[] creatures = Physics.OverlapSphere(transform.position, groupAwarenessRadius, creatureLayer);

        foreach (Collider col in creatures)
        {
            if (col.gameObject != gameObject)
            {
                CreatureController creature = col.GetComponent<CreatureController>();
                if (creature != null)
                {
                    nearbyCreatures.Add(creature);
                }
            }
        }
    }

    void CheckIfStuck()
    {
        float movementThreshold = 0.1f;
        float distanceMoved = Vector3.Distance(transform.position, lastPositionCheck);

        if (distanceMoved < movementThreshold && currentSpeed > minSpeed && currentState != ECreatureState.Idle)
        {
            creatureStuckTimer += Time.deltaTime;

            if (creatureStuckTimer > 1.5f)
            {
                OnStuck();
                creatureStuckTimer = 0f;
            }
        }
        else
        {
            creatureStuckTimer = 0f;

            if (distanceMoved > 0.01f)
            {
                lastPositionCheck = transform.position;
            }
        }
    }

    void OnStuck()
    {
        if (debugMode)
            Debug.Log($"{name} was stuck! Unstucking...");

        // Petite téléportation aléatoire
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        transform.position += new Vector3(randomOffset.x, 0, randomOffset.y);

        // Nouvelle cible
        SetNewWanderTarget();

        // Petite impulsion arrière
        velocity = -transform.forward * moveSpeed;
    }

    Vector3 CalculateFlockingForce()
    {
        if (nearbyCreatures.Count == 0) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int count = 0;

        foreach (CreatureController creature in nearbyCreatures)
        {
            // Only flock with creatures in similar states
            if (creature.IsPacified == IsPacified)
            {
                float distance = Vector3.Distance(transform.position, creature.transform.position);

                // Separation
                if (distance < separationDistance && distance > 0)
                {
                    Vector3 diff = transform.position - creature.transform.position;
                    diff.Normalize();
                    diff /= distance;
                    separation += diff;
                }

                // Alignment
                alignment += creature.velocity;

                // Cohesion
                cohesion += creature.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            alignment /= count;
            alignment.Normalize();

            cohesion /= count;
            cohesion = (cohesion - transform.position).normalized;
        }

        return separation * separationWeight + alignment * alignmentWeight + cohesion * cohesionWeight;
    }

    void AlertNearbyCreatures()
    {
        foreach (CreatureController creature in nearbyCreatures)
        {
            if (!creature.IsPacified && creature.currentState != ECreatureState.Eating)
            {
                creature.excitementLevel = Mathf.Min(5f, creature.excitementLevel + 2f);
            }
        }
    }
    #endregion

    #region Detection

    GameObject FindNearestOrb()
    {
        Collider[] orbsInRange = Physics.OverlapSphere(transform.position, detectionRadius, orbLayer);

        GameObject nearestOrb = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in orbsInRange)
        {
            if (orbsBlackList.Contains(col.transform))
                continue;

            OrbComponent orb = col.GetComponent<OrbComponent>();
            if (orb != null && orb.CanBeTargeted())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);

                bool isTargeted = nearbyCreatures.Any(c =>
                    c.IsEating && c.currentTarget == col.transform);

                if (!isTargeted && distance < nearestDistance)
                {
                    if (enableSmartGiveUp && IsPathBlocked(col.transform.position))
                        continue;

                    nearestDistance = distance;
                    nearestOrb = col.gameObject;
                }
            }
        }

        return nearestOrb;
    }

    bool CanEatOrb()
    {
        return !nearbyCreatures.Any(c =>
            c.IsEating && c.currentTarget == currentTarget);
    }
    #endregion

    #region Animation
    void UpdateAnimation()
    {
        // Bobbing
        bobTimer += Time.deltaTime * bobSpeed;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmount * (currentSpeed / moveSpeed + 0.3f);

        // Squash and stretch
        float speedFactor = currentSpeed / maxSpeed;
        float stretch = 1f + speedFactor * squashStretchAmount;
        float squash = 1f - speedFactor * squashStretchAmount * 0.5f;

        // Tilt
        float tilt = velocity.magnitude > 0.1f ?
            Vector3.SignedAngle(transform.forward, velocity.normalized, Vector3.up) * -0.5f : 0f;
        tilt = Mathf.Clamp(tilt, -tiltAmount, tiltAmount);

        // Apply transformations
        if (currentState != ECreatureState.Eating)
        {
            transform.localScale = new Vector3(
                originalScale.x * squash,
                originalScale.y * stretch,
                originalScale.z * squash
            );
        }


        if (currentState != ECreatureState.Eating)
        {
            float bobScale = 1f + bobOffset * 0.5f;
            transform.localScale = new Vector3(
                originalScale.x * squash,
                originalScale.y * stretch * bobScale,
                originalScale.z * squash
            );
        }

        // Tilt rotation
        Vector3 euler = transform.rotation.eulerAngles;
        euler.z = tilt;
        transform.rotation = Quaternion.Euler(euler);
    }
    #endregion

    #region Visuals
    void UpdateVisuals()
    {
        if (creatureRenderer == null || visualProfile == null) return;

        creatureRenderer.GetPropertyBlock(propBlock);

        // Get target colors based on state
        CreatureVisualProfileSO.StateProfile profile = GetStateProfile();

        // Apply to shader
        propBlock.SetColor("_BaseColor", profile.baseColor);
        propBlock.SetFloat("_BaseColorMultiplier", visualProfile.baseColorMultiplier);
        propBlock.SetFloat("_BodyAlpha", visualProfile.bodyAlpha);

        propBlock.SetColor("_RimColor", profile.rimColor);
        propBlock.SetFloat("_RimIntensity", profile.rimIntensity);

        propBlock.SetColor("_EyesColor", profile.eyesColor);
        propBlock.SetFloat("_EyesIntensity", profile.eyesIntensity);

        propBlock.SetColor("_HeartColor", profile.heartColor);
        propBlock.SetFloat("_HeartIntensity", profile.heartIntensity);

        // Heart beat speed based on excitement
        float heartBeatSpeed = Mathf.Lerp(profile.heartBeatBaseSpeed, profile.heartBeatExcitedSpeed, excitementLevel / maxExcitement);
        propBlock.SetFloat("_HeartBeatSpeed", heartBeatSpeed);

        // Additional shader properties if needed
        if (currentState == ECreatureState.Eating)
        {
            // Add eating-specific shader effects
            propBlock.SetFloat("_EatPulse", Mathf.Sin(stateTimer * 20f) * 0.5f + 0.5f);
        }

        creatureRenderer.SetPropertyBlock(propBlock);
    }
    CreatureVisualProfileSO.StateProfile GetStateProfile()
    {
        if (visualProfile == null)
        {
            Debug.LogWarning($"No visual profile assigned to {name}");
            return new CreatureVisualProfileSO.StateProfile();
        }
        float t;
        switch (currentState)
        {
            case ECreatureState.Eating:
                return visualProfile.EatingProfile;

            case ECreatureState.Draining:
                return visualProfile.DrainingProfile;

            case ECreatureState.Pacified:
                return visualProfile.PacifiedProfile;

            case ECreatureState.Seeking:
                // Blend between normal and excited based on excitement level
                t = excitementLevel / maxExcitement;
                return CreatureVisualProfileSO.StateProfile.Lerp(
                    visualProfile.NormalProfile,
                    visualProfile.ExcitedProfile,
                    t
                );

            case ECreatureState.Idle:
            case ECreatureState.Wandering:
            default:
                // If there's some excitement even in wandering/idle, blend a bit
                if (excitementLevel > 0)
                {
                    t = excitementLevel / maxExcitement * 0.5f; // Half intensity for non-seeking states
                    return CreatureVisualProfileSO.StateProfile.Lerp(
                        visualProfile.NormalProfile,
                        visualProfile.ExcitedProfile,
                        t
                    );
                }
                return visualProfile.NormalProfile;
        }
    }

    #endregion

    #region Audio
    void UpdateAudio()
    {
        currentSoundCooldown -= Time.deltaTime;

        if (currentSoundCooldown <= 0 && Random.value < 0.01f)
        {
            if (excitementLevel > maxExcitement * 0.5f)
            {
                PlayRandomSound(excitedSounds);
            }
            else if (currentState != ECreatureState.Eating)
            {
                PlayRandomSound(idleSounds);
            }
        }
    }

    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && audioSource != null)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
            currentSoundCooldown = Random.Range(soundCooldown * 0.5f, soundCooldown * 1.5f);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            currentSoundCooldown = soundCooldown * 0.5f;
        }
    }
    #endregion

    #region Particles
    void UpdateParticles()
    {
        if (excitementParticles != null)
        {
            var emission = excitementParticles.emission;
            emission.enabled = excitementLevel > maxExcitement * 0.5f && !IsPacified;

            if (emission.enabled)
            {
                emission.rateOverTime = Mathf.Lerp(5f, 20f, (excitementLevel - 5f) / 5f);
            }
        }
    }
    #endregion

    #region Public API

    public void Pacify()
    {
        if (currentState != ECreatureState.Pacified)
        {
            ChangeState(ECreatureState.Pacified);
        }
    }

    public void ForceChangeTarget(Transform newTarget)
    {
        currentTarget = newTarget;
        if (currentState == ECreatureState.Wandering || currentState == ECreatureState.Idle)
        {
            ChangeState(ECreatureState.Seeking);
        }
    }
    #endregion

    #region Helper Methods

    void SetNewWanderTarget()
    {
        int attempts = 0;
        Vector3 newTarget;

        float distanceFromHome = Vector3.Distance(transform.position, startPosition);
        bool shouldReturnHome = distanceFromHome > wanderRadius * 1.5f;

        do
        {
            if (shouldReturnHome)
            {
                Vector3 directionToHome = (startPosition - transform.position).normalized;
                Vector2 randomOffset = Random.insideUnitCircle * wanderRadius * 0.5f;
                newTarget = transform.position + directionToHome * wanderRadius * homeInfluence;
                newTarget.x += randomOffset.x;
                newTarget.z += randomOffset.y;
            }
            else
            {
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                newTarget = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            }

            attempts++;
        }
        while (Vector3.Distance(transform.position, newTarget) < wanderRadius * 0.3f && attempts < 10);

        wanderTarget = new Vector3(newTarget.x, transform.position.y, newTarget.z);

        if (nearbyCreatures.Count > 2)
        {
            Vector3 groupCenter = transform.position;
            foreach (var creature in nearbyCreatures)
            {
                groupCenter += creature.transform.position;
            }
            groupCenter /= (nearbyCreatures.Count + 1);

            wanderTarget = Vector3.Lerp(wanderTarget, groupCenter, 0.3f);
        }

        if (debugMode)
            Debug.Log($"{name} new wander target at distance: {Vector3.Distance(transform.position, wanderTarget):F1}");
    }

    void UpdateExcitement(float delta)
    {
        excitementLevel = Mathf.Max(0, excitementLevel - calmRate * delta);
    }

    IEnumerator PacifyEffect()
    {
        float duration = 0.5f;
        float timer = 0f;

        while (timer < duration)
        {
            float scale = 1f + Mathf.Sin((timer / duration) * Mathf.PI) * 0.3f;
            transform.localScale = originalScale * scale;
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Detection radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Group awareness radius
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, groupAwarenessRadius);

        // Separation distance
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, separationDistance);

        if (enableDrain)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, lightSourceDetectionRadius);

            // Drain distance
            Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, drainDistance);
        }

        // Current velocity
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, velocity);

            // Target
            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }

            // Wander target
            if (currentState == ECreatureState.Wandering)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(wanderTarget, 0.5f);
                Gizmos.DrawLine(transform.position, wanderTarget);
            }

            // Light target
            if (currentLightTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, currentLightTarget.position);
                Gizmos.DrawWireSphere(currentLightTarget.position, 0.5f);
            }
        }
    }

    void OnGUI()
    {
        if (!debugMode) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 4);
        if (screenPos.z > 0)
        {
            string debugText = $"{currentState}\n" +
                              $"Speed: {currentSpeed:F1}/{velocity.magnitude:F1}\n" +
                              $"Excitement: {excitementLevel:F1}\n" +
                              $"BehaviorTimer: {behaviorTimer:F1}\n" +
                              $"Target Dist: {(wanderTarget != Vector3.zero ? Vector3.Distance(transform.position, wanderTarget).ToString("F1") : "N/A")}";

            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 100), debugText);
        }
    }

    #endregion
}
