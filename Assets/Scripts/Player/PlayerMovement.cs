using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(InputHandler))]
public class PlayerMovement : MonoBehaviour {
    [SerializeField] PlayerConfig playerConfig;
    public Transform cameraTarget;
    
    CharacterController charController;
    InputHandler inputHandler;
    Vector3 velocity;

    bool isFlying = false;
    bool isInWater = false;
    bool wasJumping = false;
    float lastJumpPressTime = 0f;

    void Awake() {
        charController = GetComponent<CharacterController>();
        inputHandler = GetComponent<InputHandler>();
    }

    void Update() {
        if (!charController.enabled) return;
        ApplyStance();
        MovePlayer();
    }

    void ApplyStance() {
        if (cameraTarget == null) return;

        float targetHeight = playerConfig.standingHeight;
        float camTargetY = 1.5f;

        bool isSwimmingFast = isInWater && inputHandler.IsSprinting;

        if (inputHandler.IsCrawling || isSwimmingFast) {
            targetHeight = playerConfig.crawlingHeight;
            camTargetY = 0.6f;
        } else if (inputHandler.IsCrouching && (!isFlying || isInWater)) {
            targetHeight = playerConfig.crouchingHeight;
            camTargetY = 1.2f;
        }

        charController.height = Mathf.Lerp(charController.height, targetHeight, Time.deltaTime * playerConfig.stanceTransitionSpeed);
        charController.center = new Vector3(0, charController.height / 2f, 0);

        Vector3 camPos = cameraTarget.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, camTargetY, Time.deltaTime * playerConfig.stanceTransitionSpeed);
        cameraTarget.localPosition = camPos;
    }


    void MovePlayer() {
        bool jumpInput = inputHandler.IsJumping;
        bool jumpHeld = inputHandler.IsJumpHeld;

        HandleDoubleJumpForFlight(jumpInput);
        
        if (isFlying) {
            if (isInWater) {
                HandleWaterFlightMovement(jumpHeld);
            } else {
                HandleFlightMovement(jumpHeld);
            }
        } else if (isInWater) {
            HandleWaterMovement(jumpHeld);
        } else {
            HandleGroundMovement(jumpInput, jumpHeld);
        }

        wasJumping = jumpInput;
    }

    void HandleDoubleJumpForFlight(bool jumpInput) {
        if (jumpInput && !wasJumping) {
            if (Time.time - lastJumpPressTime <= playerConfig.doubleTapWindow) {
                isFlying = !isFlying;
            }
            lastJumpPressTime = Time.time;
        }
    }

    void HandleGroundMovement(bool jumpInput, bool jumpHeld) {
        Vector3 horizontalMove = GetHorizontalMove();

        float currentSpeed = GetCurrentSpeed();
        Vector3 finalVelocity = horizontalMove * currentSpeed;
        bool isGrounded = charController.isGrounded;

        HandleEdgeDetection(ref finalVelocity, isGrounded);
        HandleJumping(jumpInput, isGrounded);
        HandleGravity();

        finalVelocity.y = velocity.y;
        charController.Move(finalVelocity * Time.deltaTime);
    }

    void HandleEdgeDetection(ref Vector3 finalVelocity, bool isGrounded) {
        if (inputHandler.IsCrouching && isGrounded) {
            Vector3 futurePosX = transform.position + new Vector3(finalVelocity.x * Time.deltaTime, playerConfig.edgeCheckYOffset, 0);
            if (!Physics.Raycast(futurePosX, Vector3.down, playerConfig.edgeCheckDistance)) {
                finalVelocity.x = 0;
            }

            Vector3 futurePosZ = transform.position + new Vector3(0, playerConfig.edgeCheckYOffset, finalVelocity.z * Time.deltaTime);
            if (!Physics.Raycast(futurePosZ, Vector3.down, playerConfig.edgeCheckDistance)) {
                finalVelocity.z = 0;
            }
        }
    }

    void HandleJumping(bool jumpInput, bool isGrounded) {
        if (isGrounded && velocity.y < 0) {
            velocity.y = playerConfig.groundedGravityReset;
        }

        bool canJump = !inputHandler.IsCrawling && !inputHandler.IsCrouching;
        if (jumpInput && isGrounded && canJump && !wasJumping) {
            velocity.y = Mathf.Sqrt(playerConfig.jumpHeight * playerConfig.groundedGravityReset * playerConfig.gravity);
        }
    }

    void HandleGravity() {
        velocity.y += playerConfig.gravity * Time.deltaTime;
    }

    float GetCurrentSpeed() {
        if (inputHandler.IsCrawling) return playerConfig.crawlSpeed;
        if (inputHandler.IsCrouching) return playerConfig.crouchSpeed;
        if (inputHandler.IsSprinting) return playerConfig.runSpeed;
        return playerConfig.walkSpeed;
    }

    void HandleWaterMovement(bool jumpHeld) {
        Vector3 horizontalMove = GetHorizontalMove();

        float currentSpeed = inputHandler.IsSprinting ? playerConfig.fastSwimSpeed : playerConfig.swimSpeed;
        Vector3 finalVelocity = horizontalMove * currentSpeed;

        if (jumpHeld) {
            velocity.y = Mathf.Lerp(velocity.y, playerConfig.swimUpSpeed, Time.deltaTime * playerConfig.verticalWaterDrag);
        } else if (inputHandler.IsCrouching) {
            velocity.y = Mathf.Lerp(velocity.y, -playerConfig.swimDownSpeed, Time.deltaTime * playerConfig.verticalWaterDrag);
        } else {
            velocity.y = Mathf.Lerp(velocity.y, playerConfig.waterSinkingSpeed, Time.deltaTime * playerConfig.verticalWaterDrag);
        }

        finalVelocity.y = velocity.y;
        charController.Move(finalVelocity * Time.deltaTime);
    }

    void HandleWaterFlightMovement(bool jumpHeld) {
        Vector3 horizontalMove = GetHorizontalMove();

        float currentSpeed = inputHandler.IsSprinting ? playerConfig.fastFlySpeed * 0.6f : playerConfig.flySpeed * 0.6f;
        Vector3 targetVelocity = horizontalMove * currentSpeed;

        if (jumpHeld) {
            targetVelocity.y = playerConfig.verticalFlySpeed * 0.6f;
        } else if (inputHandler.IsCrouching) {
            targetVelocity.y = -playerConfig.verticalFlySpeed * 0.6f;
        } else {
            targetVelocity.y = 0f;
        }

        velocity = Vector3.Lerp(velocity, targetVelocity, Time.deltaTime * playerConfig.verticalWaterDrag * 1.5f);

        charController.Move(velocity * Time.deltaTime);
    }

    void HandleFlightMovement(bool jumpHeld) {
        Vector3 horizontalMove = GetHorizontalMove();
        
        float currentSpeed = inputHandler.IsSprinting ? playerConfig.fastFlySpeed : playerConfig.flySpeed;
        Vector3 finalVelocity = horizontalMove * currentSpeed;

        if (jumpHeld) {
            velocity.y = playerConfig.verticalFlySpeed;
        } else if (inputHandler.IsCrouching) {
            velocity.y = -playerConfig.verticalFlySpeed;
        } else {
            velocity.y = 0f; 
        }

        finalVelocity.y = velocity.y;
        charController.Move(finalVelocity * Time.deltaTime);

        if (charController.isGrounded && !jumpHeld) {
            isFlying = false;
        }
    }

    Vector3 GetHorizontalMove() {
        Vector2 moveInput = inputHandler.MoveInput;
        return transform.right * moveInput.x + transform.forward * moveInput.y;
    }
}
