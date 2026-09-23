using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DepthSort2D : MonoBehaviour
{
    public enum SortDirection
    {
        CameraDepth,
        WorldZ,
        WorldY
    }

    [Header("Sorting")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SortDirection sortDirection = SortDirection.CameraDepth;
    [SerializeField] private int orderMultiplier = 100;
    [SerializeField] private int orderOffset;
    [SerializeField] private bool sortChildRenderers = true;
    [SerializeField] private bool spritesOnly = true;

    private Renderer[] renderers;
    private int[] baseSortingOrders;
    private int[] baseSortingLayerIds;
    private float lastSortValue = float.NaN;
    private int lastCameraInstanceId;
    private Vector3 sortPosition;
    private bool hasSortPositionOverride;
    private int runtimeOrderOffset;
    private bool hasRuntimeOrderOffset;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnEnable()
    {
        SortNow();
    }

    private void LateUpdate()
    {
        SortNow();
    }

    public void SortNow()
    {
        if (renderers == null)
        {
            CacheRenderers();
        }

        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Camera activeCamera = GetTargetCamera();
        float sortValue = GetSortValue(activeCamera);
        int cameraInstanceId = activeCamera != null ? activeCamera.GetInstanceID() : 0;

        if (Mathf.Approximately(sortValue, lastSortValue) && cameraInstanceId == lastCameraInstanceId)
        {
            return;
        }

        int currentOrderOffset = hasRuntimeOrderOffset ? runtimeOrderOffset : orderOffset;
        int depthOrder = -Mathf.RoundToInt(sortValue * orderMultiplier) + currentOrderOffset;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.sortingLayerID = baseSortingLayerIds[i];
            renderer.sortingOrder = baseSortingOrders[i] + depthOrder;
        }

        lastSortValue = sortValue;
        lastCameraInstanceId = cameraInstanceId;
    }

    public void SetSortDirection(SortDirection direction)
    {
        if (sortDirection == direction)
        {
            return;
        }

        sortDirection = direction;
        lastSortValue = float.NaN;
    }

    public void SetSortPosition(Vector3 position)
    {
        sortPosition = position;
        hasSortPositionOverride = true;
        lastSortValue = float.NaN;
    }

    public void ClearSortPositionOverride()
    {
        hasSortPositionOverride = false;
        lastSortValue = float.NaN;
    }

    public void SetSpritesOnly(bool value)
    {
        if (spritesOnly == value)
        {
            return;
        }

        spritesOnly = value;
        CacheRenderers();
        lastSortValue = float.NaN;
    }

    public void SetOrderOffset(int offset)
    {
        runtimeOrderOffset = offset;
        hasRuntimeOrderOffset = true;
        lastSortValue = float.NaN;
    }

    public void ClearOrderOffset()
    {
        hasRuntimeOrderOffset = false;
        lastSortValue = float.NaN;
    }

    private void CacheRenderers()
    {
        Renderer[] foundRenderers = sortChildRenderers
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();

        if (spritesOnly)
        {
            List<Renderer> spriteRenderers = new List<Renderer>();
            for (int i = 0; i < foundRenderers.Length; i++)
            {
                if (foundRenderers[i] is SpriteRenderer)
                {
                    spriteRenderers.Add(foundRenderers[i]);
                }
            }

            renderers = spriteRenderers.ToArray();
        }
        else
        {
            renderers = foundRenderers;
        }

        baseSortingOrders = new int[renderers.Length];
        baseSortingLayerIds = new int[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            baseSortingOrders[i] = renderers[i].sortingOrder;
            baseSortingLayerIds[i] = renderers[i].sortingLayerID;
        }
    }

    private Camera GetTargetCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        return Camera.main;
    }

    private float GetSortValue(Camera activeCamera)
    {
        Vector3 sourcePosition = hasSortPositionOverride ? sortPosition : transform.position;

        switch (sortDirection)
        {
            case SortDirection.WorldZ:
                return sourcePosition.z;
            case SortDirection.WorldY:
                return sourcePosition.y;
            case SortDirection.CameraDepth:
            default:
                if (activeCamera == null)
                {
                    return sourcePosition.z;
                }

                return Vector3.Dot(sourcePosition - activeCamera.transform.position, activeCamera.transform.forward);
        }
    }
}
