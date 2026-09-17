using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKnockback : MonoBehaviour
{
    private Coroutine activeKnockback;

    public void Push(Vector3 direction, float force, float duration)
    {
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
                StopCoroutine(activeKnockback);
            }

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

    private IEnumerator SmoothAgentKnockback(
        NavMeshAgent agent,
        Vector3 direction,
        float force,
        float duration)
    {
        bool wasStopped = agent.isStopped;
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
            agent.isStopped = wasStopped;
        }

        activeKnockback = null;
    }
}
