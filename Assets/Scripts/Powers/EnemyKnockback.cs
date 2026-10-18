using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKnockback : MonoBehaviour
{
    private Coroutine activeKnockback;
    private NavMeshAgent activeAgent;
    private bool agentWasStoppedBeforeKnockback;

    public void Push(Vector3 direction, float force, float duration)
    {
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI == null)
        {
            enemyAI = GetComponentInParent<EnemyAI>();
        }
        if (enemyAI == null)
        {
            enemyAI = GetComponentInChildren<EnemyAI>();
        }

        if (enemyAI != null && enemyAI.IsFrozen)
        {
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        direction.Normalize();
        force = Mathf.Max(0f, force);
        duration = Mathf.Max(0.01f, duration);

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = GetComponentInParent<NavMeshAgent>();
        }

        if (agent != null && agent.isOnNavMesh)
        {
            if (activeKnockback != null)
            {
                CancelActiveKnockback();
            }

            activeAgent = agent;
            agentWasStoppedBeforeKnockback = agent.isStopped;
            activeKnockback = StartCoroutine(SmoothAgentKnockback(agent, direction, force, duration));
            return;
        }

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = GetComponentInParent<Rigidbody>();
        }

        if (rigidbody != null && !rigidbody.isKinematic)
        {
            rigidbody.AddForce(direction * force, ForceMode.Impulse);
        }
    }

    public void CancelForFreeze()
    {
        CancelActiveKnockback();
    }

    private void CancelActiveKnockback()
    {
        if (activeKnockback != null)
        {
            StopCoroutine(activeKnockback);
            activeKnockback = null;
        }

        if (activeAgent != null && activeAgent.isOnNavMesh)
        {
            activeAgent.isStopped = agentWasStoppedBeforeKnockback;
            activeAgent.velocity = Vector3.zero;
        }

        activeAgent = null;
    }

    private IEnumerator SmoothAgentKnockback(
        NavMeshAgent agent,
        Vector3 direction,
        float force,
        float duration)
    {
        agent.isStopped = true;

        float elapsed = 0f;
        while (elapsed < duration && agent != null && agent.isOnNavMesh)
        {
            float strength = 1f - (elapsed / duration);
            agent.Move(direction * force * strength * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = agentWasStoppedBeforeKnockback;
        }

        activeKnockback = null;
        activeAgent = null;
    }
}
