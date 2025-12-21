using UnityEngine;

public class CursorChanger : MonoBehaviour
{
    public Texture2D defaultCursor;   // The normal cursor texture
    public Texture2D clickCursor;     // The texture to show when clicked
    public Vector2 hotSpot = Vector2.zero; // The hotspot (pivot point) of the cursor
    public CursorMode cursorMode = CursorMode.Auto;

    void Start()
    {
        // Set the default cursor when the game starts
        Cursor.SetCursor(defaultCursor, hotSpot, cursorMode);
    }

    void Update()
    {
        // Change cursor when mouse button is pressed
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(clickCursor, hotSpot, cursorMode);
        }

        // Return to default cursor when mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            Cursor.SetCursor(defaultCursor, hotSpot, cursorMode);
        }
    }
}
