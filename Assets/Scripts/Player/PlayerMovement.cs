using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
	private enum MovementStance
	{
		Standing,
		Crouching,
		Crawling,
	}

	[SerializeField] private MovementConfig config;
	[SerializeField] private Animator animator;

	private Vector3 currentHorizontalVelocity;
	private float footstepDistanceAccumulator;
	private MovementStance currentStance = MovementStance.Standing;
	private MovementStance targetStance = MovementStance.Standing;
	private float currentControllerHeight;
	private float currentEyeHeight;

	// Runtime State
	private CharacterController characterController;
	private InputHandler inputHandler;
	private AudioSource audioSource;
	private float verticalVelocity;
	private bool isFlying;
	private bool wasGrounded;
	private float lastJumpPressedTime = -10f;
		private static readonly int speedHash = Animator.StringToHash("Speed");
		private static readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
		private static readonly int isSprintingHash = Animator.StringToHash("IsSprinting");
		private static readonly int isFlyingHash = Animator.StringToHash("IsFlying");
		private static readonly int jumpTriggerHash = Animator.StringToHash("JumpTrigger");

	public bool IsFlying => isFlying;
	public bool IsGrounded => characterController != null && characterController.isGrounded;
	public bool IsCrouching => currentStance == MovementStance.Crouching;
	public bool IsCrawling => currentStance == MovementStance.Crawling;
	public float CameraEyeHeight => currentEyeHeight;
	public float LandingImpactVelocity { get; private set; }

	private void Awake()
	{
		characterController = GetComponent<CharacterController>();
		inputHandler = GetComponent<InputHandler>();
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		if (audioSource != null)
		{
			audioSource.playOnAwake = false;
		}

		currentControllerHeight = config.standingHeight > 0f ? config.standingHeight : characterController.height;
		currentEyeHeight = config.standingEyeHeight;
		ApplyControllerDimensions(currentControllerHeight);
		currentStance = MovementStance.Standing;
		wasGrounded = characterController != null && characterController.isGrounded;
		if (animator == null)
		{
			animator = GetComponent<Animator>();
			if (animator == null)
			{
				animator = GetComponentInChildren<Animator>();
			}
			if (animator == null && config.debugAnimator)
			{
				Debug.LogWarning("PlayerMovement: Animator not found on GameObject or children. Please assign an Animator in the inspector.");
			}
		}
	}

	private void Update()
	{
		UpdateFlightToggle();

		if (!isFlying)
		{
			UpdateTargetStance();
		}

		UpdateStanceDimensions();

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

		if (Time.time - lastJumpPressedTime <= config.doubleTapJumpWindow)
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
			float acceleration = hasMoveInput ? config.groundAcceleration : config.groundDeceleration;
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
					config.airAcceleration * Time.deltaTime);
		}

		if (isGrounded && IsEdgeSafetyEnabled() && currentHorizontalVelocity.sqrMagnitude > 0f)
		{
			ConstrainCrouchEdgeMovement(ref currentHorizontalVelocity);
		}

		if (isGrounded && inputHandler.IsJumping)
		{
			verticalVelocity = Mathf.Sqrt(config.jumpHeight * -2f * config.gravity);
			PlayJumpSound();
			if (animator != null)
			{
				animator.SetTrigger(jumpTriggerHash);
			}
		}

		verticalVelocity += config.gravity * Time.deltaTime;
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
		float speed = inputHandler.IsSprinting ? config.flightSpeed * config.flightSprintMultiplier : config.flightSpeed;
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
		move.y = verticalInput * config.flightVerticalSpeed;

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
		switch (targetStance)
		{
			case MovementStance.Crouching:
				return config.crouchSpeed;
			case MovementStance.Crawling:
				return config.crawlSpeed;
			default:
				return inputHandler.IsSprinting ? config.sprintSpeed : config.walkSpeed;
		}
	}

	private void UpdateTargetStance()
	{
		if (characterController == null || !characterController.isGrounded)
		{
			targetStance = currentStance;
			return;
		}

		MovementStance requestedStance = MovementStance.Standing;
		if (inputHandler.IsCrawling)
		{
			requestedStance = MovementStance.Crawling;
		}
		else if (inputHandler.IsCrouching)
		{
			requestedStance = MovementStance.Crouching;
		}

		targetStance = ResolveStance(requestedStance);
	}

	private MovementStance ResolveStance(MovementStance requestedStance)
	{
		switch (requestedStance)
		{
			case MovementStance.Crawling:
				if (CanFitHeight(config.crawlHeight))
				{
					return MovementStance.Crawling;
				}

				if (CanFitHeight(config.crouchHeight))
				{
					return MovementStance.Crouching;
				}

				return targetStance;
			case MovementStance.Crouching:
				if (CanFitHeight(config.crouchHeight))
				{
					return MovementStance.Crouching;
				}

				if (CanFitHeight(config.crawlHeight))
				{
					return MovementStance.Crawling;
				}

				return targetStance == MovementStance.Crawling ? MovementStance.Crawling : MovementStance.Standing;
			default:
				if (CanFitHeight(config.standingHeight))
				{
					return MovementStance.Standing;
				}

				if (CanFitHeight(config.crouchHeight))
				{
					return MovementStance.Crouching;
				}

				if (CanFitHeight(config.crawlHeight))
				{
					return MovementStance.Crawling;
				}

				return targetStance;
		}
	}

	private void UpdateStanceDimensions()
	{
		if (characterController == null)
		{
			return;
		}

		float targetHeight = GetStanceHeight(targetStance);
		float targetEye = GetStanceEyeHeight(targetStance);

		currentControllerHeight = Mathf.MoveTowards(currentControllerHeight, targetHeight, config.stanceTransitionSpeed * Time.deltaTime);
		currentEyeHeight = Mathf.MoveTowards(currentEyeHeight, targetEye, config.stanceTransitionSpeed * Time.deltaTime);

		ApplyControllerDimensions(currentControllerHeight);

		if (Mathf.Abs(currentControllerHeight - targetHeight) <= 0.001f)
		{
			currentStance = targetStance;
		}
	}

	private float GetStanceHeight(MovementStance stance)
	{
		switch (stance)
		{
			case MovementStance.Crouching:
				return config.crouchHeight;
			case MovementStance.Crawling:
				return config.crawlHeight;
			default:
				return config.standingHeight;
		}
	}

	private float GetStanceEyeHeight(MovementStance stance)
	{
		switch (stance)
		{
			case MovementStance.Crouching:
				return config.crouchEyeHeight;
			case MovementStance.Crawling:
				return config.crawlEyeHeight;
			default:
				return config.standingEyeHeight;
		}
	}

	private void ApplyControllerDimensions(float height)
	{
		float clampedHeight = Mathf.Max(height, characterController.radius * 2f);
		characterController.height = clampedHeight;

		Vector3 center = characterController.center;
		center.y = clampedHeight * 0.5f;
		characterController.center = center;
	}

	private bool CanFitHeight(float height)
	{
		if (characterController == null)
		{
			return false;
		}

		float checkInset = Mathf.Max(0.01f, characterController.skinWidth + 0.01f);
		float radius = Mathf.Max(0.01f, characterController.radius - checkInset);
		float clampedHeight = Mathf.Max(height, radius * 2f);
		Vector3 basePosition = transform.position + Vector3.up * checkInset;
		Vector3 bottom = basePosition + Vector3.up * radius;
		Vector3 top = basePosition + Vector3.up * (clampedHeight - radius);

		int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < hitCount; i++)
		{
			Collider collider = overlapBuffer[i];
			if (collider != null && !IsSelfOrChild(collider))
			{
				return false;
			}
		}

		return true;
	}

	private bool IsEdgeSafetyEnabled()
	{
		if (inputHandler != null && inputHandler.IsCrawling)
		{
			return false;
		}

		return currentStance == MovementStance.Crouching || targetStance == MovementStance.Crouching;
	}

	private void ConstrainCrouchEdgeMovement(ref Vector3 horizontalVelocity)
	{
		Vector3 planarMove = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z) * Time.deltaTime;
		if (planarMove.sqrMagnitude <= 0.000001f)
		{
			return;
		}

		Vector3 basePosition = transform.position;
		if (HasGroundSupport(basePosition + planarMove))
		{
			return;
		}

		bool xSupported = Mathf.Abs(planarMove.x) <= 0.000001f || HasGroundSupport(basePosition + new Vector3(planarMove.x, 0f, 0f));
		bool zSupported = Mathf.Abs(planarMove.z) <= 0.000001f || HasGroundSupport(basePosition + new Vector3(0f, 0f, planarMove.z));

		if (!xSupported)
		{
			horizontalVelocity.x = 0f;
		}

		if (!zSupported)
		{
			horizontalVelocity.z = 0f;
		}
	}

	private bool HasGroundSupport(Vector3 candidatePosition)
	{
		float sampleRadius = Mathf.Max(0.01f, characterController.radius - 0.02f);
		float rayLength = Mathf.Max(0.25f, characterController.stepOffset + 0.25f);

		for (int i = 0; i < supportProbeOffsets.Length; i++)
		{
			Vector3 origin = candidatePosition + supportProbeOffsets[i] * sampleRadius + Vector3.up * 0.05f;
			int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, raycastBuffer, rayLength, ~0, QueryTriggerInteraction.Ignore);
			bool supported = false;

			for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
			{
				Collider hitCollider = raycastBuffer[hitIndex].collider;
				if (hitCollider != null && !IsSelfOrChild(hitCollider))
				{
					supported = true;
					break;
				}
			}

			if (!supported)
			{
				return false;
			}
		}

		return true;
	}

	private bool IsSelfOrChild(Collider collider)
	{
		if (collider == null)
		{
			return false;
		}

		if (collider.transform == transform)
		{
			return true;
		}

		return collider.transform.IsChildOf(transform);
	}

	private static readonly Vector3[] supportProbeOffsets =
	{
		Vector3.zero,
		Vector3.forward,
		Vector3.back,
		Vector3.left,
		Vector3.right,
	};

	private static readonly Collider[] overlapBuffer = new Collider[16];
	private static readonly RaycastHit[] raycastBuffer = new RaycastHit[16];

	private void HandleMovementSounds(bool isGrounded, Vector2 moveInput, bool justLanded)
	{
		if (isFlying || !isGrounded)
		{
			footstepDistanceAccumulator = 0f;
			if (justLanded && config.landingClip != null)
			{
				PlayLandingSound(LandingImpactVelocity);
			}
			return;
		}

		if (justLanded && config.landingClip != null)
		{
			PlayLandingSound(LandingImpactVelocity);
		}

		if (moveInput.sqrMagnitude <= 0.01f || config.footstepDistance <= 0f)
		{
			footstepDistanceAccumulator = 0f;
			return;
		}

		footstepDistanceAccumulator += currentHorizontalVelocity.magnitude * Time.deltaTime;
		while (footstepDistanceAccumulator >= config.footstepDistance)
		{
			footstepDistanceAccumulator -= config.footstepDistance;
			PlayFootstepSound();
		}
	}

	private void PlayFootstepSound()
	{
		if (audioSource == null || config.footstepClips == null || config.footstepClips.Length == 0)
		{
			return;
		}

		AudioClip clip = config.footstepClips[Random.Range(0, config.footstepClips.Length)];
		if (clip == null)
		{
			return;
		}

		PlayClipWithPitchVariation(clip, 1f);
	}

	private void PlayJumpSound()
	{
		if (audioSource == null || config.jumpClip == null)
		{
			return;
		}

		PlayClipWithPitchVariation(config.jumpClip, 1f);
	}

	private void PlayLandingSound(float impactVelocity)
	{
		if (audioSource == null || config.landingClip == null)
		{
			return;
		}

		float landingVolume = Mathf.Lerp(0.35f, 0.9f, Mathf.Clamp01(impactVelocity / 12f));
		PlayClipWithPitchVariation(config.landingClip, landingVolume);
	}

	private void PlayClipWithPitchVariation(AudioClip clip, float volumeScale)
	{
		float originalPitch = audioSource.pitch;
		if (config.pitchVariation > 0f)
		{
			audioSource.pitch = 1f + Random.Range(-config.pitchVariation, config.pitchVariation);
		}

		audioSource.PlayOneShot(clip, volumeScale);
		audioSource.pitch = originalPitch;
	}

	private void UpdateAnimatorParameters(Vector2 moveInput, bool isGrounded = true)
	{
		if (animator == null) return;

		// Speed: use normalized horizontal input magnitude (0..1) for blending
		float normalizedSpeed = Mathf.Clamp01(moveInput.magnitude);
		animator.SetFloat(speedHash, normalizedSpeed);

		if (config.debugAnimator)
		{
			Debug.Log($"Animator Params -> Speed: {normalizedSpeed}, IsGrounded: {isGrounded}, IsFlying: {isFlying}, IsSprinting: {inputHandler.IsSprinting}");
		}

		animator.SetBool(isGroundedHash, isGrounded);
		animator.SetBool(isSprintingHash, inputHandler.IsSprinting);
		animator.SetBool(isFlyingHash, isFlying);
	}
}
