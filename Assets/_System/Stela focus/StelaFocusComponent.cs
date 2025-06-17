using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class EnhancedFocusComponent : MonoBehaviour
{
    [Header("Refs")]
    public Canvas canvas;
    public Volume postProcessVolume;


    [Header("Target Settings")]
    public Transform focusTarget;
    public float triggerRadius = 10f;
    public bool drawDebugGizmos = true;

    [Header("Camera Focus")]
    public float focusFOV = 35f;
    public float focusDuration = 2f;
    public float holdDuration = 2f;
    public AnimationCurve focusCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual Effects")]
    public bool useVignette = true;
    [Range(0, 1)] public float vignetteIntensity = 0.5f;
    public bool useDepthOfField = true;
    public float dofFocusDistance = 10f;
    public float dofFocalLength = 50f;
    public bool useChromaticAberration = true;
    [Range(0, 1)] public float chromaticIntensity = 0.3f;

    [Header("Motion Effects")]
    public bool useCameraShake = true;
    public float shakeIntensity = 0.1f;
    public float shakeFrequency = 1f;
    public bool useSlowMotion = true;
    [Range(0.1f, 1f)] public float slowMotionScale = 0.5f;

    [Header("UI Effects")]
    public bool showCinematicBars = true;
    public float barHeight = 100f;
    public float barAnimationSpeed = 2f;
    public Color barColor = Color.black;

    [Header("Audio")]
    public AudioClip focusSound;
    public AudioClip ambientSound;
    [Range(0, 1)] public float audioVolume = 0.7f;

    [Header("Material Dissolve")]
    public bool useDissolveEffect = true;
    public Renderer targetRenderer;
    public string dissolvePropertyName = "_DissolveAmount";
    public float dissolveDuration = 3f;
    public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool triggerRevealComponent = true;

    // Private variables
    private Camera mainCamera;
    private Material dissolveMaterial;
    private PlayerController playerController;
    private Vignette vignette;
    private DepthOfField depthOfField;
    private ChromaticAberration chromaticAberration;
    private AudioSource audioSource;

    private float originalFOV;
    private Quaternion originalRotation;
    private float originalTimeScale;
    private bool isPlaying = false;
    private bool hasTriggered = false;

    // UI References
    private GameObject cinematicBarsParent;
    private RectTransform topBar;
    private RectTransform bottomBar;

    private void Start()
    {
        SetupComponents();
        CreateCinematicBars();
        SetupPostProcessing();
    }

    private void SetupComponents()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        originalFOV = mainCamera.fieldOfView;
        originalRotation = mainCamera.transform.rotation;
        originalTimeScale = Time.timeScale;

        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = audioVolume;
        audioSource.spatialBlend = 0f; // 2D sound

        // Setup dissolve material
        if (useDissolveEffect && targetRenderer != null)
        {
            dissolveMaterial = targetRenderer.material;
            if (dissolveMaterial != null && dissolveMaterial.HasProperty(dissolvePropertyName))
            {
                // Start with fully dissolved (invisible)
                dissolveMaterial.SetFloat(dissolvePropertyName, 1f);
            }
        }
    }

    private void CreateCinematicBars()
    {
        if (!showCinematicBars) return;

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("CinematicCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        cinematicBarsParent = new GameObject("CinematicBars");
        cinematicBarsParent.transform.SetParent(canvas.transform, false);

        // Create top bar
        GameObject topBarGO = new GameObject("TopBar");
        topBarGO.transform.SetParent(cinematicBarsParent.transform, false);
        topBar = topBarGO.AddComponent<RectTransform>();
        var topImage = topBarGO.AddComponent<UnityEngine.UI.Image>();
        topImage.color = barColor;

        topBar.anchorMin = new Vector2(0, 1);
        topBar.anchorMax = new Vector2(1, 1);
        topBar.pivot = new Vector2(0.5f, 1);
        topBar.sizeDelta = new Vector2(0, 0);
        topBar.anchoredPosition = Vector2.zero;

        // Create bottom bar
        GameObject bottomBarGO = new GameObject("BottomBar");
        bottomBarGO.transform.SetParent(cinematicBarsParent.transform, false);
        bottomBar = bottomBarGO.AddComponent<RectTransform>();
        var bottomImage = bottomBarGO.AddComponent<UnityEngine.UI.Image>();
        bottomImage.color = barColor;

        bottomBar.anchorMin = new Vector2(0, 0);
        bottomBar.anchorMax = new Vector2(1, 0);
        bottomBar.pivot = new Vector2(0.5f, 0);
        bottomBar.sizeDelta = new Vector2(0, 0);
        bottomBar.anchoredPosition = Vector2.zero;

        cinematicBarsParent.SetActive(false);
    }

    private void SetupPostProcessing()
    {
        if (postProcessVolume == null)
        {
            GameObject ppGO = new GameObject("PostProcessVolume");
            postProcessVolume = ppGO.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
        }

        // Get references to effects
        if (postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out depthOfField);
            postProcessVolume.profile.TryGet(out chromaticAberration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || isPlaying) return;

        if (other.TryGetComponent<PlayerController>(out playerController))
        {
            hasTriggered = true;
            StartCoroutine(PlayCinematicFocus());
        }
    }

    private IEnumerator PlayCinematicFocus()
    {
        isPlaying = true;

        // Disable player controls
        if (playerController != null)
            playerController.enabled = false;

        // Trigger the reveal component if present
        if (triggerRevealComponent)
        {
            var revealComponent = GetComponentInChildren<StelaRevealerComponent>();
            if (revealComponent != null)
                revealComponent.StartReveal();
        }

        // Play focus sound
        if (focusSound != null && audioSource != null)
            audioSource.PlayOneShot(focusSound);

        // Start all effects
        StartCoroutine(AnimateCameraFocus());
        StartCoroutine(AnimatePostProcessing());
        StartCoroutine(AnimateCinematicBars(true));
        StartCoroutine(AnimateDissolve());

        if (useSlowMotion)
            StartCoroutine(AnimateTimeScale(slowMotionScale));

        if (useCameraShake)
            StartCoroutine(CameraShake());

        // Wait for focus duration + hold duration
        yield return new WaitForSecondsRealtime(focusDuration + holdDuration);

        // Reverse all effects
        StartCoroutine(AnimateCameraUnfocus());
        StartCoroutine(AnimatePostProcessingReverse());
        StartCoroutine(AnimateCinematicBars(false));

        if (useSlowMotion)
            StartCoroutine(AnimateTimeScale(1f));

        yield return new WaitForSecondsRealtime(focusDuration);

        // Re-enable player controls
        if (playerController != null)
            playerController.enabled = true;

        isPlaying = false;
    }

    private IEnumerator AnimateCameraFocus()
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        // Calculate target rotation
        Vector3 directionToTarget = focusTarget.position - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(directionToTarget);

        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = focusCurve.Evaluate(elapsed / focusDuration);

            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, focusFOV, t);

            yield return null;
        }
    }

    private IEnumerator AnimateCameraUnfocus()
    {
        float elapsed = 0f;
        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = focusCurve.Evaluate(elapsed / focusDuration);

            mainCamera.transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, originalFOV, t);

            yield return null;
        }
    }

    private IEnumerator AnimatePostProcessing()
    {
        float elapsed = 0f;

        // Store original values
        float origVignette = vignette != null && vignette.active ? vignette.intensity.value : 0f;
        float origChromatic = chromaticAberration != null && chromaticAberration.active ? chromaticAberration.intensity.value : 0f;

        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = focusCurve.Evaluate(elapsed / focusDuration);

            if (useVignette && vignette != null)
            {
                vignette.active = true;
                vignette.intensity.value = Mathf.Lerp(origVignette, vignetteIntensity, t);
            }

            if (useDepthOfField && depthOfField != null)
            {
                depthOfField.active = true;
                depthOfField.focusDistance.value = dofFocusDistance;
                depthOfField.focalLength.value = Mathf.Lerp(300f, dofFocalLength, t);
            }

            if (useChromaticAberration && chromaticAberration != null)
            {
                chromaticAberration.active = true;
                chromaticAberration.intensity.value = Mathf.Lerp(origChromatic, chromaticIntensity, t);
            }

            yield return null;
        }
    }

    private IEnumerator AnimatePostProcessingReverse()
    {
        float elapsed = 0f;

        float startVignette = vignette != null ? vignette.intensity.value : 0f;
        float startChromatic = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;
        float startFocalLength = depthOfField != null ? depthOfField.focalLength.value : 300f;

        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = focusCurve.Evaluate(elapsed / focusDuration);

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(startVignette, 0f, t);

            if (depthOfField != null)
                depthOfField.focalLength.value = Mathf.Lerp(startFocalLength, 300f, t);

            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(startChromatic, 0f, t);

            yield return null;
        }

        // Disable effects
        if (vignette != null) vignette.active = false;
        if (depthOfField != null) depthOfField.active = false;
        if (chromaticAberration != null) chromaticAberration.active = false;
    }

    private IEnumerator AnimateCinematicBars(bool show)
    {
        if (!showCinematicBars || cinematicBarsParent == null) yield break;

        cinematicBarsParent.SetActive(true);

        float elapsed = 0f;
        float duration = 1f / barAnimationSpeed;
        float startHeight = show ? 0f : barHeight;
        float targetHeight = show ? barHeight : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = focusCurve.Evaluate(elapsed / duration);
            float currentHeight = Mathf.Lerp(startHeight, targetHeight, t);

            if (topBar != null)
                topBar.sizeDelta = new Vector2(0, currentHeight);

            if (bottomBar != null)
                bottomBar.sizeDelta = new Vector2(0, currentHeight);

            yield return null;
        }

        if (!show)
            cinematicBarsParent.SetActive(false);
    }

    private IEnumerator AnimateTimeScale(float targetScale)
    {
        float elapsed = 0f;
        float startScale = Time.timeScale;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Time.timeScale = Mathf.Lerp(startScale, targetScale, t);
            yield return null;
        }
    }

    private IEnumerator AnimateDissolve()
    {
        if (!useDissolveEffect || dissolveMaterial == null) yield break;

        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = dissolveCurve.Evaluate(elapsed / dissolveDuration);

            // Animate from 1 (fully dissolved) to 0 (fully visible)
            float dissolveValue = Mathf.Lerp(0f, 1f, t);
            dissolveMaterial.SetFloat(dissolvePropertyName, dissolveValue);

            yield return null;
        }

        // Ensure it's fully visible
        dissolveMaterial.SetFloat(dissolvePropertyName, 0f);
    }

    private IEnumerator CameraShake()
    {
        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < focusDuration + holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float x = Mathf.PerlinNoise(Time.time * shakeFrequency, 0) * shakeIntensity;
            float y = Mathf.PerlinNoise(0, Time.time * shakeFrequency) * shakeIntensity;

            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);

            yield return null;
        }

        mainCamera.transform.localPosition = originalPos;
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        if (focusTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, focusTarget.position);
            Gizmos.DrawWireCube(focusTarget.position, Vector3.one * 0.5f);
        }
    }
}