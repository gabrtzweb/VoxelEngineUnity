using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement), typeof(PlayerInteractions), typeof(CameraController))]
[RequireComponent(typeof(CharacterController))]
public class InputHandler : MonoBehaviour {
    GameInput input;
    
    public PlayerMovement Movement { get; private set; }
    public PlayerInteractions Interactions { get; private set; }
    public CameraController CameraCtrl { get; private set; }
    public CharacterController CharController { get; private set; }
    public Vector2 MoveInput => input.Player.Move.ReadValue<Vector2>();
    public Vector2 LookInput => input.Player.Look.ReadValue<Vector2>();
    public bool IsSprinting => input.Player.Sprint.IsPressed();
    public bool IsJumping => input.Player.Jump.WasPressedThisFrame();
    public bool IsJumpHeld => input.Player.Jump.IsPressed();
    public bool PrimaryActionPressed => input.Player.UsePrimary.WasPressedThisFrame();
    public bool SecondaryActionPressed => input.Player.UseSecondary.WasPressedThisFrame();
    public bool IsPrimaryActionHeld => input.Player.UsePrimary.IsPressed();
    public bool IsSecondaryActionHeld => input.Player.UseSecondary.IsPressed();

    // Optional actions: the generated GameInput asset may not include these actions.
    InputAction FindPlayerAction(string name) {
        if (input == null || input.asset == null) return null;
        return input.asset.FindAction($"Player/{name}");
    }

    public bool PickBlockPressed {
        get {
            var a = FindPlayerAction("PickBlock");
            return a != null && a.WasPressedThisFrame();
        }
    }

    public bool IsCrouching => input.Player.Crouch.IsPressed();

    public bool IsCrawling {
        get {
            var a = FindPlayerAction("Crawl");
            return a != null && a.IsPressed();
        }
    }

    public bool ToggleMapPressed {
        get {
            var a = FindPlayerAction("ToggleMap");
            return a != null && a.WasPressedThisFrame();
        }
    }

    void Awake() {
        input = new GameInput();
        
        Movement = GetComponent<PlayerMovement>();
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
