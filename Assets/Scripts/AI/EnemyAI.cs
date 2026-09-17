using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3.5f;

    [Header("Detection Settings")]
    public float aggroRadius = 5f;        // Ακτίνα για να αρχίσει το κυνήγι
    public float losePlayerRadius = 12f;  // Ακτίνα για να χάσει τον παίκτη

    [Header("Patrol Settings")]
    public float patrolRange = 8f;        // Ακτίνα γύρω από το αρχικό σημείο για την περιπολία

    private float distanceToPlayer;
    private Vector3 originPos;            // Το κέντρο της περιοχής περιπολίας
    private NavMeshAgent agent;

    public enum EnemyState
    {
        Patrol,
        Chase
    }

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Patrol;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        originPos = transform.position;

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        currentState = EnemyState.Patrol;

        // Δημιουργία των 2 ημιδιάφανων σφαιρών για το URP στο Runtime
        CreateRuntimeSpheres();
    }

    private void Start()
    {
        SetRandomPatrolDestination();
    }

    private void Update()
    {
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

    private void UpdatePatrol()
    {
        // 1. Αν ο παίκτης πλησιάσει, άλλαξε state σε Chase
        if (distanceToPlayer <= aggroRadius)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // 2. Περιπολία: Μόλις φτάσει στο σημείο, διάλεξε νέο τυχαίο σημείο στο NavMesh
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetRandomPatrolDestination();
        }
    }

    private void UpdateChase()
    {
        agent.SetDestination(player.position);

        // Αν ο παίκτης φύγει πολύ μακριά, γύρνα αμέσως σε Patrol
        if (distanceToPlayer >= losePlayerRadius)
        {
            currentState = EnemyState.Patrol;
            SetRandomPatrolDestination();
        }
    }

    private void SetRandomPatrolDestination()
    {
        Vector3 randomPoint = originPos + Random.insideUnitSphere * patrolRange;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // Δημιουργεί 2 ημιδιάφανες σφαίρες ως children του εχθρού για το Runtime
    private void CreateRuntimeSpheres()
    {
        // 1. Σφαίρα Aggro Radius (Κίτρινη)
        GameObject aggroObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aggroObj.name = "Runtime_AggroRadius";
        aggroObj.transform.SetParent(transform);
        aggroObj.transform.localPosition = Vector3.zero;
        aggroObj.transform.localScale = Vector3.one * (aggroRadius * 2f);

        Destroy(aggroObj.GetComponent<Collider>()); // Αφαίρεση collider για να μην επηρεάζει τα physics

        Renderer aggroRend = aggroObj.GetComponent<Renderer>();
        SetTransparentMaterialURP(aggroRend, new Color(1f, 0.92f, 0.016f, 0.25f)); // Ημιδιάφανο κίτρινο

        // 2. Σφαίρα Lose Player Radius (Κόκκινη)
        GameObject loseObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        loseObj.name = "Runtime_LoseRadius";
        loseObj.transform.SetParent(transform);
        loseObj.transform.localPosition = Vector3.zero;
        loseObj.transform.localScale = Vector3.one * (losePlayerRadius * 2f);

        Destroy(loseObj.GetComponent<Collider>());

        Renderer loseRend = loseObj.GetComponent<Renderer>();
        SetTransparentMaterialURP(loseRend, new Color(1f, 0f, 0f, 0.15f)); // Ημιδιάφανο κόκκινο
    }

    // Δημιουργία ημιδιάφανου URP Material μέσω κώδικα
    private void SetTransparentMaterialURP(Renderer rend, Color color)
    {
        // Απενεργοποίηση λήψης σκιών από το Renderer
        rend.receiveShadows = false;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Χρήση Unlit shader για να μην επηρεάζεται από το φως/σκιές
        Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");

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