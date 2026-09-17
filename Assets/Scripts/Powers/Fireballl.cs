using UnityEngine;

public class Fireballl : MonoBehaviour
{
    [Header("Fireball")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float spawnDistance = 1.2f;
    [SerializeField] private float releaseSpeed = 12f;
    [SerializeField] private float maxLifetime = 5f;

    [Header("Material")]
    [SerializeField] private bool useFireballShader = true;
    [SerializeField] private Color shaderCoreColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color shaderFlameColor = new Color(1f, 0.08f, 0.005f);
    [SerializeField] private Color shaderRimColor = new Color(1f, 0.3f, 0.02f);
    [SerializeField] private float shaderEmission = 3f;
    [SerializeField] private float shaderNoiseScale = 6f;
    [SerializeField] private float shaderScrollSpeed = 2f;

    [Header("Enemy Knockback")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackDuration = 0.25f;

    [Header("Homing")]
    [SerializeField] private float homingTurnSpeed = 720f;
    [SerializeField] private float homingRadius = 10f;
    [SerializeField, Range(-1f, 1f)] private float minimumTargetDot = 0f;

    [Header("Facing")]
    [SerializeField] private float facingTurnSpeed = 540f;

    [Header("Charging")]
    [SerializeField, Range(0.01f, 1f)] private float startingScale = 0.15f;
    [SerializeField] private float chargeDuration = 0.35f;
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField, Range(0f, 0.25f)] private float pulseAmount = 0.08f;

    private GameObject chargingFireball;
    private Rigidbody chargingRigidbody;
    private Vector3 originalScale;
    private Vector3 releaseDirection;
    private Material[] fireballMaterials;
    private float chargeElapsed;
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            CreateFireball();
        }

        if (chargingFireball == null)
        {
            return;
        }

        UpdateChargingFireball();

        if (Input.GetMouseButtonDown(0))
        {
            ReleaseFireball();
        }
    }

    private void CreateFireball()
    {
        if (chargingFireball != null)
        {
            return;
        }

        if (fireballPrefab == null)
        {
            Debug.LogWarning("Fireball prefab is not assigned on Fireballl.");
            return;
        }

        releaseDirection = GetFacingDirection();
        chargingFireball = Instantiate(
            fireballPrefab,
            transform.position + releaseDirection * spawnDistance,
            Quaternion.LookRotation(releaseDirection));

        originalScale = chargingFireball.transform.localScale;
        chargingFireball.transform.localScale = originalScale * startingScale;
        chargeElapsed = 0f;
        ApplyFireballShader();

        chargingRigidbody = chargingFireball.GetComponent<Rigidbody>();
        if (chargingRigidbody == null)
        {
            chargingRigidbody = chargingFireball.AddComponent<Rigidbody>();
        }

        chargingRigidbody.isKinematic = true;
        chargingRigidbody.detectCollisions = false;
    }

    private void UpdateChargingFireball()
    {
        Vector3 desiredDirection = GetFacingDirection();
        float turnRadians = facingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
        releaseDirection = Vector3.RotateTowards(
            releaseDirection,
            desiredDirection,
            turnRadians,
            0f).normalized;

        chargingFireball.transform.position = transform.position + releaseDirection * spawnDistance;
        chargingFireball.transform.rotation = Quaternion.LookRotation(releaseDirection);

        chargeElapsed += Time.deltaTime;
        float chargeProgress = chargeDuration <= 0f ? 1f : Mathf.Clamp01(chargeElapsed / chargeDuration);
        float scale = Mathf.SmoothStep(startingScale, 1f, chargeProgress);
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        chargingFireball.transform.localScale = originalScale * scale * pulse;
        UpdateShaderEmission(pulse);
    }

    private void ReleaseFireball()
    {
        FireballProjectile projectile = chargingFireball.GetComponent<FireballProjectile>();
        if (projectile == null)
        {
            projectile = chargingFireball.AddComponent<FireballProjectile>();
        }

        Transform target = FindClosestEnemy(releaseDirection);
        projectile.Configure(
            knockbackForce,
            gameObject,
            target,
            releaseSpeed,
            homingTurnSpeed,
            knockbackDuration);

        chargingFireball.transform.localScale = originalScale;
        UpdateShaderEmission(1f);
        chargingRigidbody.isKinematic = false;
        chargingRigidbody.detectCollisions = true;
        chargingRigidbody.velocity = releaseDirection * releaseSpeed;

        Destroy(chargingFireball, maxLifetime);
        chargingFireball = null;
        chargingRigidbody = null;
        fireballMaterials = null;
    }

    private void ApplyFireballShader()
    {
        if (!useFireballShader)
        {
            fireballMaterials = null;
            return;
        }

        Shader fireballShader = Shader.Find("Custom/Fireball");
        if (fireballShader == null)
        {
            Debug.LogWarning("Custom/Fireball shader could not be found.");
            return;
        }

        Renderer[] renderers = chargingFireball.GetComponentsInChildren<Renderer>();
        fireballMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
            material.shader = fireballShader;
            material.SetColor("_CoreColor", shaderCoreColor);
            material.SetColor("_FlameColor", shaderFlameColor);
            material.SetColor("_RimColor", shaderRimColor);
            material.SetFloat("_Emission", shaderEmission);
            material.SetFloat("_NoiseScale", shaderNoiseScale);
            material.SetFloat("_ScrollSpeed", shaderScrollSpeed);
            fireballMaterials[i] = material;
        }
    }

    private void UpdateShaderEmission(float pulse)
    {
        if (fireballMaterials == null)
        {
            return;
        }

        foreach (Material material in fireballMaterials)
        {
            if (material != null)
            {
                material.SetFloat("_Emission", shaderEmission * pulse);
            }
        }
    }

    private Vector3 GetFacingDirection()
    {
        if (playerController != null && playerController.FacingDirection.sqrMagnitude > 0.01f)
        {
            return playerController.FacingDirection.normalized;
        }

        return transform.forward.sqrMagnitude > 0.01f ? transform.forward.normalized : Vector3.forward;
    }

    private Transform FindClosestEnemy(Vector3 firingDirection)
    {
        if (homingRadius <= 0f)
        {
            return null;
        }

        firingDirection.y = 0f;
        if (firingDirection.sqrMagnitude <= 0.01f)
        {
            return null;
        }

        firingDirection.Normalize();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closestEnemy = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            float distanceSqr = toEnemy.sqrMagnitude;

            if (distanceSqr > homingRadius * homingRadius || distanceSqr <= 0.01f)
            {
                continue;
            }

            float targetDot = Vector3.Dot(firingDirection, toEnemy.normalized);
            if (targetDot < minimumTargetDot)
            {
                continue;
            }

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }
}
