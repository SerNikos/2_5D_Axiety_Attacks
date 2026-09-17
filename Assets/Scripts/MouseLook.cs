using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // Κλειδώνει και κρύβει το ποντίκι στο κέντρο της οθόνης
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Λήψη κίνησης ποντικιού
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Υπολογισμός κατακόρυφης περιστροφής (πάνω/κάτω)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Όριο 90 μοιρών

        // Περιστροφή κάμερας (πάνω/κάτω)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Περιστροφή του σώματος του παίκτη (αριστερά/δεξιά)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}