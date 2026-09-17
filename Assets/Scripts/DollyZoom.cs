using UnityEngine;
using Cinemachine;

public class DollyZoom : MonoBehaviour
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    private CinemachineTransposer transposer;
    private Camera mainCam;

    // =====================================================
    // ANXIETY STATE
    // =====================================================

    [Header("Anxiety State")]
    [SerializeField] private float anxietyDistance = 5f;
    [SerializeField] private float anxietyFOV = 25f;
    [SerializeField] private float anxietyVerticalOffset = 0.20f;

    // =====================================================
    // TRANSITION
    // =====================================================

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 3f;

    // =====================================================
    // SHAKE
    // =====================================================

    [Header("Shake")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeAmount = 0.01f;

    public float shakeSpeed = 35f;

    // =====================================================
    // STATE
    // =====================================================

    // Current state
    private bool anxious = false;

    // Auto captured normal values
    private float normalDistance;
    private float normalFOV;

    // Runtime interpolated values
    private float currentDistance;
    private float currentFOV;
    private float currentVerticalOffset;

    // ORIGINAL stable camera offset
    private Vector3 baseOffset;

    // =====================================================
    // UNITY METHODS
    // =====================================================

    private void Start()
    {
        // Get Cinemachine components
        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        mainCam = Camera.main;

        // Save ORIGINAL offset once
        baseOffset = transposer.m_FollowOffset;

        // Capture current editor setup automatically
        normalDistance = -baseOffset.z;
        normalFOV = vcam.m_Lens.FieldOfView;

        // Initialize runtime values
        currentDistance = normalDistance;
        currentFOV = normalFOV;
        currentVerticalOffset = 0f;
    }

    private void LateUpdate()
    {
        // ---------------------------------------------------
        // TESTING WITH Z KEY
        // ---------------------------------------------------

        if (Input.GetKeyDown(KeyCode.Z))
        {
            anxious = !anxious;
        }

        // Determine target values
        float targetDistance = anxious ? anxietyDistance : normalDistance;
        float targetFOV = anxious ? anxietyFOV : normalFOV;
        float targetVerticalOffset = anxious ? anxietyVerticalOffset : 0f;

        // Smooth dolly movement
        currentDistance = Mathf.Lerp(
            currentDistance,
            targetDistance,
            Time.deltaTime * transitionSpeed
        );

        // Smooth lens zoom
        currentFOV = Mathf.Lerp(
            currentFOV,
            targetFOV,
            Time.deltaTime * transitionSpeed
        );

        currentVerticalOffset = Mathf.Lerp(
            currentVerticalOffset,
            targetVerticalOffset,
            Time.deltaTime * transitionSpeed
        );

        ApplyCamera();
    }

    // =====================================================
    // CAMERA LOGIC
    // =====================================================

    private void ApplyCamera()
    {
        // ALWAYS start from clean stable base
        Vector3 offset = baseOffset;

        // Apply dolly zoom distance
        offset.z = -currentDistance;
        offset.y += currentVerticalOffset;

        // Apply shake on top
        if (anxious && enableShake)
        {
            float shakeX =
                Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;

            float shakeY =
                Mathf.Cos(Time.time * shakeSpeed * 0.8f) * shakeAmount;

            offset.x += shakeX;
            offset.y += shakeY;
        }

        // Apply camera offset
        transposer.m_FollowOffset = offset;

        // Apply FOV
        vcam.m_Lens.FieldOfView = currentFOV;
    }

    // =====================================================
    // PUBLIC API
    // =====================================================

    public void EnableAnxiety()
    {
        anxious = true;
    }

    public void DisableAnxiety()
    {
        anxious = false;
        shakeSpeed = 35;
    }

    public void SetAnxiety(bool state)
    {
        anxious = state;
    }

    public bool IsAnxious()
    {
        return anxious;
    }

}