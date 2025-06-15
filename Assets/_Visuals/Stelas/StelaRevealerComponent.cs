using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StelaRevealerComponent : MonoBehaviour
{
    [Header("Dissolve Settings")]
    public Material dissolveMaterial;
    public float dissolveSpeed = 0.3f;
    public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool autoStart = true;
    public bool loopEffect = false;

    [Header("Wall Dimensions")]
    public Vector2 wallSize = new Vector2(10f, 6f);
    public Transform wallTransform;

    [Header("Attraction Points")]
    public int numberOfAttractionPoints = 5;
    public GameObject forceFieldPrefab;
    public float attractionStrength = 1f;
    public float attractionRange = 2f;
    public float pointSpacing = 1f;

    [Header("Particle System")]
    public ParticleSystem ambientDust;
    public int maxParticles = 300;
    public float particleLifetime = 8f;
    public Vector3 emissionArea = new Vector3(12f, 8f, 3f);

    [Header("Pattern Settings")]
    public RevealPattern revealPattern = RevealPattern.LeftToRight;
    public float patternRandomness = 0.3f;
    public bool useNoiseOffset = true;
    public float noiseScale = 0.1f;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip revealSound;

    // Private variables
    private float currentDissolve = 0f;
    private List<GameObject> forceFields = new List<GameObject>();
    private List<Vector3> targetPositions = new List<Vector3>();
    private bool isRevealing = false;
    private float revealStartTime;

    // Enums
    public enum RevealPattern
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop,
        CenterOut,
        Spiral,
        Random,
        Custom
    }

    void Start()
    {
        InitializeEffect();
        if (autoStart)
        {
            StartReveal();
        }
    }

    void InitializeEffect()
    {
        // Configure wall transform if not set
        if (wallTransform == null)
            wallTransform = transform;

        // Setup particle system
        SetupParticleSystem();

        // Create force fields
        CreateForceFields();

        // Initialize dissolve material (1 = invisible, 0 = visible)
        if (dissolveMaterial != null)
        {
            dissolveMaterial.SetFloat("_Dissolve", 1f); // Start invisible
        }
    }

    void SetupParticleSystem()
    {
        if (ambientDust == null) return;

        var main = ambientDust.main;
        main.maxParticles = maxParticles;
        main.startLifetime = particleLifetime;
        main.startSpeed = Random.Range(0.3f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        // Configure emission
        var emission = ambientDust.emission;
        emission.rateOverTime = maxParticles / (particleLifetime * 0.7f);

        // Configure shape to emit around the wall
        var shape = ambientDust.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = emissionArea;

        // Position the particle system
        ambientDust.transform.position = wallTransform.position + Vector3.forward * (emissionArea.z * 0.5f);

        // Enable external forces
        var externalForces = ambientDust.externalForces;
        externalForces.enabled = true;
        externalForces.multiplier = 1f;
    }

    void CreateForceFields()
    {
        // Clear existing force fields
        foreach (GameObject field in forceFields)
        {
            if (field != null)
                DestroyImmediate(field);
        }
        forceFields.Clear();
        targetPositions.Clear();

        // Calculate target positions based on pattern
        CalculateTargetPositions();

        // Create force field objects
        for (int i = 0; i < targetPositions.Count; i++)
        {
            GameObject forceField = CreateSingleForceField(i);
            forceFields.Add(forceField);
        }
    }

    void CalculateTargetPositions()
    {
        targetPositions.Clear();
        Vector3 wallCenter = wallTransform.position;

        switch (revealPattern)
        {
            case RevealPattern.LeftToRight:
                for (int i = 0; i < numberOfAttractionPoints; i++)
                {
                    float x = Mathf.Lerp(-wallSize.x * 0.5f, wallSize.x * 0.5f, (float)i / (numberOfAttractionPoints - 1));
                    Vector3 pos = wallCenter + new Vector3(x, 0, 0);
                    targetPositions.Add(pos);
                }
                break;

            case RevealPattern.RightToLeft:
                for (int i = 0; i < numberOfAttractionPoints; i++)
                {
                    float x = Mathf.Lerp(wallSize.x * 0.5f, -wallSize.x * 0.5f, (float)i / (numberOfAttractionPoints - 1));
                    Vector3 pos = wallCenter + new Vector3(x, 0, 0);
                    targetPositions.Add(pos);
                }
                break;

            case RevealPattern.TopToBottom:
                for (int i = 0; i < numberOfAttractionPoints; i++)
                {
                    float y = Mathf.Lerp(wallSize.y * 0.5f, -wallSize.y * 0.5f, (float)i / (numberOfAttractionPoints - 1));
                    Vector3 pos = wallCenter + new Vector3(0, y, 0);
                    targetPositions.Add(pos);
                }
                break;

            case RevealPattern.BottomToTop:
                for (int i = 0; i < numberOfAttractionPoints; i++)
                {
                    float y = Mathf.Lerp(-wallSize.y * 0.5f, wallSize.y * 0.5f, (float)i / (numberOfAttractionPoints - 1));
                    Vector3 pos = wallCenter + new Vector3(0, y, 0);
                    targetPositions.Add(pos);
                }
                break;

            case RevealPattern.CenterOut:
                for (int i = 0; i < numberOfAttractionPoints; i++)
                {
                    float angle = (2f * Mathf.PI * i) / numberOfAttractionPoints;
                    float radius = Mathf.Min(wallSize.x, wallSize.y) * 0.3f;
                    float x = Mathf.Cos(angle) * radius;
                    float y = Mathf.Sin(angle) * radius;
                    Vector3 pos = wallCenter + new Vector3(x, y, 0);
                    targetPositions.Add(pos);
                }
                break;

            case RevealPattern.Spiral:
                for (int i = 0; i < numberOfAttractionPoints; i++)
                {
                    float t = (float)i / numberOfAttractionPoints;
                    float angle = t * 4f * Mathf.PI; // 2 tours complets
                    float radius = t * Mathf.Min(wallSize.x, wallSize.y) * 0.4f;
                    float x = Mathf.Cos(angle) * radius;
                    float y = Mathf.Sin(angle) * radius;
                    Vector3 pos = wallCenter + new Vector3(x, y, 0);
                    targetPositions.Add(pos);
                }
                break;

            case RevealPattern.Random:
                for (int i = 0; i < numberOfAttractionPoints; i++)
                {
                    float x = Random.Range(-wallSize.x * 0.5f, wallSize.x * 0.5f);
                    float y = Random.Range(-wallSize.y * 0.5f, wallSize.y * 0.5f);
                    Vector3 pos = wallCenter + new Vector3(x, y, 0);
                    targetPositions.Add(pos);
                }
                break;
        }

        if (useNoiseOffset)
        {
            for (int i = 0; i < targetPositions.Count; i++)
            {
                Vector3 noise = new Vector3(
                    (Mathf.PerlinNoise(i * noiseScale, 0) - 0.5f) * patternRandomness,
                    (Mathf.PerlinNoise(0, i * noiseScale) - 0.5f) * patternRandomness,
                    0
                );
                targetPositions[i] += noise;
            }
        }
    }

    GameObject CreateSingleForceField(int index)
    {
        GameObject forceFieldObj;

        if (forceFieldPrefab != null)
        {
            forceFieldObj = Instantiate(forceFieldPrefab, transform);
        }
        else
        {
            forceFieldObj = new GameObject($"ForceField_{index}");
            forceFieldObj.transform.SetParent(transform);
        }

        ParticleSystemForceField field = forceFieldObj.GetComponent<ParticleSystemForceField>();
        if (field == null)
        {
            field = forceFieldObj.AddComponent<ParticleSystemForceField>();
        }

        field.shape = ParticleSystemForceFieldShape.Sphere;
        field.startRange = 0.1f;
        field.endRange = attractionRange;
        field.length = attractionStrength;
        field.gravityFocus = 1f; 
        field.rotationSpeed = 0f;
        field.rotationAttraction = 0f;
        field.drag = 0.1f;

        forceFieldObj.transform.position = wallTransform.position + Vector3.back * 100f;
        forceFieldObj.SetActive(false);

        return forceFieldObj;
    }

    void Update()
    {
        if (isRevealing)
        {
            UpdateRevealEffect();
        }
    }

    void UpdateRevealEffect()
    {
        float elapsed = Time.time - revealStartTime;
        float normalizedTime = elapsed / (1f / dissolveSpeed);
        float curvedProgress = dissolveCurve.Evaluate(normalizedTime);

        currentDissolve = Mathf.Lerp(1f, 0f, curvedProgress);
        if (dissolveMaterial != null)
        {
            dissolveMaterial.SetFloat("_Dissolve", currentDissolve);
        }

        UpdateForceFields(curvedProgress);

        if (normalizedTime >= 1f)
        {
            if (loopEffect)
                RestartEffect();
            else
                CompleteEffect();
        }
    }

    void UpdateForceFields(float progress)
    {
        for (int i = 0; i < forceFields.Count; i++)
        {
            float pointProgress = Mathf.Clamp01((progress * numberOfAttractionPoints) - i);

            if (pointProgress > 0f)
            {
                // Activate and position force field
                forceFields[i].SetActive(true);

                // Smooth movement to target position
                Vector3 startPos = wallTransform.position + Vector3.back * 100f;
                Vector3 targetPos = targetPositions[i];
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, pointProgress);

                forceFields[i].transform.position = currentPos;

                // Adjust force field strength based on progress
                var field = forceFields[i].GetComponent<ParticleSystemForceField>();
                if (field != null)
                {
                    field.length = attractionStrength * pointProgress;
                }
            }
            else
            {
                forceFields[i].SetActive(false);
            }
        }
    }

    public void StartReveal()
    {
        isRevealing = true;
        revealStartTime = Time.time;
        currentDissolve = 1f; // Start invisible

        // @todo reveal sound

        Debug.Log("Dust reveal effect started!");
    }

    public void StopReveal()
    {
        isRevealing = false;

        // Deactivate all force fields
        foreach (GameObject field in forceFields)
        {
            if (field != null)
                field.SetActive(false);
        }
    }

    public void RestartEffect()
    {
        currentDissolve = 0f;
        revealStartTime = Time.time;

        if (dissolveMaterial != null)
        {
            dissolveMaterial.SetFloat("_DissolveAmount", 0f);
        }

        // Optionally regenerate random pattern
        if (revealPattern == RevealPattern.Random)
        {
            CalculateTargetPositions();
        }
    }

    public void CompleteEffect()
    {
        isRevealing = false;
        currentDissolve = 0f; // Fully visible

        if (dissolveMaterial != null)
        {
            dissolveMaterial.SetFloat("_Dissolve", 0f);
        }
        StartCoroutine(DeactivateForceFieldsDelayed(1f));
    }

    IEnumerator DeactivateForceFieldsDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (GameObject field in forceFields)
        {
            if (field != null)
                field.SetActive(false);
        }
    }

    public void SetRevealPattern(RevealPattern pattern)
    {
        revealPattern = pattern;
        CalculateTargetPositions();
    }

    public void SetNumberOfPoints(int count)
    {
        numberOfAttractionPoints = Mathf.Clamp(count, 1, 20);
        CreateForceFields();
    }

    public void SetAttractionStrength(float strength)
    {
        attractionStrength = strength;

        foreach (GameObject fieldObj in forceFields)
        {
            var field = fieldObj.GetComponent<ParticleSystemForceField>();
            if (field != null)
            {
                field.length = attractionStrength;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (wallTransform == null) wallTransform = transform;

        // Draw wall bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(wallTransform.position, new Vector3(wallSize.x, wallSize.y, 0.1f));

        // Draw emission area
        Gizmos.color = Color.cyan;
        Vector3 emissionPos = wallTransform.position + Vector3.forward * (emissionArea.z * 0.5f);
        Gizmos.DrawWireCube(emissionPos, emissionArea);

        // Draw target positions
        if (targetPositions != null && targetPositions.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < targetPositions.Count; i++)
            {
                Gizmos.DrawWireSphere(targetPositions[i], 0.2f);
                Gizmos.DrawLine(wallTransform.position, targetPositions[i]);
            }
        }

        // Draw attraction ranges
        if (forceFields != null)
        {
            Gizmos.color = Color.green;
            foreach (GameObject fieldObj in forceFields)
            {
                if (fieldObj != null && fieldObj.activeInHierarchy)
                {
                    Gizmos.DrawWireSphere(fieldObj.transform.position, attractionRange);
                }
            }
        }
    }
}