using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AnxietyLightEvent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private DollyZoom dollyZoom;
    [SerializeField] private AnxietyBreathingClickHold breathing;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private ChargeCircle chargeCircle;

    [Header("Player Color")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string playerColorLayerName = "PlayerColor";

    [Header("UI / Visual References (Opacity Control)")]
    [SerializeField] private CanvasGroup canvasInstructions; // CanvasGroup στο Canvas Instructions
    [SerializeField] private SpriteRenderer circleBreathingSprite; // 🔥 Το SpriteRenderer του Circle Breathing
    [SerializeField] private CanvasGroup circleInnerCanvasGroup; // 🔥 CanvasGroup στον εσωτερικό Canvas του Circle
    [SerializeField] private float uiFadeSpeed = 2f;

    [Header("Light Settings")]
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Focus Mode")]
    [SerializeField] private float saturationFadeSpeed = 2f;

    [Header("Anxiety")]
    [SerializeField] private float anxiety;
    [SerializeField] private float maxAnxiety = 100f;
    [SerializeField] private float minAnxiety = 0f;

    private float originalIntensity;

    private bool running;
    private Coroutine routine;
    private bool ending;

    private bool focusMode;
    private float previousTimeScale = 1f;
    private bool restoreFocusVisualsAfterPanelFade;

    private VolumeProfile originalVolumeProfile;
    private VolumeProfile runtimeVolumeProfile;
    private ColorAdjustments colorAdjustments;
    private float originalSaturation;
    private bool originalSaturationOverride;
    private bool originalColorAdjustmentsActive;
    private Coroutine saturationRoutine;

    private Camera playerColorOverlayCamera;
    private GameObject playerColorOverlayObject;
    private UniversalAdditionalCameraData mainCameraData;
    private UniversalAdditionalCameraData playerColorOverlayCameraData;
    private readonly List<Transform> playerLayerObjects = new List<Transform>();
    private readonly List<int> originalPlayerLayers = new List<int>();
    private int playerColorLayer = -1;
    private int originalMainCameraCullingMask;
    private bool originalMainCameraCullingMaskCaptured;
    private bool playerColorOverlayConfigured;

    private void OnValidate()
    {
        HideUIOnStart();
    }

    private void Awake()
    {
        originalIntensity = directionalLight.intensity;

        HideUIOnStart();
        InitializeRuntimeVolumeProfile();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        RestoreStateImmediately();
        RestorePlayerColorOverlay();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        RestoreStateImmediately();
        RestorePlayerColorOverlay();
        RestoreRuntimeVolumeProfile();
    }

    private void SubscribeToEvents()
    {
        if (breathing != null)
        {
            breathing.OnBreathingStarted -= HandleBreathingStarted;
            breathing.OnBreathingStarted += HandleBreathingStarted;
            breathing.OnInhaleComplete -= HandleInhale;
            breathing.OnInhaleComplete += HandleInhale;
            breathing.OnCycleFinished -= HandleExhale;
            breathing.OnCycleFinished += HandleExhale;
        }

        if (chargeCircle != null)
        {
            chargeCircle.OnAttemptCompleted -= HandleAttemptCompleted;
            chargeCircle.OnAttemptCompleted += HandleAttemptCompleted;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (breathing != null)
        {
            breathing.OnBreathingStarted -= HandleBreathingStarted;
            breathing.OnInhaleComplete -= HandleInhale;
            breathing.OnCycleFinished -= HandleExhale;
        }

        if (chargeCircle != null)
        {
            chargeCircle.OnAttemptCompleted -= HandleAttemptCompleted;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            // Start a new event only when one is not already running or ending
            if (!running && !ending)
            {
                routine = StartCoroutine(EventRoutine());
            }
        }
    }

    void HideUIOnStart()
    {
        if (canvasInstructions != null) canvasInstructions.alpha = 0f;
        if (circleInnerCanvasGroup != null) circleInnerCanvasGroup.alpha = 0f;
        if (circleBreathingSprite != null) SetSpriteAlpha(circleBreathingSprite, 0f);
    }

    // =========================================================
    // EVENT START (ZOOM IN)
    // =========================================================
    IEnumerator EventRoutine()
    {
        running = true;
        ending = false;
        anxiety = 0f;

        breathing.EnableBreathing();
        dollyZoom.EnableAnxiety();

        StartCoroutine(FadeUI(1f));

        yield return FadeLight(0f);

        while (running)
        {
            yield return null;
        }
    }

    void HandleInhale()
    {
        if (!running) return;
        AddAnxiety(20f);
    }

    void HandleBreathingStarted()
    {
        if (!running || ending || focusMode) return;

        focusMode = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        InitializePlayerColorOverlay();
        StartSaturationTransition(-100f, false);
    }

    void HandleAttemptCompleted(bool success)
    {
        restoreFocusVisualsAfterPanelFade = true;

        if (!success)
        {
            if (dollyZoom != null)
            {
                dollyZoom.DisableAnxiety();
            }

            StartCoroutine(EndSequence());
        }
    }

    private void ExitFocusMode(bool restoreWorldColor = true)
    {
        if (!focusMode) return;

        focusMode = false;
        Time.timeScale = previousTimeScale;

        if (restoreWorldColor)
        {
            StartSaturationTransition(originalSaturation, true);
        }
    }

    void HandleExhale()
    {
        if (!running || ending) return;
        ReduceAnxiety(50f);

        if (dollyZoom != null)
        {
            dollyZoom.DisableAnxiety();
        }

        // Stop accepting new mouse input as soon as the breathing cycle finishes.
        // The visual end sequence continues, but another click must not restart breathing.
        breathing.DisableBreathing();

        StartCoroutine(EndSequence());
    }

    // =========================================================
    // END SEQUENCE (ZOOM OUT + FADE OUT)
    // =========================================================
    IEnumerator EndSequence()
    {
        ending = true;

        // 🫁 short emotional delay after exhale
        yield return new WaitForSecondsRealtime(0.3f);

        // 💡 light returns smoothly
        yield return FadeLight(originalIntensity);

        // Fade out the result panel before restoring the world and Player state.
        yield return FadeUIAndResetChargeCircle();

        // ⏳ calm moment
        yield return new WaitForSecondsRealtime(4f);

        running = false;
        ending = false;
    }

    // =========================================================
    // FADE UI & SPRITE HELPER
    // =========================================================
    private IEnumerator FadeUIAndResetChargeCircle()
    {
        yield return FadeUI(0f);

        if (chargeCircle != null)
        {
            chargeCircle.ResetAfterPanelFade();
        }

        if (restoreFocusVisualsAfterPanelFade)
        {
            restoreFocusVisualsAfterPanelFade = false;

            // Resume gameplay only after the result panel has completely disappeared.
            ExitFocusMode(false);
            StartSaturationTransition(originalSaturation, true);

            while (saturationRoutine != null)
            {
                yield return null;
            }

            RestorePlayerColorOverlay();
        }
    }

    IEnumerator FadeUI(float targetAlpha)
    {
        float startInstructionsAlpha = canvasInstructions != null ? canvasInstructions.alpha : 0f;
        float startInnerCanvasAlpha = circleInnerCanvasGroup != null ? circleInnerCanvasGroup.alpha : 0f;
        float startSpriteAlpha = circleBreathingSprite != null ? circleBreathingSprite.color.a : 0f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * uiFadeSpeed;

            // 1. Fade για το Canvas Instructions
            if (canvasInstructions != null)
            {
                canvasInstructions.alpha = Mathf.Lerp(startInstructionsAlpha, targetAlpha, t);
            }

            // 2. Fade για τον εσωτερικό Canvas (Κείμενο του Circle)
            if (circleInnerCanvasGroup != null)
            {
                circleInnerCanvasGroup.alpha = Mathf.Lerp(startInnerCanvasAlpha, targetAlpha, t);
            }

            // 3. Fade για το 2D Sprite του Circle
            if (circleBreathingSprite != null)
            {
                SetSpriteAlpha(circleBreathingSprite, Mathf.Lerp(startSpriteAlpha, targetAlpha, t));
            }

            yield return null;
        }

        // Κλείδωμα στις τελικές τιμές
        if (canvasInstructions != null) canvasInstructions.alpha = targetAlpha;
        if (circleInnerCanvasGroup != null) circleInnerCanvasGroup.alpha = targetAlpha;
        if (circleBreathingSprite != null) SetSpriteAlpha(circleBreathingSprite, targetAlpha);
    }

    void SetSpriteAlpha(SpriteRenderer sprite, float alpha)
    {
        Color color = sprite.color;
        color.a = alpha;
        sprite.color = color;
    }

    void AddAnxiety(float amount)
    {
        anxiety = Mathf.Clamp(anxiety + amount, minAnxiety, maxAnxiety);
    }

    void ReduceAnxiety(float amount)
    {
        anxiety = Mathf.Clamp(anxiety - amount, minAnxiety, maxAnxiety);
    }

    IEnumerator FadeLight(float target)
    {
        float start = directionalLight.intensity;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * fadeSpeed;
            directionalLight.intensity = Mathf.Lerp(start, target, t);
            yield return null;
        }

        directionalLight.intensity = target;
    }

    private void InitializeRuntimeVolumeProfile()
    {
        if (globalVolume == null) return;

        originalVolumeProfile = globalVolume.sharedProfile;
        if (originalVolumeProfile == null)
        {
            originalVolumeProfile = globalVolume.profile;
        }

        if (originalVolumeProfile == null) return;

        runtimeVolumeProfile = Instantiate(originalVolumeProfile);
        runtimeVolumeProfile.name = originalVolumeProfile.name + " (Runtime)";
        globalVolume.profile = runtimeVolumeProfile;

        if (!runtimeVolumeProfile.TryGet(out colorAdjustments))
        {
            colorAdjustments = runtimeVolumeProfile.Add<ColorAdjustments>(true);
            originalSaturation = 0f;
            originalSaturationOverride = false;
            originalColorAdjustmentsActive = false;
        }
        else
        {
            originalSaturation = colorAdjustments.saturation.value;
            originalSaturationOverride = colorAdjustments.saturation.overrideState;
            originalColorAdjustmentsActive = colorAdjustments.active;
        }
    }

    private void StartSaturationTransition(float target, bool restoreOriginalState)
    {
        if (colorAdjustments == null) return;

        if (saturationRoutine != null)
        {
            StopCoroutine(saturationRoutine);
        }

        saturationRoutine = StartCoroutine(AnimateSaturation(target, restoreOriginalState));
    }

    private IEnumerator AnimateSaturation(float target, bool restoreOriginalState)
    {
        float start = colorAdjustments.saturation.value;
        float t = 0f;
        float speed = Mathf.Max(0.01f, saturationFadeSpeed);

        colorAdjustments.active = true;
        colorAdjustments.saturation.overrideState = true;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * speed;
            colorAdjustments.saturation.value = Mathf.Lerp(start, target, t);
            yield return null;
        }

        colorAdjustments.saturation.value = target;

        if (restoreOriginalState)
        {
            colorAdjustments.saturation.overrideState = originalSaturationOverride;
            colorAdjustments.active = originalColorAdjustmentsActive;
        }

        saturationRoutine = null;
    }

    private void RestoreStateImmediately()
    {
        restoreFocusVisualsAfterPanelFade = false;

        if (focusMode)
        {
            focusMode = false;
            Time.timeScale = previousTimeScale;
        }

        if (saturationRoutine != null)
        {
            StopCoroutine(saturationRoutine);
            saturationRoutine = null;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = originalSaturation;
            colorAdjustments.saturation.overrideState = originalSaturationOverride;
            colorAdjustments.active = originalColorAdjustmentsActive;
        }
    }

    private void RestoreRuntimeVolumeProfile()
    {
        if (globalVolume != null && originalVolumeProfile != null)
        {
            globalVolume.profile = originalVolumeProfile;
        }

        if (runtimeVolumeProfile != null && runtimeVolumeProfile != originalVolumeProfile)
        {
            Destroy(runtimeVolumeProfile);
        }

        runtimeVolumeProfile = null;
    }

    private void InitializePlayerColorOverlay()
    {
        if (playerColorOverlayConfigured)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        playerColorLayer = LayerMask.NameToLayer(playerColorLayerName);
        if (playerColorLayer < 0)
        {
            Debug.LogError($"The layer '{playerColorLayerName}' does not exist.", this);
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            return;
        }

        mainCameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (mainCameraData == null || mainCameraData.renderType != CameraRenderType.Base)
        {
            Debug.LogError("Player color rendering requires the Main Camera to be a URP Base camera.", this);
            return;
        }

        if (mainCameraData.cameraStack == null)
        {
            Debug.LogError("Player color rendering requires a URP camera stack.", this);
            return;
        }

        originalMainCameraCullingMask = mainCamera.cullingMask;
        originalMainCameraCullingMaskCaptured = true;
        SetPlayerLayerRecursively(player.transform);
        mainCamera.cullingMask = originalMainCameraCullingMask & ~(1 << playerColorLayer);

        CreatePlayerColorOverlay();

        if (playerColorOverlayCameraData == null)
        {
            RestorePlayerColorOverlay();
            return;
        }

        if (!mainCameraData.cameraStack.Contains(playerColorOverlayCamera))
        {
            mainCameraData.cameraStack.Add(playerColorOverlayCamera);
        }

        playerColorOverlayConfigured = true;
    }

    private void CreatePlayerColorOverlay()
    {
        playerColorOverlayObject = new GameObject("Player Color Overlay Camera");
        playerColorOverlayObject.transform.SetParent(mainCamera.transform, false);

        playerColorOverlayCamera = playerColorOverlayObject.AddComponent<Camera>();
        playerColorOverlayCameraData = playerColorOverlayObject.AddComponent<UniversalAdditionalCameraData>();
        playerColorOverlayCameraData.renderType = CameraRenderType.Overlay;
        playerColorOverlayCameraData.renderPostProcessing = false;
        playerColorOverlayCameraData.renderShadows = false;
        playerColorOverlayCameraData.requiresDepthTexture = false;
        playerColorOverlayCameraData.requiresColorTexture = false;
        playerColorOverlayCameraData.volumeLayerMask = 0;

        SyncPlayerColorOverlay();
    }

    private void LateUpdate()
    {
        if (playerColorOverlayConfigured)
        {
            SyncPlayerColorOverlay();
        }
    }

    private void SyncPlayerColorOverlay()
    {
        if (playerColorOverlayCamera == null || mainCamera == null)
        {
            return;
        }

        playerColorOverlayCamera.CopyFrom(mainCamera);
        playerColorOverlayCamera.cullingMask = 1 << playerColorLayer;
        playerColorOverlayCamera.clearFlags = CameraClearFlags.Nothing;
        playerColorOverlayCamera.enabled = true;
        playerColorOverlayCamera.transform.localPosition = Vector3.zero;
        playerColorOverlayCamera.transform.localRotation = Quaternion.identity;
        playerColorOverlayCamera.transform.localScale = Vector3.one;
    }

    private void SetPlayerLayerRecursively(Transform target)
    {
        playerLayerObjects.Add(target);
        originalPlayerLayers.Add(target.gameObject.layer);
        target.gameObject.layer = playerColorLayer;

        for (int i = 0; i < target.childCount; i++)
        {
            SetPlayerLayerRecursively(target.GetChild(i));
        }
    }

    private void RestorePlayerColorOverlay()
    {
        if (mainCameraData != null && playerColorOverlayCamera != null && mainCameraData.cameraStack != null)
        {
            mainCameraData.cameraStack.Remove(playerColorOverlayCamera);
        }

        if (playerColorOverlayObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(playerColorOverlayObject);
            }
            else
            {
                DestroyImmediate(playerColorOverlayObject);
            }
        }

        for (int i = 0; i < playerLayerObjects.Count; i++)
        {
            if (playerLayerObjects[i] != null)
            {
                playerLayerObjects[i].gameObject.layer = originalPlayerLayers[i];
            }
        }

        if (mainCamera != null && originalMainCameraCullingMaskCaptured)
        {
            mainCamera.cullingMask = originalMainCameraCullingMask;
        }

        playerLayerObjects.Clear();
        originalPlayerLayers.Clear();
        playerColorOverlayObject = null;
        playerColorOverlayCamera = null;
        playerColorOverlayCameraData = null;
        originalMainCameraCullingMaskCaptured = false;
        playerColorOverlayConfigured = false;
    }
}