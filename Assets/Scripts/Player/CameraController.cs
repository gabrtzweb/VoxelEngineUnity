using UnityEngine;

[RequireComponent(typeof(InputHandler))]
public class CameraController : MonoBehaviour {
    [Header("First Person")]
    public Transform cameraTarget;

    [Header("Settings")]
    public float mouseSensitivity = 0.4f;
    public float topClamp = -80f;
    public float bottomClamp = 80f;
    
    InputHandler inputHandler;
    float cameraPitch = 0f;
    float playerYaw = 0f;

    void Awake() {
        inputHandler = GetComponent<InputHandler>();

        if (cameraTarget == null && Camera.main != null) {
            cameraTarget = Camera.main.transform;
        }
    }

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        HandleRotation();
    }

    void HandleRotation() {
        if (cameraTarget == null)
            return;

        Vector2 lookInput = inputHandler.LookInput * mouseSensitivity;

        playerYaw += lookInput.x;
        cameraPitch -= lookInput.y;
        
        cameraPitch = Mathf.Clamp(cameraPitch, topClamp, bottomClamp);

        cameraTarget.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, playerYaw, 0f);
    }
}
