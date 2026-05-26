using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
	// Movement Speeds
	[SerializeField] private float walkSpeed = 5f;
	[SerializeField] private float sprintSpeed = 8f;
	[SerializeField] private float crouchSpeed = 2.6f;
	[SerializeField] private float crawlSpeed = 1.5f;

	// Jump and Gravity
	[SerializeField] private float jumpHeight = 1.25f;
	[SerializeField] private float gravity = -25f;

	// Flight Mode
	[SerializeField] private float flightSpeed = 7f;
	[SerializeField] private float flightSprintMultiplier = 2f;
	[SerializeField] private float flightVerticalSpeed = 6f;
	[SerializeField] private float doubleTapJumpWindow = 0.28f;

	// Stance
	[SerializeField] private float standingHeight = 1.8f;
	[SerializeField] private float crouchHeight = 1.4f;
	[SerializeField] private float crawlHeight = 0.6f;

	// Crouch Edge Protection
	[SerializeField] private float edgeCheckDownDistance = 1.4f;
	[SerializeField] private LayerMask edgeCheckMask = ~0;

	// Runtime State
	private CharacterController characterController;
	private InputHandler inputHandler;
	private float verticalVelocity;
	private bool isFlying;
	private float lastJumpPressedTime = -10f;

	// Public Debug State
	public bool IsFlying => isFlying;
	public bool IsCrouching => !inputHandler.IsCrawling && inputHandler.IsCrouching;
	public bool IsCrawling => inputHandler.IsCrawling;

	private void Awake()
	{
		characterController = GetComponent<CharacterController>();
		inputHandler = GetComponent<InputHandler>();
		ApplyCharacterHeight(standingHeight);
	}

	private void Update()
	{
		UpdateFlightToggle();
		UpdateStance();

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

	private void UpdateStance()
	{
		float targetHeight;
		if (inputHandler.IsCrawling)
		{
			targetHeight = crawlHeight;
		}
		else if (inputHandler.IsCrouching)
		{
			targetHeight = crouchHeight;
		}
		else
		{
			targetHeight = standingHeight;
		}

		if (targetHeight > characterController.height && !CanExpandToHeight(targetHeight))
		{
			return;
		}

		ApplyCharacterHeight(targetHeight);
	}

	private void HandleGroundedMovement()
	{
		bool isGrounded = characterController.isGrounded;
		if (isGrounded && verticalVelocity < 0f)
		{
			verticalVelocity = -2f;
		}

		Vector2 moveInput = inputHandler.MoveInput;
		float speed = GetGroundSpeed();
		Vector3 horizontalMove = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;

		if (inputHandler.IsCrouching && isGrounded && WouldStepOffEdge(horizontalMove * Time.deltaTime))
		{
			horizontalMove = Vector3.zero;
		}

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

	private float GetGroundSpeed()
	{
		if (inputHandler.IsCrawling)
		{
			return crawlSpeed;
		}
		if (inputHandler.IsCrouching)
		{
			return crouchSpeed;
		}
		return inputHandler.IsSprinting ? sprintSpeed : walkSpeed;
	}

	private void ApplyCharacterHeight(float targetHeight)
	{
		characterController.height = targetHeight;
		Vector3 center = characterController.center;
		center.y = targetHeight * 0.5f;
		characterController.center = center;
	}

	private bool CanExpandToHeight(float targetHeight)
	{
		float radius = Mathf.Max(0.01f, characterController.radius - 0.02f);
		Vector3 baseCenter = transform.position + Vector3.up * radius;
		Vector3 topCenter = transform.position + Vector3.up * (targetHeight - radius);
		return !Physics.CheckCapsule(baseCenter, topCenter, radius, edgeCheckMask, QueryTriggerInteraction.Ignore);
	}

	private bool WouldStepOffEdge(Vector3 horizontalDelta)
	{
		horizontalDelta.y = 0f;
		if (horizontalDelta.sqrMagnitude <= 0.000001f)
		{
			return false;
		}

		Vector3 probeOrigin = transform.position + horizontalDelta + Vector3.up * 0.15f;
		return !Physics.Raycast(probeOrigin, Vector3.down, edgeCheckDownDistance, edgeCheckMask, QueryTriggerInteraction.Ignore);
	}
}
