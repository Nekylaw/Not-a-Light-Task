using System;
using Game.Services.LightSources;
using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;

public class OrbComponent : MonoBehaviour
{
    [SerializeField]
    private OrbSettings _orbSettings = null;
    [SerializeField]
    private OrbSettings _absorbSettings = null;

    private Coroutine _attractOrbCoroutine = null;
    private Coroutine _eatingCoroutine = null;

    private Renderer orbRenderer;
    private MaterialPropertyBlock propBlock;
    private Collider orbCollider;
    private Rigidbody orbRigidbody;

    private bool isAbsorbed = false;

    // State tracking
    private bool isBeingEaten = false;
    public bool IsActive => enabled && !isBeingEaten;

    // Events
    public delegate void OrbEatenDelegate();
    public OrbEatenDelegate OnEaten = null;
    
    //original look
    private Vector3 ab_originalScale;
    private Color ab_originalColor;

    private void Awake()
    {
        orbRenderer = GetComponent<Renderer>();
        orbCollider = GetComponent<Collider>();
        orbRigidbody = GetComponent<Rigidbody>();
        propBlock = new MaterialPropertyBlock();
    }

    public void AttractTo(Vector3 lightPoint, LightSourceComponent lightSource)
    {
        if (_attractOrbCoroutine != null || isBeingEaten)
            return;

        _attractOrbCoroutine = StartCoroutine(AttractOrbCoroutine(lightPoint, lightSource));
    }

    private IEnumerator AttractOrbCoroutine(Vector3 lightPoint, LightSourceComponent lightSource)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        float timer = 0f;
        float duration = lightSource.Settings.Duration;
        float spiralSpeed = lightSource.Settings.SpiralSpeed;
        float radius = lightSource.Settings.AttractRange;

        Vector3 startPosition = transform.position;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float currentRadius = Mathf.Lerp(radius, 0f, t);
            float angle = spiralSpeed * t * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius;
            Vector3 directionToCenter = (lightPoint - startPosition).normalized;
            Quaternion rotationToTarget = Quaternion.LookRotation(directionToCenter);
            Vector3 orbitalOffset = rotationToTarget * offset;

            transform.position = Vector3.Slerp(startPosition, lightPoint, t) + orbitalOffset;
            yield return null;
        }

        transform.position = lightPoint;
        LightSourcesService.Instance.SwitchOn(lightSource);
        _attractOrbCoroutine = null;
        Destroy(this.gameObject);
    }

    // Called when a creature starts eating this orb
    public void StartBeingEaten()
    {
        if (isBeingEaten)
            return;

        isBeingEaten = true;

        // Cancel any ongoing attraction
        if (_attractOrbCoroutine != null)
        {
            StopCoroutine(_attractOrbCoroutine);
            _attractOrbCoroutine = null;
        }

        // Stop physics
        if (orbRigidbody != null)
        {
            orbRigidbody.linearVelocity = Vector3.zero;
            orbRigidbody.isKinematic = true;
        }

        // Disable collider so other creatures can't target it
        if (orbCollider != null)
        {
            orbCollider.enabled = false;
        }

        // Start eating effect
        _eatingCoroutine = StartCoroutine(EatingEffectCoroutine());
    }

    // Called when the creature finishes eating
    public void BeEaten()
    {
        if (_eatingCoroutine != null)
        {
            StopCoroutine(_eatingCoroutine);
        }

        // Trigger any eaten events
        OnEaten?.Invoke();

        // Destroy the orb
        Destroy(gameObject);
    }

    public IEnumerator EatingEffectCoroutine()
    {
        float timer = 0f;
        Vector3 originalScale = transform.localScale;
        Color originalColor = Color.white;

        if (orbRenderer != null)
        {
            orbRenderer.GetPropertyBlock(propBlock);
            if (propBlock.HasProperty("_BaseColor"))
            {
                originalColor = propBlock.GetColor("_BaseColor");
            }
            else if (orbRenderer.material.HasProperty("_BaseColor"))
            {
                originalColor = orbRenderer.material.GetColor("_BaseColor");
            }
        }

        while (timer < _orbSettings.EatingEffectDuration)
        {
            timer += Time.deltaTime;
            float t = timer / _orbSettings.EatingEffectDuration;

            // Scale effect
            float scaleMultiplier = _orbSettings.ScaleCurve.Evaluate(t);
            transform.localScale = originalScale * scaleMultiplier;

            // Visual effect - pulsing and fading
            if (orbRenderer != null)
            {
                float alpha = _orbSettings.AlphaCurve.Evaluate(t);
                Color currentColor = originalColor;
                currentColor.a = alpha;

                // Add pulsing effect
                float pulse = Mathf.Sin(t * Mathf.PI * 8f) * 0.3f + 1f;
                currentColor *= pulse;

                orbRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", currentColor);

                //// If using standard shader
                //if (orbRenderer.material.HasProperty("_Color"))
                //{
                //    propBlock.SetColor("_Color", currentColor);
                //}

                orbRenderer.SetPropertyBlock(propBlock);
            }

            // Small rotation while being eaten
            transform.Rotate(Vector3.up * 200f * Time.deltaTime);

            yield return null;
        }

        _eatingCoroutine = null;
    }

    public IEnumerator AbsorbingOrbCoroutine()
    {
        isAbsorbed = true;
        float timer = 0f;
        ab_originalScale = transform.localScale;
        ab_originalColor = orbRenderer.material.GetColor("_BaseColor");
        
        Vector3 originalScale = transform.localScale;
        Color originalColor = orbRenderer.material.GetColor("_BaseColor");
        

        if (orbRenderer != null)
        {
            orbRenderer.GetPropertyBlock(propBlock);
            if (propBlock.HasProperty("_BaseColor"))
            {
                originalColor = propBlock.GetColor("_BaseColor");
            }
            else if (orbRenderer.material.HasProperty("_BaseColor"))
            {
                originalColor = orbRenderer.material.GetColor("_BaseColor");
            }
        }

        while (timer < _absorbSettings.EatingEffectDuration)
        {
            timer += Time.deltaTime;
            float t = timer / _absorbSettings.EatingEffectDuration;

            // Scale effect
            float scaleMultiplier = _absorbSettings.ScaleCurve.Evaluate(t);
            transform.localScale = originalScale * scaleMultiplier;

            // Visual effect - pulsing and fading
            if (orbRenderer != null)
            {
                float alpha = _absorbSettings.AlphaCurve.Evaluate(t);
                Color currentColor = originalColor;
                currentColor.a = alpha;

                // Add pulsing effect
                float pulse = Mathf.Sin(t * Mathf.PI * 8f) * 0.3f + 1f;
                currentColor *= pulse;

                orbRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", currentColor);

                //// If using standard shader
                //if (orbRenderer.material.HasProperty("_Color"))
                //{
                //    propBlock.SetColor("_Color", currentColor);
                //}

                orbRenderer.SetPropertyBlock(propBlock);
            }

            // Small rotation while being eaten
            transform.Rotate(Vector3.up * 200f * Time.deltaTime);

            yield return null;
        }
    }
    
    

    // Helper method to check if orb can be targeted
    public bool CanBeTargeted()
    {
        return IsActive && !isBeingEaten && gameObject.activeInHierarchy;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") && isBeingEaten)
        {
            isBeingEaten = false;
            
            transform.localScale = ab_originalScale;
            gameObject.GetComponent<Renderer>().material.color = ab_originalColor;
        }
    }
}