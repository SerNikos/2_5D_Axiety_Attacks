using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX; // Required for Visual Effect Graph
using Cinemachine;

public class TriggerLightning : MonoBehaviour
{
    [Header("Visual Effect")]
    public VisualEffect lightningEffect;

    [Header("Audio")]
    public AudioSource audioSource; // Reference to the Audio Source
    public AudioClip lightningSound;  // The thunder/lightning sound clip

    [Header("Impact Knockback")]
    [SerializeField] private Transform impactPoint;
    [SerializeField, Min(0f)] private float impactRadius = 3f;
    [SerializeField, Min(0f)] private float impactForce = 50f;
    [SerializeField, Min(0.01f)] private float impactDuration = 0.5f;
    [SerializeField, Min(0f)] private float impactDelay = 0.15f;

    [Header("Facing Placement")]
    [SerializeField, Min(0f)] private float lightningDistance = 2f;
    [SerializeField] private float lightningHeight = 6.8f;

    [Header("Contextual Render Order")]
    [SerializeField, Min(0f)] private float lightningSortingDuration = 0.75f;
    [SerializeField] private int lightningSortingPriority = 20;

    [Header("Camera Tilt")]
    [SerializeField, Min(0f)] private float cameraTiltImpulse = 0.06f;

    private PlayerController playerController;
    private Vector3 lastFacingDirection = Vector3.forward;
    private Renderer lightningRenderer;
    private DepthSort2D lightningDepthSort;
    private Coroutine sortingPriorityCoroutine;
    private CinemachineImpulseSource cameraImpulseSource;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        SetupCameraImpulse();

        lightningRenderer = GetComponent<Renderer>();
        if (lightningRenderer != null)
        {
            lightningRenderer.sortingLayerName = "Default";
            lightningDepthSort = GetComponent<DepthSort2D>();

            if (lightningDepthSort == null)
            {
                lightningDepthSort = gameObject.AddComponent<DepthSort2D>();
            }

            lightningDepthSort.SetSortDirection(DepthSort2D.SortDirection.CameraDepth);
            lightningDepthSort.SetSpritesOnly(false);
        }
    }

    private void Update()
    {
        UpdateLightningRotation();

        // Check if the 'L' key is pressed down
        if (Input.GetKeyDown(KeyCode.L))
        {
            // Trigger Visual Effect
            if (lightningEffect != null)
            {
                lightningEffect.SendEvent("OnPlay");
            }
            else
            {
                Debug.LogWarning("Lightning Visual Effect reference is missing!");
            }

            StartLightningSortingPriority();

            // Play Sound Effect
            if (audioSource != null && lightningSound != null)
            {
                audioSource.PlayOneShot(lightningSound);
            }
            else if (audioSource == null)
            {
                Debug.LogWarning("Audio Source reference is missing!");
            }

            StartImpactKnockback();
            TriggerCameraTilt();
        }
    }

    private void SetupCameraImpulse()
    {
        cameraImpulseSource = GetComponent<CinemachineImpulseSource>();
        if (cameraImpulseSource == null)
        {
            cameraImpulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        ConfigureImpulseSourceDefaults(cameraImpulseSource);

        EnsureImpulseListenerExists();
    }

    private void EnsureImpulseListenerExists()
    {
        CinemachineImpulseListener[] listeners = FindObjectsOfType<CinemachineImpulseListener>(true);
        if (listeners != null && listeners.Length > 0)
        {
            for (int i = 0; i < listeners.Length; i++)
            {
                ConfigureImpulseListenerDefaults(listeners[i]);
            }

            return;
        }

        CinemachineVirtualCameraBase[] virtualCameras = FindObjectsOfType<CinemachineVirtualCameraBase>(true);
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            if (virtualCameras[i].GetComponent<CinemachineImpulseListener>() == null)
            {
                CinemachineImpulseListener listener = virtualCameras[i].gameObject.AddComponent<CinemachineImpulseListener>();
                ConfigureImpulseListenerDefaults(listener);
            }
        }
    }

    private void ConfigureImpulseSourceDefaults(CinemachineImpulseSource source)
    {
        if (source == null)
        {
            return;
        }

        if (source.m_ImpulseDefinition == null)
        {
            source.m_ImpulseDefinition = new CinemachineImpulseDefinition();
        }

        source.m_ImpulseDefinition.m_ImpulseChannel = 1;
        source.m_ImpulseDefinition.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        source.m_ImpulseDefinition.m_CustomImpulseShape = source.m_ImpulseDefinition.m_CustomImpulseShape ?? new AnimationCurve();
        source.m_ImpulseDefinition.m_ImpulseDuration = 0.2f;
        source.m_ImpulseDefinition.m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        source.m_ImpulseDefinition.m_DissipationDistance = 100f;
        source.m_ImpulseDefinition.m_DissipationRate = 0.25f;
        source.m_ImpulseDefinition.m_PropagationSpeed = 343f;
        source.m_DefaultVelocity = Vector3.right;
    }

    private void ConfigureImpulseListenerDefaults(CinemachineImpulseListener listener)
    {
        if (listener == null)
        {
            return;
        }

        if (listener.m_ChannelMask == 0)
        {
            listener.m_ChannelMask = 1;
        }

        if (listener.m_Gain <= 0f)
        {
            listener.m_Gain = 1f;
        }

        listener.m_ApplyAfter = CinemachineCore.Stage.Noise;
        listener.m_Use2DDistance = false;
        listener.m_UseCameraSpace = true;
    }

    private void TriggerCameraTilt()
    {
        if (cameraImpulseSource == null || cameraTiltImpulse <= 0f)
        {
            return;
        }

        cameraImpulseSource.GenerateImpulseWithVelocity(Vector3.right * cameraTiltImpulse);
    }

    private void UpdateLightningRotation()
    {
        if (playerController == null || playerController.FacingDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Vector3 facingDirection = playerController.FacingDirection;
        facingDirection.y = 0f;
        facingDirection.Normalize();

        transform.position = playerController.transform.position
            + facingDirection * lightningDistance
            + Vector3.up * lightningHeight;
        lastFacingDirection = facingDirection;

        if (lightningDepthSort != null)
        {
            lightningDepthSort.SetSortPosition(GetImpactPosition());
        }
    }

    private void StartLightningSortingPriority()
    {
        if (lightningDepthSort == null)
        {
            return;
        }

        lightningDepthSort.SetOrderOffset(lightningSortingPriority);

        if (sortingPriorityCoroutine != null)
        {
            StopCoroutine(sortingPriorityCoroutine);
        }

        sortingPriorityCoroutine = StartCoroutine(ClearLightningSortingPriority());
    }

    private IEnumerator ClearLightningSortingPriority()
    {
        if (lightningSortingDuration > 0f)
        {
            yield return new WaitForSeconds(lightningSortingDuration);
        }

        if (lightningDepthSort != null)
        {
            lightningDepthSort.ClearOrderOffset();
        }

        sortingPriorityCoroutine = null;
    }

    private void StartImpactKnockback()
    {
        if (impactRadius <= 0f || impactForce <= 0f)
        {
            return;
        }

        StartCoroutine(ApplyImpactKnockbackAfterDelay());
    }

    private IEnumerator ApplyImpactKnockbackAfterDelay()
    {
        if (impactDelay > 0f)
        {
            yield return new WaitForSeconds(impactDelay);
        }

        Vector3 impactPosition = GetImpactPosition();
        Collider[] hitColliders = Physics.OverlapSphere(
            impactPosition,
            impactRadius,
            ~0,
            QueryTriggerInteraction.Collide);
        HashSet<Transform> pushedEnemies = new HashSet<Transform>();

        foreach (Collider hitCollider in hitColliders)
        {
            Transform enemy = FindTaggedEnemy(hitCollider.transform);
            if (enemy == null || !pushedEnemies.Add(enemy))
            {
                continue;
            }

            Vector3 direction = enemy.position - impactPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = enemy.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = lastFacingDirection;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.01f)
            {
                continue;
            }

            PushEnemy(enemy, direction.normalized);
        }
    }

    private Vector3 GetImpactPosition()
    {
        if (impactPoint != null)
        {
            return impactPoint.position;
        }

        if (lightningEffect != null)
        {
            Vector3 effectPosition = lightningEffect.transform.position;
            RaycastHit[] groundHits = Physics.RaycastAll(
                effectPosition,
                Vector3.down,
                100f,
                ~0,
                QueryTriggerInteraction.Ignore);

            bool foundGround = false;
            Vector3 groundPosition = effectPosition;

            foreach (RaycastHit groundHit in groundHits)
            {
                if (FindTaggedEnemy(groundHit.collider.transform) != null)
                {
                    continue;
                }

                if (!foundGround || groundHit.point.y < groundPosition.y)
                {
                    foundGround = true;
                    groundPosition = groundHit.point;
                }
            }

            if (foundGround)
            {
                return groundPosition;
            }

            return effectPosition;
        }

        return transform.position;
    }

    private Transform FindTaggedEnemy(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (current.CompareTag("Enemy"))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private void PushEnemy(Transform enemy, Vector3 direction)
    {
        Rigidbody enemyRigidbody = enemy.GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = enemy.GetComponentInParent<Rigidbody>();
        }

        if (enemyRigidbody != null && !enemyRigidbody.isKinematic)
        {
            enemyRigidbody.AddForce(direction * impactForce, ForceMode.Impulse);
            return;
        }

        EnemyKnockback knockback = enemy.GetComponent<EnemyKnockback>();
        if (knockback == null)
        {
            knockback = enemy.gameObject.AddComponent<EnemyKnockback>();
        }

        knockback.Push(direction, impactForce, impactDuration);
    }

    private void OnDrawGizmosSelected()
    {
        if (impactRadius <= 0f)
        {
            return;
        }

        Vector3 impactPosition = GetImpactPosition();
        Gizmos.color = new Color(1f, 0.6f, 0.05f, 0.75f);
        Gizmos.DrawWireSphere(impactPosition, impactRadius);
        Gizmos.DrawLine(impactPosition, impactPosition + Vector3.up * 0.5f);
    }
}