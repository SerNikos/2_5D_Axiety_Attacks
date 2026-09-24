using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    private float knockbackForce;
    private GameObject owner;
    private Transform target;
    private Rigidbody projectileRigidbody;
    private float homingSpeed;
    private float homingTurnSpeed;
    private float knockbackDuration;
    private GameObject impactEffectPrefab;
    private bool affectGrassOnImpact;
    private float grassImpactRadius;
    private float grassImpactPushRate;
    private float grassImpactMaxStrength;
    private float grassImpactDuration;
    private bool hasHit;

    public void Configure(float force, GameObject fireballOwner)
    {
        Configure(force, fireballOwner, null, 0f, 0f, 0.25f);
    }

    public void Configure(
        float force,
        GameObject fireballOwner,
        Transform homingTarget,
        float speed,
        float turnSpeed,
        float duration)
    {
        Configure(force, fireballOwner, homingTarget, speed, turnSpeed, duration, null);
    }

    public void Configure(
        float force,
        GameObject fireballOwner,
        Transform homingTarget,
        float speed,
        float turnSpeed,
        float duration,
        GameObject fireballImpactEffectPrefab)
    {
        knockbackForce = Mathf.Max(0f, force);
        owner = fireballOwner;
        target = homingTarget;
        homingSpeed = Mathf.Max(0f, speed);
        homingTurnSpeed = Mathf.Max(0f, turnSpeed);
        knockbackDuration = Mathf.Max(0.01f, duration);
        impactEffectPrefab = fireballImpactEffectPrefab;
        projectileRigidbody = GetComponent<Rigidbody>();
        IgnoreOwnerCollisions();
    }

    public void ConfigureGrassImpact(
        bool affectGrass,
        float radius,
        float pushRate,
        float maxStrength,
        float duration)
    {
        affectGrassOnImpact = affectGrass;
        grassImpactRadius = Mathf.Max(0f, radius);
        grassImpactPushRate = Mathf.Max(0f, pushRate);
        grassImpactMaxStrength = Mathf.Max(0f, maxStrength);
        grassImpactDuration = Mathf.Max(0.01f, duration);
    }

    private void FixedUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy || projectileRigidbody == null)
        {
            return;
        }

        Vector3 targetDirection = target.position - transform.position;
        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        targetDirection.Normalize();

        Vector3 currentDirection = projectileRigidbody.velocity;
        currentDirection.y = 0f;
        if (currentDirection.sqrMagnitude <= 0.01f)
        {
            currentDirection = transform.forward;
            currentDirection.y = 0f;
        }

        if (currentDirection.sqrMagnitude <= 0.01f)
        {
            currentDirection = targetDirection;
        }
        else
        {
            currentDirection.Normalize();
        }

        float turnRadians = homingTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 newDirection = Vector3.RotateTowards(
            currentDirection,
            targetDirection,
            turnRadians,
            0f);

        projectileRigidbody.velocity = newDirection * homingSpeed;
        transform.rotation = Quaternion.LookRotation(newDirection);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 impactPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        HandleHit(collision.collider, impactPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other, other.ClosestPoint(transform.position));
    }

    private void HandleHit(Collider hitCollider, Vector3 impactPoint)
    {
        if (hasHit)
        {
            return;
        }

        Transform enemy = FindTaggedEnemy(hitCollider.transform);
        if (enemy == null)
        {
            return;
        }

        Vector3 direction = enemy.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        direction.Normalize();
        PushEnemy(enemy, direction);
        ApplyGrassImpact(impactPoint);
        SpawnImpactEffect(impactPoint);
        hasHit = true;
        Destroy(gameObject);
    }

    private void SpawnImpactEffect(Vector3 impactPoint)
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            return;
        }

        FireballExplosionEffect.Spawn(impactPoint);
    }

    private void ApplyGrassImpact(Vector3 impactPoint)
    {
        if (!affectGrassOnImpact || GrassManager.Instance == null)
        {
            return;
        }

        GrassManager.Instance.ApplyImpact(
            impactPoint,
            grassImpactRadius,
            grassImpactPushRate,
            grassImpactMaxStrength,
            grassImpactDuration);
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
            enemyRigidbody.AddForce(direction * knockbackForce, ForceMode.Impulse);
            return;
        }

        EnemyKnockback knockback = enemy.GetComponent<EnemyKnockback>();
        if (knockback == null)
        {
            knockback = enemy.gameObject.AddComponent<EnemyKnockback>();
        }

        knockback.Push(direction, knockbackForce, knockbackDuration);
    }

    private void IgnoreOwnerCollisions()
    {
        if (owner == null)
        {
            return;
        }

        Collider[] fireballColliders = GetComponentsInChildren<Collider>();
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();

        foreach (Collider fireballCollider in fireballColliders)
        {
            foreach (Collider ownerCollider in ownerColliders)
            {
                Physics.IgnoreCollision(fireballCollider, ownerCollider);
            }
        }
    }
}
