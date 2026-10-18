using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VineRootPower : MonoBehaviour
{
    [Header("Vine Root")]
    // How long enemies stay frozen and how long the temporary vine visual remains visible.
    [SerializeField, Min(0.1f)] private float freezeDuration = 3f;
    // Dimensions of the box in front of the player that can catch enemies.
    [SerializeField, Min(0.1f)] private float areaLength = 3.5f;
    [SerializeField, Min(0.1f)] private float areaWidth = 2f;
    [SerializeField, Min(0.1f)] private float areaHeight = 2.5f;
    // Time for the segmented tendrils to extend from the player toward their target.
    [SerializeField, Min(0f)] private float sproutDuration = 1f;
    // Small gap between the enemy's collider and the wrapping vines.
    [SerializeField, Min(0f)] private float wrapClearance = 0.035f;
    // Number of turns the tendrils make while spiraling up around the enemy.
    [SerializeField, Range(1f, 4f)] private float wrapTurns = 2.5f;
    // Each tendril chooses a base diameter from this range when the power activates.
    [SerializeField, Min(0.01f)] private float minimumVineDiameter = 0.18f;
    [SerializeField, Min(0.01f)] private float maximumVineDiameter = 0.3f;
    // Reduce the diameter toward the tip so each tendril tapers naturally.
    [SerializeField, Range(0.1f, 0.8f)] private float tipDiameterScale = 0.3f;
    // Adds subtle smooth thickness changes along a tendril, without sharp bumps.
    [SerializeField, Range(0f, 0.35f)] private float diameterVariation = 0.12f;
    // Number of sides around the tube; more sides make the vine look smoother.
    [SerializeField, Range(5, 12)] private int tubeSides = 8;

    [Header("Anxiety Availability")]
    // Optional reference; when assigned, the power is blocked if spells are unavailable.
    [SerializeField] private AnxietyBar anxietyBar;

    private PlayerController playerController;

    private void Awake()
    {
        // Read the player's movement/facing component from this same GameObject.
        playerController = GetComponent<PlayerController>();

        // Find the bar automatically when it was not assigned in the Inspector.
        if (anxietyBar == null)
        {
            anxietyBar = FindObjectOfType<AnxietyBar>();
        }
    }

    private void Update()
    {
        // Support both the number-row 3 and numeric-keypad 3.
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            // Do not activate spells while the anxiety system says they are unavailable.
            if (anxietyBar != null && !anxietyBar.SpellsAvailable)
            {
                return;
            }

            ActivateVines();
        }
    }

    private void ActivateVines()
    {
        // Use a flat, horizontal facing direction for the forward attack area.
        Vector3 direction = GetFacingDirection();
        // Find the sideways direction so the attack box and vines can spread left/right.
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

        // Put the box halfway down its length so it starts near the player and extends forward.
        Vector3 center = transform.position + direction * (areaLength * 0.5f);
        center.y += areaHeight * 0.5f;

        // OverlapBox takes half-sizes, so each Inspector dimension is divided by two.
        // Rotating the box makes its long axis follow the direction the player is facing.
        Collider[] colliders = Physics.OverlapBox(
            center,
            new Vector3(areaWidth * 0.5f, areaHeight * 0.5f, areaLength * 0.5f),
            Quaternion.LookRotation(direction));

        // One enemy can have several colliders; the set prevents freezing it repeatedly.
        HashSet<EnemyAI> uniqueEnemies = new HashSet<EnemyAI>();
        List<EnemyAI> affectedEnemies = new List<EnemyAI>();
        foreach (Collider hitCollider in colliders)
        {
            // EnemyAI may be on the collider's parent or on one of its children.
            EnemyAI enemy = hitCollider.GetComponentInParent<EnemyAI>();
            if (enemy == null)
            {
                enemy = hitCollider.GetComponentInChildren<EnemyAI>();
            }

            if (enemy != null && uniqueEnemies.Add(enemy))
            {
                // EnemyAI pauses its NavMeshAgent and restores its prior stopped state later.
                enemy.FreezeFor(freezeDuration);
                affectedEnemies.Add(enemy);
            }
        }

        // Aim the visual at caught enemies, or show a forward-reaching effect if none were caught.
        CreateVineVisual(direction, right, affectedEnemies);
    }

    private void CreateVineVisual(Vector3 direction, Vector3 right, List<EnemyAI> affectedEnemies)
    {
        // This parent object lets Unity remove all generated vine pieces together.
        GameObject visual = new GameObject("Vine Root Effect");
        visual.transform.position = transform.position;

        // Use the project's URP shader when available, with Standard as a fallback.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material vineMaterial = shader != null ? new Material(shader) : null;
        if (vineMaterial != null)
        {
            // Set whichever color property is supported by the selected render pipeline.
            Color vineColor = new Color(0.12f, 0.72f, 0.12f);
            if (vineMaterial.HasProperty("_BaseColor"))
            {
                vineMaterial.SetColor("_BaseColor", vineColor);
            }

            if (vineMaterial.HasProperty("_Color"))
            {
                vineMaterial.SetColor("_Color", vineColor);
            }
        }

        if (affectedEnemies.Count == 0)
        {
            // Keep the spell visible when it misses by curling the vines at the reach limit.
            Vector3 fallbackTarget = transform.position + direction * areaLength;
            Bounds fallbackBounds = new Bounds(
                fallbackTarget + Vector3.up * (areaHeight * 0.5f),
                new Vector3(0.45f, areaHeight, 0.45f));
            CreateVineBundle(visual.transform, transform.position, fallbackBounds, null, right, vineMaterial, 0);
        }
        else
        {
            // Send one bundle from the player to each enemy that was frozen by the hitbox.
            for (int targetIndex = 0; targetIndex < affectedEnemies.Count; targetIndex++)
            {
                Bounds targetBounds = GetEnemyBounds(affectedEnemies[targetIndex]);
                CreateVineBundle(
                    visual.transform,
                    transform.position,
                    targetBounds,
                    affectedEnemies[targetIndex].transform,
                    right,
                    vineMaterial,
                    targetIndex);
            }
        }

        // Clean up the visual and its material after the freeze effect ends.
        Destroy(visual, freezeDuration);
        if (vineMaterial != null)
        {
            Destroy(vineMaterial, freezeDuration);
        }
    }

    private Bounds GetEnemyBounds(EnemyAI enemy)
    {
        // Use a modest default around the enemy pivot if it has no usable collider.
        Bounds bounds = new Bounds(
            enemy.transform.position + Vector3.up * 0.8f,
            new Vector3(0.8f, 1.6f, 0.8f));
        bool foundCollider = false;

        Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
        foreach (Collider enemyCollider in enemyColliders)
        {
            if (!enemyCollider.enabled || !enemyCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!foundCollider)
            {
                bounds = enemyCollider.bounds;
                foundCollider = true;
            }
            else
            {
                bounds.Encapsulate(enemyCollider.bounds);
            }
        }

        return bounds;
    }

    private void CreateVineBundle(
        Transform parent,
        Vector3 playerPosition,
        Bounds targetBounds,
        Transform targetTransform,
        Vector3 playerRight,
        Material material,
        int targetIndex)
    {
        const int tendrilCount = 3;
        const int groundSegmentCount = 12;
        const int wrapSegmentCount = 36;
        Vector3 targetPosition = targetBounds.center;
        float playerGroundHeight = GetGroundHeight(playerPosition, null, playerPosition.y);
        float targetGroundHeight = GetGroundHeight(targetPosition, targetTransform, playerGroundHeight);
        Vector3 targetBase = new Vector3(targetPosition.x, targetGroundHeight, targetPosition.z);
        float wrapHeight = Mathf.Max(0.5f, targetBounds.size.y);
        float enemyRadius = Mathf.Max(targetBounds.extents.x, targetBounds.extents.z);

        // Route along the ground to the enemy's feet, then use its bounds to form the wrap.
        Vector3 routeDirection = targetBase - playerPosition;
        routeDirection.y = 0f;
        if (routeDirection.sqrMagnitude <= 0.001f)
        {
            routeDirection = GetFacingDirection();
        }
        routeDirection.Normalize();

        // This sideways axis is perpendicular to the route and is used to loop around the target.
        Vector3 routeSide = Vector3.Cross(Vector3.up, routeDirection).normalized;
        float growDuration = Mathf.Min(sproutDuration, freezeDuration);
        float minDiameter = Mathf.Min(minimumVineDiameter, maximumVineDiameter);
        float maxDiameter = Mathf.Max(minimumVineDiameter, maximumVineDiameter);

        for (int tendril = 0; tendril < tendrilCount; tendril++)
        {
            float lane = tendril - (tendrilCount - 1) * 0.5f;
            // Follow the enemy's footprint closely, with only a small gap between tendrils.
            float radius = Mathf.Max(0.06f, enemyRadius + wrapClearance + tendril * 0.015f);
            float rootDiameter = Random.Range(minDiameter, maxDiameter);
            float diameterSeed = Random.Range(0f, 1000f);
            float groundLift = rootDiameter * 0.5f + 0.01f;
            Vector3 start = playerPosition
                + routeDirection * 0.25f
                + playerRight * (lane * areaWidth * 0.12f);
            start.y = playerGroundHeight + groundLift;

            // End the crawling section where the wrapping loop begins around the enemy.
            Vector3 wrapStart = targetBase + routeSide * radius + Vector3.up * groundLift;
            List<Vector3> points = new List<Vector3>(groundSegmentCount + wrapSegmentCount + 1)
            {
                start
            };

            // Create a gently winding ground path from the player to the target.
            for (int segment = 1; segment <= groundSegmentCount; segment++)
            {
                float progress = (float)segment / groundSegmentCount;
                float sideWiggle = Mathf.Sin(progress * Mathf.PI * 3f + tendril + targetIndex) * 0.18f;
                Vector3 point = Vector3.Lerp(start, wrapStart, progress)
                    + routeSide * (sideWiggle * Mathf.Sin(progress * Mathf.PI));
                point.y = Mathf.Lerp(start.y, wrapStart.y, progress);
                points.Add(point);
            }

            // Continue from the ground path into an upward spiral that circles the enemy.
            for (int segment = 1; segment <= wrapSegmentCount; segment++)
            {
                float progress = (float)segment / wrapSegmentCount;
                float angle = progress * Mathf.PI * 2f * wrapTurns;
                Vector3 point = targetBase
                    + routeSide * (Mathf.Cos(angle) * radius)
                    + routeDirection * (Mathf.Sin(angle) * radius)
                    + Vector3.up * (0.04f + wrapHeight * progress);
                points.Add(point);
            }

            // Build one continuous tube and reveal it progressively along the complete path.
            StartCoroutine(GrowVineTendril(
                parent,
                points,
                material,
                growDuration,
                rootDiameter,
                diameterSeed,
                minDiameter,
                maxDiameter,
                groundSegmentCount));
        }
    }

    private float GetGroundHeight(Vector3 position, Transform targetToIgnore, float fallbackHeight)
    {
        const float probeHeight = 4f;
        const float probeDepth = 12f;
        Vector3 rayOrigin = position + Vector3.up * probeHeight;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            probeHeight + probeDepth,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearestHitDistance = float.MaxValue;
        float groundHeight = fallbackHeight;
        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;
            if (IsPartOfHierarchy(hitTransform, transform) ||
                (targetToIgnore != null && IsPartOfHierarchy(hitTransform, targetToIgnore)))
            {
                continue;
            }

            if (hit.distance < nearestHitDistance)
            {
                nearestHitDistance = hit.distance;
                groundHeight = hit.point.y;
            }
        }

        return groundHeight;
    }

    private bool IsPartOfHierarchy(Transform candidate, Transform root)
    {
        return candidate == root || candidate.IsChildOf(root);
    }

    private IEnumerator GrowVineTendril(
        Transform parent,
        List<Vector3> centerline,
        Material material,
        float growDuration,
        float rootDiameter,
        float diameterSeed,
        float minDiameter,
        float maxDiameter,
        int groundSegmentCount)
    {
        // A single mesh removes the visible joins between the old cylinder segments.
        GameObject vine = new GameObject("Continuous Vine Tendril");
        vine.transform.SetParent(parent, false);

        Mesh mesh = new Mesh { name = "Continuous Vine Mesh" };
        mesh.MarkDynamic();
        vine.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = vine.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.receiveShadows = false;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Dispose of the generated mesh along with the temporary visual effect.
        Destroy(mesh, freezeDuration);

        List<Vector3> visiblePath = new List<Vector3>(centerline.Count);
        List<Vector3> vertices = new List<Vector3>(centerline.Count * tubeSides + 2);
        List<int> triangles = new List<int>(centerline.Count * tubeSides * 6);
        float elapsed = 0f;

        // Grow from the player to the target by rebuilding only the portion already reached.
        while (vine != null && mesh != null)
        {
            float rawProgress = growDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / growDuration);
            float progress = Mathf.SmoothStep(0f, 1f, rawProgress);
            UpdateVineMesh(
                mesh,
                centerline,
                parent.position,
                progress,
                rootDiameter,
                diameterSeed,
                minDiameter,
                maxDiameter,
                groundSegmentCount,
                visiblePath,
                vertices,
                triangles);

            if (rawProgress >= 1f)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void UpdateVineMesh(
        Mesh mesh,
        List<Vector3> centerline,
        Vector3 worldOrigin,
        float progress,
        float rootDiameter,
        float diameterSeed,
        float minDiameter,
        float maxDiameter,
        int groundSegmentCount,
        List<Vector3> visiblePath,
        List<Vector3> vertices,
        List<int> triangles)
    {
        mesh.Clear();
        visiblePath.Clear();
        vertices.Clear();
        triangles.Clear();

        if (progress <= 0f || centerline.Count < 2)
        {
            return;
        }

        // Find the moving tip along the path and include the partial segment at that position.
        float pathPosition = progress * (centerline.Count - 1);
        int lastSegment = Mathf.Min(Mathf.FloorToInt(pathPosition), centerline.Count - 2);
        float segmentProgress = pathPosition - lastSegment;

        for (int pointIndex = 0; pointIndex <= lastSegment; pointIndex++)
        {
            visiblePath.Add(centerline[pointIndex]);
        }
        visiblePath.Add(Vector3.Lerp(
            centerline[lastSegment],
            centerline[lastSegment + 1],
            segmentProgress));

        // A zero-length first frame has no direction to form a tube around yet.
        if (visiblePath.Count < 2 ||
            (visiblePath[visiblePath.Count - 1] - visiblePath[visiblePath.Count - 2]).sqrMagnitude <= 0.000001f)
        {
            return;
        }

        // Make a round cross-section at each path point; adjacent rings share a smooth surface.
        for (int ring = 0; ring < visiblePath.Count; ring++)
        {
            Vector3 tangent;
            if (ring == 0)
            {
                tangent = visiblePath[1] - visiblePath[0];
            }
            else if (ring == visiblePath.Count - 1)
            {
                tangent = visiblePath[ring] - visiblePath[ring - 1];
            }
            else
            {
                tangent = visiblePath[ring + 1] - visiblePath[ring - 1];
            }

            tangent.Normalize();
            Vector3 reference = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.95f
                ? Vector3.right
                : Vector3.up;
            Vector3 axisA = Vector3.Cross(tangent, reference).normalized;
            Vector3 axisB = Vector3.Cross(tangent, axisA).normalized;

            float ringPathPosition = ring == visiblePath.Count - 1 ? pathPosition : ring;
            float ringProgress = ringPathPosition / (centerline.Count - 1);
            float noise = Mathf.PerlinNoise(diameterSeed, ringProgress * 8f);
            float variation = 1f + (noise * 2f - 1f) * diameterVariation;
            float localDiameter = Mathf.Clamp(rootDiameter * variation, minDiameter, maxDiameter);
            float taperedDiameter = Mathf.Lerp(localDiameter, localDiameter * tipDiameterScale, ringProgress);
            float radius = taperedDiameter * 0.5f;
            Vector3 center = visiblePath[ring] - worldOrigin;

            // Keep the lower surface of the crawling section resting on the sampled ground.
            if (ringPathPosition <= groundSegmentCount)
            {
                float startGround = centerline[0].y - rootDiameter * 0.5f - 0.01f;
                float targetGround = centerline[groundSegmentCount].y - rootDiameter * 0.5f - 0.01f;
                float groundProgress = ringPathPosition / groundSegmentCount;
                float groundHeight = Mathf.Lerp(startGround, targetGround, groundProgress);
                center.y = groundHeight + radius + 0.005f - worldOrigin.y;
            }

            for (int side = 0; side < tubeSides; side++)
            {
                float angle = side * Mathf.PI * 2f / tubeSides;
                Vector3 radial = axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle);
                vertices.Add(center + radial * radius);
            }
        }

        // Connect each ring to the next to create one continuous tapered vine mesh.
        for (int ring = 0; ring < visiblePath.Count - 1; ring++)
        {
            for (int side = 0; side < tubeSides; side++)
            {
                int nextSide = (side + 1) % tubeSides;
                int current = ring * tubeSides + side;
                int currentNext = ring * tubeSides + nextSide;
                int next = (ring + 1) * tubeSides + side;
                int nextNext = (ring + 1) * tubeSides + nextSide;

                triangles.Add(current);
                triangles.Add(currentNext);
                triangles.Add(next);
                triangles.Add(currentNext);
                triangles.Add(nextNext);
                triangles.Add(next);
            }
        }

        // Cap both ends so the tendril looks solid instead of like a hollow pipe.
        int startCap = vertices.Count;
        vertices.Add(visiblePath[0] - worldOrigin);
        int endCap = vertices.Count;
        vertices.Add(visiblePath[visiblePath.Count - 1] - worldOrigin);
        int lastRing = (visiblePath.Count - 1) * tubeSides;

        for (int side = 0; side < tubeSides; side++)
        {
            int nextSide = (side + 1) % tubeSides;
            triangles.Add(startCap);
            triangles.Add(nextSide);
            triangles.Add(side);
            triangles.Add(endCap);
            triangles.Add(lastRing + side);
            triangles.Add(lastRing + nextSide);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 GetFacingDirection()
    {
        // Prefer the direction last set by the player's movement controls.
        if (playerController != null && playerController.FacingDirection.sqrMagnitude > 0.01f)
        {
            Vector3 direction = playerController.FacingDirection;
            direction.y = 0f;
            return direction.normalized;
        }

        // If there is no PlayerController or facing direction, use the object's forward axis.
        Vector3 fallback = transform.forward;
        fallback.y = 0f;
        // Vector3.back matches the PlayerController's default initial facing direction.
        return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector3.back;
    }
}
