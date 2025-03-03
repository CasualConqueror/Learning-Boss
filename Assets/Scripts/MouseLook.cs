using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody; // Reference to the character's body
    public float mouseSensitivity = 100f; // Sensitivity of the mouse
    public Camera mainCamera; // Reference to the camera

    private float xRotation = 0f; // Vertical rotation for the camera

    void Start()
    {
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Automatically find the main camera
        }
    }

    void Update()
    {
        // Mouse input for rotation
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Calculate vertical rotation (up/down) and clamp it
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply rotation to the camera (vertical rotation)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Apply rotation to the player body (horizontal rotation)
        playerBody.Rotate(Vector3.up * mouseX);

        // Toggle camera projection mode
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleCameraMode();
        }
    }

    void ToggleCameraMode()
    {
        if (mainCamera != null)
        {
            mainCamera.orthographic = !mainCamera.orthographic;

            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = 5f; // Adjust size for orthographic view
                Debug.Log("Switched to Orthographic mode.");
            }
            else
            {
                Debug.Log("Switched to Perspective mode.");
            }
        }
    }
}
