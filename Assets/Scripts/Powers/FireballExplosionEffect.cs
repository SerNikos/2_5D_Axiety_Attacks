using UnityEngine;

public class FireballExplosionEffect : MonoBehaviour
{
    private const float Duration = 0.55f; // Πολύ πιο γρήγορη συνολική διάρκεια
    private const float StartGlowScale = 0.35f;
    private const float EndGlowScale = 2f;
    private const float GlowDeclineRate = 2.0f; // Πολύ πιο γρήγορη πτώση emission
    private const float ParticleBurstRate = 110;

    // Χρόνος (σε δευτερόλεπτα) που ξεκινάει το dissolve της σφαίρας
    private const float DissolveStartTime = 0.12f;

    private ParticleSystem mainParticleSystem;
    private ParticleSystem secondaryParticleSystem;
    private ParticleSystemRenderer mainParticleRenderer;
    private ParticleSystemRenderer secondaryParticleRenderer;
    private Transform glowTransform;
    private Renderer glowRenderer;
    private Material mainParticleMaterial;
    private Material secondaryParticleMaterial;
    private Material glowMaterial;
    private float elapsed;

    public static void Spawn(Vector3 position)
    {
        GameObject explosion = new GameObject("Fireball Explosion");
        explosion.transform.position = position;
        explosion.AddComponent<FireballExplosionEffect>();
    }

    private void Awake()
    {
        CreateMainParticles();
        CreateSecondaryParticles();
        CreateGlow();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Duration);

        // Επιθετική καμπύλη ακαριαίας επέκτασης (Exponential Out)
        float easedProgress = 1f - Mathf.Pow(1f - progress, 5f);

        // 1. Ακαριαίο Scaling
        if (glowTransform != null)
        {
            float glowScale = Mathf.Lerp(StartGlowScale, EndGlowScale, easedProgress);
            glowTransform.localScale = Vector3.one * glowScale;
        }

        // 2. Ταχύτατο Dissolve / Fade out
        if (glowMaterial != null)
        {
            // Ακαριαίο ξέσπασμα και πολύ γρήγορη πτώση φωτεινότητας
            float emissionValue = 24f * Mathf.Exp(-GlowDeclineRate * elapsed) * (1f - Mathf.Exp(-elapsed * 30f));

            if (glowMaterial.HasProperty("_Emission"))
            {
                glowMaterial.SetFloat("_Emission", emissionValue);
            }

            // Γρήγορο Alpha Fade που ξεκινάει ακριβώς στο DissolveStartTime
            float fadeProgress = Mathf.Clamp01((elapsed - DissolveStartTime) / (Duration - DissolveStartTime));
            float alpha = Mathf.SmoothStep(0.95f, 0f, fadeProgress);

            if (glowMaterial.HasProperty("_Alpha"))
            {
                glowMaterial.SetFloat("_Alpha", alpha);
            }

            if (glowMaterial.HasProperty("_BaseColor"))
            {
                Color baseCol = glowMaterial.GetColor("_BaseColor");
                baseCol.a = alpha;
                glowMaterial.SetColor("_BaseColor", baseCol);
            }
            else if (glowMaterial.HasProperty("_Color"))
            {
                Color col = glowMaterial.GetColor("_Color");
                col.a = alpha;
                glowMaterial.SetColor("_Color", col);
            }
        }

        if (elapsed >= Duration)
        {
            Destroy(gameObject);
        }
    }

    private void CreateMainParticles()
    {
        GameObject particleObject = new GameObject("Fireball Main Particles");
        particleObject.transform.SetParent(transform, false);
        mainParticleSystem = particleObject.AddComponent<ParticleSystem>();
        mainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        mainParticleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = mainParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = Duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.32f); // Σύντομος χρόνος ζωής
        main.startSpeed = new ParticleSystem.MinMaxCurve(10.0f, 18.0f);    // Εκτόξευση υψηλής ταχύτητας
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.5f, 0.05f, 1f),
            new Color(1f, 1f, 0.6f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = mainParticleSystem.emission;
        emission.enabled = true;
        // Το Burst ενεργοποιείται ΑΚΡΙΒΩΣ όταν ξεκινάει το Dissolve (DissolveStartTime)
        emission.SetBursts(new[] { new ParticleSystem.Burst(DissolveStartTime, ParticleBurstRate) });

        ParticleSystem.ShapeModule shape = mainParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f; // Μεγαλύτερη ακτίνα για να φαίνεται ότι βγαίνουν από την επιφάνεια της σφαίρας

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = mainParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.05f), 0.2f),
                new GradientColorKey(new Color(1f, 0.1f, 0.0f), 0.6f),
                new GradientColorKey(new Color(0.1f, 0.01f, 0.0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.3f),
                new GradientAlphaKey(0f, 1f) // Dissolve σωματιδίων
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = mainParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.1f, 1.2f),
                new Keyframe(1f, 0f)));

        mainParticleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        mainParticleRenderer.material = CreateMainParticleMaterial();
        mainParticleSystem.Play();
    }

    private void CreateSecondaryParticles()
    {
        GameObject particleObject = new GameObject("Fireball Secondary Particles");
        particleObject.transform.SetParent(transform, false);
        secondaryParticleSystem = particleObject.AddComponent<ParticleSystem>();
        secondaryParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        secondaryParticleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = secondaryParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = Duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8.0f, 14.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
        main.startColor = new Color(1f, 0.95f, 0.6f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = secondaryParticleSystem.emission;
        emission.enabled = true;
        // Συγχρονισμός και των δευτερευόντων σπινθήρων με το DissolveStartTime
        emission.SetBursts(new[] { new ParticleSystem.Burst(DissolveStartTime, 30) });

        ParticleSystem.ShapeModule shape = secondaryParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.45f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = secondaryParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0.4f),
                new GradientColorKey(new Color(0.8f, 0.2f, 0.0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

        secondaryParticleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        secondaryParticleRenderer.material = CreateSecondaryParticleMaterial();
        secondaryParticleSystem.Play();
    }

    private void CreateGlow()
    {
        GameObject glowObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowObject.name = "Fireball Explosion Glow";
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localScale = Vector3.one * StartGlowScale;

        Collider glowCollider = glowObject.GetComponent<Collider>();
        if (glowCollider != null)
        {
            Destroy(glowCollider);
        }

        glowTransform = glowObject.transform;
        glowRenderer = glowObject.GetComponent<Renderer>();
        glowMaterial = CreateGlowMaterial();

        if (glowRenderer != null && glowMaterial != null)
        {
            glowRenderer.sharedMaterial = glowMaterial;
        }
    }

    private Material CreateMainParticleMaterial()
    {
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Particles/Standard Unlit");
        }

        if (particleShader == null)
        {
            particleShader = Shader.Find("Sprites/Default");
        }

        if (particleShader == null)
        {
            return null;
        }

        mainParticleMaterial = new Material(particleShader)
        {
            name = "Fireball Explosion Particles (Main)"
        };

        if (mainParticleMaterial.HasProperty("_BaseColor"))
        {
            mainParticleMaterial.SetColor("_BaseColor", Color.white);
        }

        if (mainParticleMaterial.HasProperty("_Color"))
        {
            mainParticleMaterial.SetColor("_Color", Color.white);
        }

        return mainParticleMaterial;
    }

    private Material CreateSecondaryParticleMaterial()
    {
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Particles/Standard Unlit");
        }

        if (particleShader == null)
        {
            particleShader = Shader.Find("Sprites/Default");
        }

        if (particleShader == null)
        {
            return null;
        }

        secondaryParticleMaterial = new Material(particleShader)
        {
            name = "Fireball Explosion Particles (Secondary)"
        };

        if (secondaryParticleMaterial.HasProperty("_BaseColor"))
        {
            secondaryParticleMaterial.SetColor("_BaseColor", Color.white);
        }

        if (secondaryParticleMaterial.HasProperty("_Color"))
        {
            secondaryParticleMaterial.SetColor("_Color", Color.white);
        }

        return secondaryParticleMaterial;
    }

    private Material CreateGlowMaterial()
    {
        Shader fireballShader = Shader.Find("Custom/Fireball");
        if (fireballShader == null)
        {
            fireballShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (fireballShader == null)
        {
            return null;
        }

        Material material = new Material(fireballShader)
        {
            name = "Fireball Explosion Glow"
        };

        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 0);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (material.HasProperty("_CoreColor"))
        {
            material.SetColor("_CoreColor", new Color(1f, 1f, 0.4f, 1f));
        }

        if (material.HasProperty("_FlameColor"))
        {
            material.SetColor("_FlameColor", new Color(1f, 0.4f, 0.01f, 1f));
        }

        if (material.HasProperty("_RimColor"))
        {
            material.SetColor("_RimColor", new Color(1f, 0.1f, 0.0f, 1f));
        }

        if (material.HasProperty("_Emission"))
        {
            material.SetFloat("_Emission", 24f);
        }

        if (material.HasProperty("_Alpha"))
        {
            material.SetFloat("_Alpha", 0.95f);
        }

        return material;
    }

    private void OnDestroy()
    {
        if (mainParticleMaterial != null)
        {
            Destroy(mainParticleMaterial);
        }

        if (secondaryParticleMaterial != null)
        {
            Destroy(secondaryParticleMaterial);
        }

        if (glowMaterial != null)
        {
            Destroy(glowMaterial);
        }
    }
}