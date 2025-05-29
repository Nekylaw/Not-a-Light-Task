using UnityEngine;

public class TrailBehaviour : MonoBehaviour
{
    public Transform Pos1;
    public Transform Pos2;
    
    [Header("Trail Renderer")]
    [Range(0.5f, 10f)]
    public float trailLength = 2f; // Longueur du trail
    [Range(0.01f, 0.5f)]
    public float minVertexDistance = 0.1f; // Distance minimale entre les vertices du trail
    public bool adjustHeightToGround = true; // Maintenir à une hauteur constante du sol
    [Range(0.5f, 5f)]
    public float heightAboveGround = 1f; // Hauteur au-dessus du sol (en mètres)
    public LayerMask groundLayer; // Layer du sol pour le raycast
    
    [Header("Décalage de chemin")]
    [Range(0f, 5f)]
    public float pathOffset = 0f;
    [Range(0f, 1f)]
    public float randomizePathOffset = 0f;
    [Range(0f, 1f)]
    public float heightVariation = 0f;
    
    [Header("Mouvement")]
    [Range(0.5f, 5f)]
    public float speed = 1.2f;
    [Range(0.1f, 2f)]
    public float smoothTime = 0.6f;
    public float waitDuration = 1.5f;
    [Range(0f, 1f)]
    public float easingPower = 0.7f;
    [Range(0f, 1f)]
    public float speedVariation = 0f;

    [Header("Oscillation")]
    [Range(0.1f, 2f)]
    public float baseFrequency = 0.7f;
    [Range(0.1f, 4f)]
    
    public float baseAmplitude = 0.8f;
    [Range(0f, 1f)]
    public float tiltAmount = 0.25f;
    [Range(0f, 1f)]
    public float phaseVariation = 0f;

    [Header("Flottement vertical")]
    [Range(0.1f, 1f)]
    public float floatFrequency = 0.4f;
    [Range(0.05f, 0.5f)]
    public float floatAmplitude = 0.15f;

    private Vector3 velocity = Vector3.zero;
    private float travelPercent = 0f;
    private bool goingToPos2 = true;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Vector3 previousDirection = Vector3.forward;
    private Vector3 pathOffsetVector;
    private float randomHeightOffset;
    private float timeOffset;
    private float actualSpeed;
    private float actualWaitDuration;
    private TrailRenderer trailRenderer;

    private void Start()
    {
        // Initialiser et configurer le TrailRenderer
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer != null)
        {
            trailRenderer.time = trailLength;
            trailRenderer.minVertexDistance = minVertexDistance;
        }
        else
        {
            Debug.LogWarning("Aucun TrailRenderer trouvé sur l'objet. Ajoutez un TrailRenderer pour voir l'effet.");
        }
        
        // Déterminer le vecteur de décalage au démarrage
        if (Pos1 != null && Pos2 != null)
        {
            Vector3 pathDirection = (Pos2.position - Pos1.position).normalized;
            Vector3 perpendicular = Vector3.Cross(pathDirection, Vector3.up).normalized;
            
            float finalOffset = pathOffset;
            if (randomizePathOffset > 0)
            {
                finalOffset += Random.Range(-randomizePathOffset, randomizePathOffset) * pathOffset;
            }
            
            pathOffsetVector = perpendicular * finalOffset;
            
            randomHeightOffset = Random.Range(-heightVariation, heightVariation);
            
            timeOffset = Random.Range(0f, 6.28f) * phaseVariation;
            
            actualSpeed = speed * (1f + Random.Range(-speedVariation, speedVariation));
            
            actualWaitDuration = waitDuration * (1f + Random.Range(-speedVariation * 0.5f, speedVariation * 0.5f));
        }
    }

    void Update()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= actualWaitDuration)
            {
                isWaiting = false;
                waitTimer = 0f;
                goingToPos2 = !goingToPos2;
                travelPercent = 0f;
            }
            return;
        }

        Vector3 start = goingToPos2 ? Pos1.position : Pos2.position;
        Vector3 end = goingToPos2 ? Pos2.position : Pos1.position;
        
        start += pathOffsetVector + new Vector3(0, randomHeightOffset, 0);
        end += pathOffsetVector + new Vector3(0, randomHeightOffset, 0);

        float distance = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        
        direction = Vector3.Lerp(previousDirection, direction, Time.deltaTime * 3f);
        previousDirection = direction;
        
        Vector3 orthogonal = Vector3.Cross(direction, Vector3.up);
        Vector3 tiltDirection = (orthogonal * tiltAmount + direction * 0.15f).normalized;

        travelPercent += Time.deltaTime * actualSpeed / distance;
        travelPercent = Mathf.Clamp01(travelPercent);
        
        float easedPercent = EaseInOut(travelPercent, easingPower);
        
        Vector3 basePos = Vector3.Lerp(start, end, easedPercent);

        float time = Time.time * 0.2f + timeOffset;
        float dynamicFrequency = baseFrequency + Mathf.PerlinNoise(time, 0f) * 0.3f;
        float dynamicAmplitude = baseAmplitude + Mathf.PerlinNoise(0f, time) * 0.2f;

        float sinOffset = Mathf.Sin(time * dynamicFrequency * Mathf.PI * 2f) * dynamicAmplitude;
        Vector3 lateralOffset = tiltDirection * sinOffset;

        float verticalOffset = Mathf.Sin(time * floatFrequency * Mathf.PI * 2f) * floatAmplitude;
        
        // Séparer le mouvement horizontal et vertical
        Vector3 horizontalTarget = basePos + lateralOffset;
        Vector3 finalTarget = horizontalTarget + new Vector3(0, verticalOffset, 0);
        
        // Appliquer le SmoothDamp d'abord
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, finalTarget, ref velocity, smoothTime);
        
        // Ajuster la hauteur pour rester à une distance fixe du sol APRÈS le smoothing
        if (adjustHeightToGround)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(smoothedPosition.x, smoothedPosition.y + 100f, smoothedPosition.z), Vector3.down, out hit, 200f, groundLayer))
            {
                // Forcer la position Y pour être exactement à heightAboveGround mètres au-dessus du sol
                // On garde seulement le flottement vertical comme offset
                smoothedPosition.y = hit.point.y + heightAboveGround + verticalOffset;
            }
        }

        transform.position = smoothedPosition;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }

        if (travelPercent >= 1f)
        {
            isWaiting = true;
        }
    }
    
    private float EaseInOut(float t, float power)
    {
        if (t < 0.5f)
        {
            return 0.5f * Mathf.Pow(2f * t, power);
        }
        else
        {
            return 1f - 0.5f * Mathf.Pow(2f * (1f - t), power);
        }
    }
    
    // Si les paramètres du trail sont modifiés dans l'inspecteur pendant l'exécution
    void OnValidate()
    {
        TrailRenderer tr = GetComponent<TrailRenderer>();
        if (tr != null)
        {
            tr.time = trailLength;
            tr.minVertexDistance = minVertexDistance;
        }
    }
}
