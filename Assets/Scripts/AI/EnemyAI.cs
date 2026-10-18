using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3.5f;

    [Header("Detection Settings")]
    public float aggroRadius = 5f;        // Ακτίνα για να αρχίσει το κυνήγι
    public float losePlayerRadius = 12f;  // Ακτίνα για να χάσει τον παίκτη

    [Header("Patrol Settings")]
    public float patrolRange = 8f;        // Ακτίνα γύρω από το αρχικό σημείο για την περιπολία

    [Header("Visualization Settings")]
    public bool showRadiusSpheres = true; // Ενεργοποίηση/Απενεργοποίηση των σφαιρών

    [Header("Sprite Animation")]
    [SerializeField, Tooltip("Assign in order: slime_walk_0001, slime_walk_0002, slime_walk_0003, slime_walk_0002.")]
    private Sprite[] idleAnimationFrames = new Sprite[4];
    [SerializeField, Tooltip("Assign hurt_002 and hurt_003; one is selected randomly on each spell hit.")]
    private Sprite[] hurtAnimationFrames = new Sprite[2];
    [SerializeField, Min(0.01f)] private float idleFrameDuration = 0.18f;
    [SerializeField, Min(0f)] private float hurtAnimationDuration = 0.35f;

    private float distanceToPlayer;
    private float nextPatrolAttemptTime;
    private float nextChaseDestinationTime;
    private float nextNavigationWarningTime;
    private Vector3 lastChaseDestination;
    private bool hasChaseDestination;
    private bool hasReportedPatrolFailure;
    private bool hasReportedInvalidPath;
    private Vector3 originPos;            // Το κέντρο της περιοχής περιπολίας
    private NavMeshAgent agent;
    private DepthSort2D depthSort;
    private SpriteRenderer enemySpriteRenderer;
    private Color originalSpriteColor;
    private float idleFrameElapsed;
    private float hurtAnimationEndsAt;
    private int idleFrameIndex;
    private bool isPlayingHurtAnimation;
    private bool isUsingFallbackHurtTint;
    private float frozenUntil;
    private bool agentWasStoppedBeforeFreeze;

    // References για τα GameObjects των σφαιρών
    private GameObject aggroObj;
    private GameObject loseObj;

    public enum EnemyState
    {
        Patrol,
        Chase
    }

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Patrol;

    public bool IsFrozen => Time.time < frozenUntil;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Find the renderer on the enemy's visual child so the AI root can stay collider-focused.
        enemySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (enemySpriteRenderer != null)
        {
            originalSpriteColor = enemySpriteRenderer.color;
        }
        originPos = transform.position;

        // Start on the first idle image when frames have been assigned in the Inspector.
        ApplyIdleAnimationFrame();

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        depthSort = GetComponent<DepthSort2D>();

        if (depthSort == null)
        {
            depthSort = gameObject.AddComponent<DepthSort2D>();
        }

        depthSort.SetSortDirection(DepthSort2D.SortDirection.CameraDepth);
        depthSort.SetSpritesOnly(true);

        currentState = EnemyState.Patrol;

        if (showRadiusSpheres)
        {
            CreateRuntimeSpheres();
        }
    }

    private void Start()
    {
        SetRandomPatrolDestination();
        Debug.Log($"Enemy '{name}' startup navigation: speed={agent.speed}, stopped={agent.isStopped}, onNavMesh={agent.isOnNavMesh}, hasPath={agent.hasPath}, pathPending={agent.pathPending}, pathStatus={agent.pathStatus}, timeScale={Time.timeScale}, agentType={agent.agentTypeID}.", this);
    }

    private void Update()
    {
        // Keep sprite animation independent from navigation and freeze state.
        UpdateSpriteAnimation();

        if (showRadiusSpheres && aggroObj == null && loseObj == null)
        {
            CreateRuntimeSpheres();
        }

        // Ενημέρωση της ορατότητας των σφαιρών σε runtime αν αλλάξει η μεταβλητή από τον Inspector
        UpdateSpheresVisibility();

        if (IsFrozen)
        {
            StopAgentMovement(false);
            return;
        }

        if (!EnsureAgentOnNavMesh()) return;

        if (frozenUntil > 0f)
        {
            agent.isStopped = agentWasStoppedBeforeFreeze;
            frozenUntil = 0f;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player == null) return;

        distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
        }
    }

    public void FreezeFor(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (Time.time >= frozenUntil)
        {
            EnemyKnockback knockback = GetComponent<EnemyKnockback>();
            if (knockback == null)
            {
                knockback = GetComponentInChildren<EnemyKnockback>();
            }
            if (knockback == null)
            {
                knockback = GetComponentInParent<EnemyKnockback>();
            }

            // Restore the state from before any shove before saving the state for the freeze.
            if (knockback != null)
            {
                knockback.CancelForFreeze();
            }

            agentWasStoppedBeforeFreeze = agent != null && agent.isStopped;
            hasChaseDestination = false;
            nextChaseDestinationTime = 0f;
            nextPatrolAttemptTime = 0f;
        }

        frozenUntil = Mathf.Max(frozenUntil, Time.time + duration);
        StopAgentMovement(true);
    }

    public void PlaySpellHitAnimation()
    {
        if (enemySpriteRenderer == null)
        {
            return;
        }

        enemySpriteRenderer.color = originalSpriteColor;
        isUsingFallbackHurtTint = false;

        // Select a valid hurt image at random, skipping any frame slots not yet assigned.
        Sprite hurtFrame = null;
        if (hurtAnimationFrames != null && hurtAnimationFrames.Length > 0)
        {
            int startIndex = Random.Range(0, hurtAnimationFrames.Length);
            for (int offset = 0; offset < hurtAnimationFrames.Length; offset++)
            {
                hurtFrame = hurtAnimationFrames[(startIndex + offset) % hurtAnimationFrames.Length];
                if (hurtFrame != null)
                {
                    break;
                }
            }
        }

        if (hurtFrame != null)
        {
            enemySpriteRenderer.sprite = hurtFrame;
        }
        else
        {
            // Give a visible hit cue until hurt sprites are assigned in the Inspector.
            Color hurtTint = Color.Lerp(originalSpriteColor, Color.red, 0.75f);
            hurtTint.a = originalSpriteColor.a;
            enemySpriteRenderer.color = hurtTint;
            isUsingFallbackHurtTint = true;
        }

        hurtAnimationEndsAt = Time.time + hurtAnimationDuration;
        isPlayingHurtAnimation = true;
    }

    private void UpdateSpriteAnimation()
    {
        // Hold the hurt image briefly, then restart the idle sequence from frame one.
        if (isPlayingHurtAnimation)
        {
            if (Time.time < hurtAnimationEndsAt)
            {
                return;
            }

            isPlayingHurtAnimation = false;
            if (isUsingFallbackHurtTint && enemySpriteRenderer != null)
            {
                enemySpriteRenderer.color = originalSpriteColor;
                isUsingFallbackHurtTint = false;
            }

            idleFrameIndex = 0;
            idleFrameElapsed = 0f;
            ApplyIdleAnimationFrame();
            return;
        }

        if (enemySpriteRenderer == null || idleAnimationFrames == null || idleAnimationFrames.Length == 0)
        {
            return;
        }

        // Advance one configured idle frame each interval, wrapping through the supplied sequence.
        idleFrameElapsed += Time.deltaTime;
        if (idleFrameElapsed < idleFrameDuration)
        {
            return;
        }

        idleFrameElapsed %= idleFrameDuration;
        idleFrameIndex = (idleFrameIndex + 1) % idleAnimationFrames.Length;
        ApplyIdleAnimationFrame();
    }

    private void ApplyIdleAnimationFrame()
    {
        if (enemySpriteRenderer == null || idleAnimationFrames == null || idleAnimationFrames.Length == 0)
        {
            return;
        }

        // Ignore empty slots so missing art does not hide the current enemy sprite.
        Sprite idleFrame = idleAnimationFrames[idleFrameIndex];
        if (idleFrame != null)
        {
            enemySpriteRenderer.sprite = idleFrame;
        }
    }

    private void StopAgentMovement(bool clearPath)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            if (clearPath && agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }

    private void UpdatePatrol()
    {
        // 1. Αν ο παίκτης πλησιάσει, άλλαξε state σε Chase
        if (distanceToPlayer <= aggroRadius)
        {
            currentState = EnemyState.Chase;
            hasChaseDestination = false;
            nextChaseDestinationTime = 0f;
            return;
        }

        // 2. Περιπολία: Μόλις φτάσει στο σημείο, διάλεξε νέο τυχαίο σημείο στο NavMesh
        if (!agent.pathPending &&
            (agent.pathStatus != NavMeshPathStatus.PathComplete ||
             !agent.hasPath ||
             agent.remainingDistance <= agent.stoppingDistance) &&
            Time.time >= nextPatrolAttemptTime)
        {
            if (agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                WarnNavigationIssue($"patrol path is {agent.pathStatus}");
                hasReportedInvalidPath = true;
            }
            else if (hasReportedInvalidPath)
            {
                hasReportedInvalidPath = false;
            }

            SetRandomPatrolDestination();
        }
    }

    private void UpdateChase()
    {
        bool playerMoved = !hasChaseDestination ||
                           (player.position - lastChaseDestination).sqrMagnitude >= 0.25f;
        bool pathNeedsRetry = !agent.pathPending &&
                              (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete);

        if (agent.isOnNavMesh &&
            Time.time >= nextChaseDestinationTime &&
            (playerMoved || pathNeedsRetry))
        {
            nextChaseDestinationTime = Time.time + 0.25f;
            if (!agent.SetDestination(player.position))
            {
                WarnNavigationIssue("could not set a chase destination");
            }
            else
            {
                lastChaseDestination = player.position;
                hasChaseDestination = true;
            }
        }

        if (!agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            WarnNavigationIssue($"chase path is {agent.pathStatus}");
        }
        else if (!agent.pathPending && !agent.hasPath)
        {
            WarnNavigationIssue("chase has no path after requesting a destination");
        }

        // Αν ο παίκτης φύγει πολύ μακριά, γύρνα αμέσως σε Patrol
        if (distanceToPlayer >= losePlayerRadius)
        {
            currentState = EnemyState.Patrol;
            hasChaseDestination = false;
            SetRandomPatrolDestination();
        }
    }

    private void SetRandomPatrolDestination()
    {
        if (!EnsureAgentOnNavMesh()) return;

        nextPatrolAttemptTime = Time.time + 1f;
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        NavMeshHit hit;
        bool foundNavMeshPoint = false;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 randomPoint = originPos + new Vector3(
                Random.Range(-patrolRange, patrolRange),
                0f,
                Random.Range(-patrolRange, patrolRange));

            if (!NavMesh.SamplePosition(randomPoint, out hit, Mathf.Max(agent.height, patrolRange), filter))
            {
                continue;
            }

            foundNavMeshPoint = true;
            if (agent.SetDestination(hit.position))
            {
                hasReportedPatrolFailure = false;
                return;
            }
        }

        if (!hasReportedPatrolFailure)
        {
            WarnNavigationIssue(foundNavMeshPoint
                ? "found NavMesh points but SetDestination rejected all patrol destinations"
                : $"could not sample a patrol point within {Mathf.Max(agent.height, patrolRange):0.##} units of the sampled positions");
            hasReportedPatrolFailure = true;
        }
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.isActiveAndEnabled)
        {
            return false;
        }

        if (agent.isOnNavMesh)
        {
            return true;
        }

        NavMeshHit hit;
        float sampleDistance = Mathf.Max(2f, agent.height);
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (NavMesh.SamplePosition(transform.position, out hit, sampleDistance, filter) &&
            agent.Warp(hit.position))
        {
            originPos = hit.position;
            return agent.isOnNavMesh;
        }

        WarnNavigationIssue($"agent is not on a NavMesh and could not warp to one (agent type {agent.agentTypeID})");
        return false;
    }

    private void WarnNavigationIssue(string message)
    {
        if (Time.time < nextNavigationWarningTime)
        {
            return;
        }

        nextNavigationWarningTime = Time.time + 5f;
        Debug.LogWarning($"Enemy '{name}': {message}. Position: {transform.position}, speed={agent.speed}, stopped={agent.isStopped}, onNavMesh={agent.isOnNavMesh}, hasPath={agent.hasPath}, pathPending={agent.pathPending}, pathStatus={agent.pathStatus}, timeScale={Time.timeScale}, agentType={agent.agentTypeID}.", this);
    }

    // Δημιουργεί 2 ημιδιάφανες σφαίρες ως children του εχθρού για το Runtime
    private void CreateRuntimeSpheres()
    {
        // 1. Σφαίρα Aggro Radius (Κίτρινη)
        aggroObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aggroObj.name = "Runtime_AggroRadius";
        aggroObj.transform.SetParent(transform);
        aggroObj.transform.localPosition = Vector3.zero;
        aggroObj.transform.localScale = Vector3.one * (aggroRadius * 2f);

        Destroy(aggroObj.GetComponent<Collider>()); // Αφαίρεση collider για να μην επηρεάζει τα physics

        Renderer aggroRend = aggroObj.GetComponent<Renderer>();
        SetTransparentMaterialURP(aggroRend, new Color(1f, 0.92f, 0.016f, 0.25f)); // Ημιδιάφανο κίτρινο

        // 2. Σφαίρα Lose Player Radius (Κόκκινη)
        loseObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        loseObj.name = "Runtime_LoseRadius";
        loseObj.transform.SetParent(transform);
        loseObj.transform.localPosition = Vector3.zero;
        loseObj.transform.localScale = Vector3.one * (losePlayerRadius * 2f);

        Destroy(loseObj.GetComponent<Collider>());

        Renderer loseRend = loseObj.GetComponent<Renderer>();
        SetTransparentMaterialURP(loseRend, new Color(1f, 0f, 0f, 0.15f)); // Ημιδιάφανο κόκκινο

        // Αρχικός ορισμός ορατότητας βάσει της boolean μεταβλητής
        UpdateSpheresVisibility();
    }

    // Ελέγχει και ενεργοποιεί/απενεργοποιεί τα GameObjects των σφαιρών
    private void UpdateSpheresVisibility()
    {
        if (aggroObj != null && aggroObj.activeSelf != showRadiusSpheres)
        {
            aggroObj.SetActive(showRadiusSpheres);
        }

        if (loseObj != null && loseObj.activeSelf != showRadiusSpheres)
        {
            loseObj.SetActive(showRadiusSpheres);
        }
    }

    // Δημιουργία ημιδιάφανου URP Material μέσω κώδικα
    private void SetTransparentMaterialURP(Renderer rend, Color color)
    {
        // Απενεργοποίηση λήψης σκιών από το Renderer
        rend.receiveShadows = false;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Χρήση Unlit shader για να μην επηρεάζεται από το φως/σκιές
        Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpShader == null)
        {
            rend.enabled = false;
            Debug.LogWarning("Universal Render Pipeline/Unlit shader is unavailable in this build; disabling the optional enemy radius visual.", this);
            return;
        }

        Material mat = new Material(urpShader);

        // Ρυθμίσεις Transparent Mode για URP Unlit
        mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        mat.SetFloat("_Blend", 0);   // 0 = Alpha
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Ορισμός χρώματος στο URP property (_BaseColor)
        mat.SetColor("_BaseColor", color);

        rend.material = mat;
    }
}