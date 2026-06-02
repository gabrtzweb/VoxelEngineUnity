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

	// Movement Speeds
	[SerializeField] private float walkSpeed = 4.317f;
	[SerializeField] private float sprintSpeed = 5.612f;

	// Jump and Gravity
	[SerializeField] private float jumpHeight = 1.252f;
	[SerializeField] private float gravity = -20f;
	[SerializeField] private float groundAcceleration = 35f;
	[SerializeField] private float groundDeceleration = 40f;
	[SerializeField] private float airAcceleration = 12f;

	// Stance Settings
	[SerializeField] private float standingHeight = 1.8f;
	[SerializeField] private float standingEyeHeight = 1.62f;
	[SerializeField] private float crouchHeight = 1.5f;
	[SerializeField] private float crouchEyeHeight = 1.27f;
	[SerializeField] private float crouchSpeed = 1.295f;
	[SerializeField] private float crawlHeight = 0.6f;
	[SerializeField] private float crawlEyeHeight = 0.42f;
	[SerializeField] private float crawlSpeed = 0.85f;
	[SerializeField] private float stanceTransitionSpeed = 10f;

	// Movement Sounds
	[SerializeField] private AudioClip[] footstepClips;
	[SerializeField] private AudioClip jumpClip;
	[SerializeField] private AudioClip landingClip;
	[SerializeField] private float footstepDistance = 1.75f;
	[SerializeField] private float pitchVariation = 0.05f;

	private Vector3 currentHorizontalVelocity;
	private float footstepDistanceAccumulator;
	private MovementStance currentStance = MovementStance.Standing;
	private MovementStance targetStance = MovementStance.Standing;
	private float currentControllerHeight;
	private float currentEyeHeight;

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

		currentControllerHeight = standingHeight > 0f ? standingHeight : characterController.height;
		currentEyeHeight = standingEyeHeight;
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
			if (animator == null && debugAnimator)
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

		if (isGrounded && IsEdgeSafetyEnabled() && currentHorizontalVelocity.sqrMagnitude > 0f)
		{
			ConstrainCrouchEdgeMovement(ref currentHorizontalVelocity);
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
		switch (targetStance)
		{
			case MovementStance.Crouching:
				return crouchSpeed;
			case MovementStance.Crawling:
				return crawlSpeed;
			default:
				return inputHandler.IsSprinting ? sprintSpeed : walkSpeed;
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
				if (CanFitHeight(crawlHeight))
				{
					return MovementStance.Crawling;
				}

				if (CanFitHeight(crouchHeight))
				{
					return MovementStance.Crouching;
				}

				return targetStance;
			case MovementStance.Crouching:
				if (CanFitHeight(crouchHeight))
				{
					return MovementStance.Crouching;
				}

				if (CanFitHeight(crawlHeight))
				{
					return MovementStance.Crawling;
				}

				return targetStance == MovementStance.Crawling ? MovementStance.Crawling : MovementStance.Standing;
			default:
				if (CanFitHeight(standingHeight))
				{
					return MovementStance.Standing;
				}

				if (CanFitHeight(crouchHeight))
				{
					return MovementStance.Crouching;
				}

				if (CanFitHeight(crawlHeight))
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

		currentControllerHeight = Mathf.MoveTowards(currentControllerHeight, targetHeight, stanceTransitionSpeed * Time.deltaTime);
		currentEyeHeight = Mathf.MoveTowards(currentEyeHeight, targetEye, stanceTransitionSpeed * Time.deltaTime);

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
				return crouchHeight;
			case MovementStance.Crawling:
				return crawlHeight;
			default:
				return standingHeight;
		}
	}

	private float GetStanceEyeHeight(MovementStance stance)
	{
		switch (stance)
		{
			case MovementStance.Crouching:
				return crouchEyeHeight;
			case MovementStance.Crawling:
				return crawlEyeHeight;
			default:
				return standingEyeHeight;
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
