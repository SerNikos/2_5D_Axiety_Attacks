using UnityEngine;
using UnityEngine.UI;

public class UIRotateWithMouse : MonoBehaviour
{
    // Reference to the RectTransform of your UI Image
    private RectTransform rectTransform;

    void Start()
    {
        // Get the RectTransform component
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Convert the mouse position to UI canvas space
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            Input.mousePosition,
            null, // camera is null if Canvas is in Screen Space - Overlay
            out mousePos
        );

        // Calculate direction from the image to the mouse
        Vector2 direction = mousePos - rectTransform.anchoredPosition;

        // Get angle in degrees
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Apply rotation (subtract 90 if your image points up instead of right)
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
