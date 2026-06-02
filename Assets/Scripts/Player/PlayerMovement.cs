using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
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

	// Movement Sounds
	[SerializeField] private AudioClip[] footstepClips;
	[SerializeField] private AudioClip jumpClip;
	[SerializeField] private AudioClip landingClip;
	[SerializeField] private float footstepDistance = 1.75f;
	[SerializeField] private float pitchVariation = 0.05f;

	private Vector3 currentHorizontalVelocity;
	private float footstepDistanceAccumulator;

	// Flight Mode
	[SerializeField] private float flightSpeed = 10.92f;
	[SerializeField] private float flightSprintMultiplier = 2f;
	[SerializeField] private float flightVerticalSpeed = 10.92f;
	[SerializeField] private float doubleTapJumpWindow = 0.28f;

	// Runtime State
	private CharacterController characterController;
	private InputHandler inputHandler;
	private AudioSource audioSource;
	[SerializeField] private Animator animator;
	[SerializeField] private bool debugAnimator = false;
	private float verticalVelocity;
	private bool isFlying;
	private bool wasGrounded;
	private float lastJumpPressedTime = -10f;

	public bool IsFlying => isFlying;
	public bool IsGrounded => characterController != null && characterController.isGrounded;
	public float LandingImpactVelocity { get; private set; }

	private void Awake()
	{
		characterController = GetComponent<CharacterController>();
		inputHandler = GetComponent<InputHandler>();
		audioSource = GetComponent<AudioSource>();
		if (audioSource != null)
		{
			audioSource.playOnAwake = false;
		}
		wasGrounded = characterController != null && characterController.isGrounded;
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
		bool justLanded = isGrounded && !wasGrounded;
		if (justLanded)
		{
			LandingImpactVelocity = Mathf.Abs(verticalVelocity);
		}

		if (isGrounded && verticalVelocity < 0f)
		{
			verticalVelocity = -2f;
		}

		Vector2 moveInput = inputHandler.MoveInput;
		Vector3 moveDirection = GetPlanarMoveDirection(moveInput);
		float targetSpeed = GetTargetMoveSpeed();
		Vector3 desiredVelocity = moveDirection * targetSpeed;
		bool hasMoveInput = moveInput.sqrMagnitude > 0.01f;

		if (isGrounded)
		{
			float acceleration = hasMoveInput ? groundAcceleration : groundDeceleration;
			currentHorizontalVelocity =
				Vector3.MoveTowards(
					currentHorizontalVelocity,
					hasMoveInput ? desiredVelocity : Vector3.zero,
					acceleration * Time.deltaTime);
		}
		else if (hasMoveInput)
		{
			currentHorizontalVelocity =
				Vector3.MoveTowards(
					currentHorizontalVelocity,
					desiredVelocity,
					airAcceleration * Time.deltaTime);
		}

		if (isGrounded && inputHandler.IsJumping)
		{
			verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
			PlayJumpSound();
			if (animator != null)
			{
				animator.SetTrigger("JumpTrigger");
			}
		}

		verticalVelocity += gravity * Time.deltaTime;
		Vector3 finalMove = currentHorizontalVelocity;
		finalMove.y = verticalVelocity;

		characterController.Move(finalMove * Time.deltaTime);
		wasGrounded = isGrounded;

		HandleMovementSounds(isGrounded, moveInput, justLanded);

		UpdateAnimatorParameters(moveInput, isGrounded);
	}

	private void HandleFlightMovement()
	{
		Vector2 moveInput = inputHandler.MoveInput;
		float speed = inputHandler.IsSprinting ? flightSpeed * flightSprintMultiplier : flightSpeed;
		Vector3 move = GetPlanarMoveDirection(moveInput) * speed;

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
		wasGrounded = characterController.isGrounded;

		UpdateAnimatorParameters(moveInput, /*isGrounded*/ false);
	}

	private Vector3 GetPlanarMoveDirection(Vector2 moveInput)
	{
		Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
		return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
	}

	private float GetTargetMoveSpeed()
	{
		return inputHandler.IsSprinting ? sprintSpeed : walkSpeed;
	}

	private void HandleMovementSounds(bool isGrounded, Vector2 moveInput, bool justLanded)
	{
		if (isFlying || !isGrounded)
		{
			footstepDistanceAccumulator = 0f;
			if (justLanded && landingClip != null)
			{
				PlayLandingSound(LandingImpactVelocity);
			}
			return;
		}

		if (justLanded && landingClip != null)
		{
			PlayLandingSound(LandingImpactVelocity);
		}

		if (moveInput.sqrMagnitude <= 0.01f || footstepDistance <= 0f)
		{
			footstepDistanceAccumulator = 0f;
			return;
		}

		footstepDistanceAccumulator += currentHorizontalVelocity.magnitude * Time.deltaTime;
		while (footstepDistanceAccumulator >= footstepDistance)
		{
			footstepDistanceAccumulator -= footstepDistance;
			PlayFootstepSound();
		}
	}

	private void PlayFootstepSound()
	{
		if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
		{
			return;
		}

		AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
		if (clip == null)
		{
			return;
		}

		PlayClipWithPitchVariation(clip, 1f);
	}

	private void PlayJumpSound()
	{
		if (audioSource == null || jumpClip == null)
		{
			return;
		}

		PlayClipWithPitchVariation(jumpClip, 1f);
	}

	private void PlayLandingSound(float impactVelocity)
	{
		if (audioSource == null || landingClip == null)
		{
			return;
		}

		float landingVolume = Mathf.Lerp(0.35f, 0.9f, Mathf.Clamp01(impactVelocity / 12f));
		PlayClipWithPitchVariation(landingClip, landingVolume);
	}

	private void PlayClipWithPitchVariation(AudioClip clip, float volumeScale)
	{
		float originalPitch = audioSource.pitch;
		if (pitchVariation > 0f)
		{
			audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
		}

		audioSource.PlayOneShot(clip, volumeScale);
		audioSource.pitch = originalPitch;
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
