using UnityEngine;

public class TrailBehaviour : MonoBehaviour
{
    public Transform Pos1;
    public Transform Pos2;

    [Header("Mouvement")]
    public float speed = 2f;
    public float smoothTime = 0.3f;
    public float waitDuration = 1f;

    [Header("Oscillation")]
    public float baseFrequency = 1f;
    public float baseAmplitude = 1f;
    public float tiltAmount = 0.4f;

    [Header("Flottement vertical")]
    public float floatFrequency = 0.5f;
    public float floatAmplitude = 0.2f;

    private Vector3 velocity = Vector3.zero;
    private float travelPercent = 0f;
    private bool goingToPos2 = true;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    void Update()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
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

        float distance = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        Vector3 orthogonal = Vector3.Cross(direction, Vector3.up);
        Vector3 tiltDirection = (orthogonal * tiltAmount + direction * 0.2f).normalized;

        // Avancement fluide entre les deux points
        travelPercent += Time.deltaTime * speed / distance;
        travelPercent = Mathf.Clamp01(travelPercent);

        Vector3 basePos = Vector3.Lerp(start, end, travelPercent);

        // Bruit de Perlin pour moduler fréquence/amplitude
        float dynamicFrequency = baseFrequency + Mathf.PerlinNoise(Time.time * 0.3f, 0f) * 0.5f;
        float dynamicAmplitude = baseAmplitude + Mathf.PerlinNoise(0f, Time.time * 0.3f) * 0.3f;

        float sinOffset = Mathf.Sin(Time.time * dynamicFrequency * Mathf.PI * 2f) * dynamicAmplitude;
        Vector3 lateralOffset = tiltDirection * sinOffset;

        // Flottement vertical
        float verticalOffset = Mathf.Sin(Time.time * floatFrequency * Mathf.PI * 2f) * floatAmplitude;
        Vector3 finalTarget = basePos + lateralOffset + new Vector3(0, verticalOffset, 0);

        // Mouvement lissé
        transform.position = Vector3.SmoothDamp(transform.position, finalTarget, ref velocity, smoothTime);

        // Orientation progressive
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }

        // Si arrivé, pause avant de repartir
        if (travelPercent >= 1f)
        {
            isWaiting = true;
        }
    }
}
