using UnityEngine;
using System.Collections;

public class FireballAndChangeScene : MonoBehaviour
{
    [Header("References")]
    public LevelLoader levelLoader;       // Reference to your LevelLoader script
    public GameObject fireballPrefab;     // The fireball prefab to instantiate

    [Header("Settings")]
    public float fadeDuration = 1.5f;     // Fade in time
    public float throwSpeed = 1000f;      // How fast the fireball moves (UI units/sec)
    public float throwTime = 1.5f;        // How long it moves before scene change

    public AudioSource audioSouce;
    public AudioClip castFireball;


    // Cast fireball and move it
    public void CastFireBall(int level)
    {
        StartCoroutine(FadeInAndThrow(level));
    }

    // Cast fireball without changing scene
    public void CastFireBallNoScene()
    {
        StartCoroutine(FadeInAndThrow(-1));
    }

    private IEnumerator FadeInAndThrow(int levelToFade)
    {
        if (fireballPrefab == null)
        {
            Debug.LogWarning("Fireball prefab not assigned!");
            yield break;
        }

        // Instantiate fireball as child
        GameObject fireball = Instantiate(fireballPrefab, transform);
        RectTransform fireballRect = fireball.GetComponent<RectTransform>();
        UnityEngine.UI.Image fireballImage = fireball.GetComponent<UnityEngine.UI.Image>();

        // Start fully transparent
        Color color = fireballImage.color;
        color.a = 0f;
        fireballImage.color = color;

        audioSouce.PlayOneShot(castFireball);
        // Step 1: Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeDuration);
            fireballImage.color = color;
            yield return null;
        }

        // Step 2: Throw fireball
        elapsed = 0f;
        while (elapsed < throwTime)
        {
            elapsed += Time.deltaTime;
            fireballRect.anchoredPosition += Vector2.right * throwSpeed * Time.deltaTime;
            yield return null;
        }

        // Step 3: Optional scene change
        if (levelToFade >= 0 && levelLoader != null)
        {
            levelLoader.ChangeSceneWithFade(levelToFade);
        }

        // Destroy the fireball after use
        Destroy(fireball);
    }
}
