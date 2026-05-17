using UnityEngine;

[RequireComponent(typeof(InputHandler), typeof(CharacterController))]
public class PlayerInteractions : MonoBehaviour {
    [SerializeField] PlayerConfig playerConfig;

    InputHandler inputHandler;
    CharacterController playerController;
    Transform playerCamera;

    float lastInteractTime;
    
    private Vector3Int cachedHitPos;
    private Vector3Int cachedAdjacentPos;
    private bool cachedRaycastHit;
    
    void Awake() {
        inputHandler = GetComponent<InputHandler>();
        playerController = GetComponent<CharacterController>();
    }

    void Start() {
        playerCamera = Camera.main.transform;
    }
}
