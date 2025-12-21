using UnityEngine;
using UnityEngine.UI;

public class UIShakeFear : MonoBehaviour
{
    // Reference to the RectTransform of the UI Image
    private RectTransform rectTransform;

    // How strong the shake is
    [SerializeField] private float shakeIntensity = 5f;

    // How fast the shake changes direction
    [SerializeField] private float shakeSpeed = 20f;

    // Store the original anchored position so it can return to it
    private Vector2 originalPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Create small random offset using sine and cosine for smooth jitter
        float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
        float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity * 0.8f;

        // Apply the offset to make it “shake”
        rectTransform.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
    }
}
