using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement), typeof(PlayerInteractions), typeof(CameraController))]
[RequireComponent(typeof(CharacterController))]
public class InputHandler : MonoBehaviour {
    GameInput input;
    
    // Component References
    public PlayerMovement Movement { get; private set; }
    public PlayerInteractions Interactions { get; private set; }
    public CameraController CameraCtrl { get; private set; }
    public CharacterController CharController { get; private set; }

    // Input Accessors
    public Vector2 MoveInput => input.Player.Move.ReadValue<Vector2>();
    public Vector2 LookInput => input.Player.Look.ReadValue<Vector2>();
    public bool IsSprinting => input.Player.Sprint.IsPressed();
    public bool IsJumping => input.Player.Jump.WasPressedThisFrame();
    public bool IsJumpHeld => input.Player.Jump.IsPressed();

    public bool IsCrouching => input.Player.Crouch.IsPressed();
    public bool IsCrawling => input.Player.Crawl.IsPressed();

    public bool PrimaryActionPressed => input.Player.UsePrimary.WasPressedThisFrame();
    public bool SecondaryActionPressed => input.Player.UseSecondary.WasPressedThisFrame();
    public bool IsPrimaryActionHeld => input.Player.UsePrimary.IsPressed();
    public bool IsSecondaryActionHeld => input.Player.UseSecondary.IsPressed()
    ;
    public bool PickActionPressed => input.Player.PickAction.WasPressedThisFrame();
    public bool DropActionPressed => input.Player.DropAction.WasPressedThisFrame();

    void Awake() {
        input = new GameInput();
        
        Movement = GetComponent<PlayerMovement>();
        Interactions = GetComponent<PlayerInteractions>();
        CameraCtrl = GetComponent<CameraController>();
        CharController = GetComponent<CharacterController>();
    }
    void OnEnable() {
        input.Enable();
    }

    void OnDisable() {
        input.Disable();
    }
}
