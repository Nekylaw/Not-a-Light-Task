using UnityEngine;

public class GlowAnimator : MonoBehaviour
{
    private MaterialPropertyBlock mpb;
    private Renderer rend;

    public Color glowColor = Color.white;
    public float minRadius = 0.1f;
    public float maxRadius = 0.3f;
    public float speed = 2.0f;

    public float minIntensity = 1.0f;
    public float maxIntensity = 3.0f;

    private float time;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        time += Time.deltaTime * speed;

        float animatedRadius = Mathf.Lerp(minRadius, maxRadius, (Mathf.Sin(time) + 1.0f) / 2.0f);
        float animatedIntensity = Mathf.Lerp(minIntensity, maxIntensity, (Mathf.Sin(time) + 1.0f) / 2.0f);

        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_GlowColor", glowColor);
        mpb.SetFloat("_GlowRadius", animatedRadius);
        mpb.SetFloat("_GlowIntensity", animatedIntensity);
        rend.SetPropertyBlock(mpb);
    }
}
