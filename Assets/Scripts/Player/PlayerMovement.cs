using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
	// Movement Speeds
	[SerializeField] private float walkSpeed = 4.317f;
	[SerializeField] private float sprintSpeed = 5.612f;

	// Jump and Gravity
	[SerializeField] private float jumpHeight = 1.252f;
	[SerializeField] private float gravity = -20f;
	[SerializeField] private float groundAcceleration = 35f;
	[SerializeField] private float groundDeceleration = 40f;
	[SerializeField] private float airAcceleration = 12f;

	private Vector3 currentHorizontalVelocity;

	// Flight Mode
	[SerializeField] private float flightSpeed = 10.92f;
	[SerializeField] private float flightSprintMultiplier = 2f;
	[SerializeField] private float flightVerticalSpeed = 10.92f;
	[SerializeField] private float doubleTapJumpWindow = 0.28f;

	// Runtime State
	private CharacterController characterController;
	private InputHandler inputHandler;
	[SerializeField] private Animator animator;
	[SerializeField] private bool debugAnimator = false;
	private float verticalVelocity;
	private bool isFlying;
	private float lastJumpPressedTime = -10f;

	private void Awake()
	{
		characterController = GetComponent<CharacterController>();
		inputHandler = GetComponent<InputHandler>();
		if (animator == null)
		{
			animator = GetComponent<Animator>();
			if (animator == null)
			{
				animator = GetComponentInChildren<Animator>();
			}
			if (animator == null && debugAnimator)
			{
				Debug.LogWarning("PlayerMovement: Animator not found on GameObject or children. Please assign an Animator in the inspector.");
			}
		}
	}

	private void Update()
	{
		UpdateFlightToggle();

		if (isFlying)
		{
			HandleFlightMovement();
			UpdateAnimatorParameters(Vector2.zero); // flight movement sets its own speed inside the handler
			return;
		}

		HandleGroundedMovement();
		// animator params updated inside grounded handler
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

		float targetSpeed =
			inputHandler.IsSprinting
				? sprintSpeed
				: walkSpeed;

		Vector3 desiredVelocity =
			(transform.right * moveInput.x +
			transform.forward * moveInput.y).normalized
			* targetSpeed;

		float acceleration =
			isGrounded
				? (moveInput.sqrMagnitude > 0.01f
					? groundAcceleration
					: groundDeceleration)
				: airAcceleration;

		currentHorizontalVelocity =
			Vector3.MoveTowards(
				currentHorizontalVelocity,
				desiredVelocity,
				acceleration * Time.deltaTime);

		if (isGrounded && inputHandler.IsJumping)
		{
			verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
			if (animator != null)
			{
				animator.SetTrigger("JumpTrigger");
			}
		}

		verticalVelocity += gravity * Time.deltaTime;
		Vector3 finalMove = currentHorizontalVelocity;
		finalMove.y = verticalVelocity;

		characterController.Move(finalMove * Time.deltaTime);

		UpdateAnimatorParameters(moveInput, isGrounded);
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

		UpdateAnimatorParameters(moveInput, /*isGrounded*/ false);
	}

	private void UpdateAnimatorParameters(Vector2 moveInput, bool isGrounded = true)
	{
		if (animator == null) return;

		// Speed: use normalized horizontal input magnitude (0..1) for blending
		float normalizedSpeed = Mathf.Clamp01(moveInput.magnitude);
		animator.SetFloat("Speed", normalizedSpeed);

		if (debugAnimator)
		{
			Debug.Log($"Animator Params -> Speed: {normalizedSpeed}, IsGrounded: {isGrounded}, IsFlying: {isFlying}, IsSprinting: {inputHandler.IsSprinting}");
		}

		animator.SetBool("IsGrounded", isGrounded);
		animator.SetBool("IsSprinting", inputHandler.IsSprinting);
		animator.SetBool("IsFlying", isFlying);
	}
}
