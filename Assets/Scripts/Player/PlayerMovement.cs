using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
	// Movement Speeds
	[SerializeField] private float walkSpeed = 5f;
	[SerializeField] private float sprintSpeed = 8f;

	// Jump and Gravity
	[SerializeField] private float jumpHeight = 1.25f;
	[SerializeField] private float gravity = -25f;

	// Flight Mode
	[SerializeField] private float flightSpeed = 7f;
	[SerializeField] private float flightSprintMultiplier = 2f;
	[SerializeField] private float flightVerticalSpeed = 6f;
	[SerializeField] private float doubleTapJumpWindow = 0.28f;

	// Runtime State
	private CharacterController characterController;
	private InputHandler inputHandler;
	private float verticalVelocity;
	private bool isFlying;
	private float lastJumpPressedTime = -10f;

	private void Awake()
	{
		characterController = GetComponent<CharacterController>();
		inputHandler = GetComponent<InputHandler>();
	}

	private void Update()
	{
		UpdateFlightToggle();

		if (isFlying)
		{
			HandleFlightMovement();
			return;
		}

		HandleGroundedMovement();
	}

	private void UpdateFlightToggle()
	{
		if (!inputHandler.IsJumping)
		{
			return;
		}

		if (Time.time - lastJumpPressedTime <= doubleTapJumpWindow)
		{
			isFlying = !isFlying;
			if (isFlying)
			{
				verticalVelocity = 0f;
			}
		}

		lastJumpPressedTime = Time.time;
	}

	private void HandleGroundedMovement()
	{
		bool isGrounded = characterController.isGrounded;
		if (isGrounded && verticalVelocity < 0f)
		{
			verticalVelocity = -2f;
		}

		Vector2 moveInput = inputHandler.MoveInput;
		float speed = inputHandler.IsSprinting ? sprintSpeed : walkSpeed;
		Vector3 horizontalMove = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;

		if (isGrounded && inputHandler.IsJumping)
		{
			verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
		}

		verticalVelocity += gravity * Time.deltaTime;
		horizontalMove.y = verticalVelocity;

		characterController.Move(horizontalMove * Time.deltaTime);
	}

	private void HandleFlightMovement()
	{
		Vector2 moveInput = inputHandler.MoveInput;
		float speed = inputHandler.IsSprinting ? flightSpeed * flightSprintMultiplier : flightSpeed;
		Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;

		float verticalInput = 0f;
		if (inputHandler.IsJumpHeld)
		{
			verticalInput += 1f;
		}
		if (inputHandler.IsCrouching)
		{
			verticalInput -= 1f;
		}
		move.y = verticalInput * flightVerticalSpeed;

		characterController.Move(move * Time.deltaTime);
	}
}
