using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class RuntimeNavMeshBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -= BuildSceneNavMeshes;
        SceneManager.sceneLoaded += BuildSceneNavMeshes;
    }

    private static void BuildSceneNavMeshes(Scene scene, LoadSceneMode mode)
    {
        NavMeshSurface[] surfaces = Object.FindObjectsOfType<NavMeshSurface>();
        int activeSurfaceCount = 0;

        foreach (NavMeshSurface surface in surfaces)
        {
            if (surface == null || !surface.isActiveAndEnabled)
            {
                continue;
            }

            activeSurfaceCount++;
            if (surface.navMeshData == null)
            {
                surface.BuildNavMesh();
                if (surface.navMeshData == null)
                {
                    Debug.LogError($"NavMeshSurface '{surface.name}' has no baked NavMeshData and failed to build one in scene '{scene.name}'.", surface);
                }
                else
                {
                    Debug.Log($"Runtime NavMesh built for '{surface.name}' in scene '{scene.name}'.", surface);
                }
            }
        }

        int vertexCount = NavMesh.CalculateTriangulation().vertices.Length;
        if (activeSurfaceCount == 0 || vertexCount == 0)
        {
            Debug.LogError($"Scene '{scene.name}' has {activeSurfaceCount} active NavMeshSurface(s) and {vertexCount} NavMesh vertices. Enemy navigation will not work until a valid NavMesh is loaded.");
        }
        else
        {
            Debug.Log($"Scene '{scene.name}' loaded {activeSurfaceCount} active NavMeshSurface(s) with {vertexCount} NavMesh vertices.");
        }
    }
}
