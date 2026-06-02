using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	// Rig References
	[SerializeField] private Transform cameraPivot;
	[SerializeField] private Transform characterVisualRoot;
	[SerializeField] private CinemachineCamera firstPersonCamera;
	[SerializeField] private CinemachineCamera thirdPersonCamera;

	// Camera Tuning
	[SerializeField] private float lookSensitivity = 0.4f;
	[SerializeField] private float minPitch = -89f;
	[SerializeField] private float maxPitch = 89f;
	[SerializeField] private bool startInFirstPerson = true;
	[SerializeField] private int activeCameraPriority = 20;
	[SerializeField] private int inactiveCameraPriority = 0;
	[SerializeField] private float normalFov = 70f;
	[SerializeField] private float sprintFov = 84f;
	[SerializeField] private float fovLerpSpeed = 10f;
	[SerializeField] private float firstPersonNearClipPlane = 0.03f;
	[SerializeField] private float walkBobFrequency = 8f;
	[SerializeField] private float walkBobAmplitude = 0.04f;
	[SerializeField] private float sprintBobFrequency = 12f;
	[SerializeField] private float sprintBobAmplitude = 0.06f;
	[SerializeField] private float landingBobDistance = 0.08f;
	[SerializeField] private float landingBobRecoverySpeed = 10f;
	[SerializeField] private float bobBlendSpeed = 12f;
	[SerializeField] private float landingFallVelocityScale = 8f;
	[SerializeField] private float moveInputThreshold = 0.01f;
	[SerializeField] private float sprintSwayAngle = 1.5f;
	[SerializeField] private float swaySmoothSpeed = 10f;

	// Runtime State
	private InputHandler inputHandler;
	private Renderer[] characterRenderers;
	private bool[] characterRendererInitialStates;
	private Camera outputCamera;
	private float defaultNearClipPlane;
	private float defaultFirstPersonNearClipPlane;
	private Vector3 cameraPivotBaseLocalPosition;
	private Vector3 currentCameraPivotOffset;
	private float bobTimer;
	private float landingBobOffset;
	private float currentSwayAngle;
	private bool wasGrounded;
	private float pitch;
	private bool isFirstPerson;

	private void Awake()
	{
		inputHandler = GetComponent<InputHandler>();

		if (cameraPivot == null)
		{
			cameraPivot = transform.Find("CameraPivot");
		}

		if (cameraPivot != null)
		{
			cameraPivotBaseLocalPosition = cameraPivot.localPosition;
		}

		outputCamera = Camera.main;
		if (outputCamera != null)
		{
			defaultNearClipPlane = outputCamera.nearClipPlane;
		}

		if (firstPersonCamera != null)
		{
			defaultFirstPersonNearClipPlane = firstPersonCamera.Lens.NearClipPlane;
		}

		CacheCharacterVisuals();

		isFirstPerson = startInFirstPerson;
		wasGrounded = IsBobGrounded();
		ApplyPerspective();
	}

	private void OnEnable()
	{
		LockCursor();
	}

	// Camera Update
	private void Update()
	{
		if (cameraPivot == null)
		{
			return;
		}

		if (inputHandler.TogglePerspectivePressed)
		{
			isFirstPerson = !isFirstPerson;
			ApplyPerspective();
		}

		Vector2 lookInput = inputHandler.LookInput * lookSensitivity;

		transform.Rotate(Vector3.up * lookInput.x, Space.World);

		pitch = Mathf.Clamp(pitch - lookInput.y, minPitch, maxPitch);
		UpdateSprintSway();
		ApplyCameraPivotRotation();
		UpdateCameraNearClip();

		if (firstPersonCamera != null)
		{
			float targetFov =
				inputHandler.IsSprinting
					? sprintFov
					: normalFov;

			firstPersonCamera.Lens.FieldOfView =
				Mathf.Lerp(
					firstPersonCamera.Lens.FieldOfView,
					targetFov,
					fovLerpSpeed * Time.deltaTime);
		}
	}

	private void LateUpdate()
	{
		UpdateCameraBob();
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			LockCursor();
		}
	}

	// Cursor State
	private void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	// Perspective Switching
	private void ApplyPerspective()
	{
		SetCharacterVisualVisible(!isFirstPerson);
		UpdateCameraNearClip();
		ResetCameraBob();

		if (firstPersonCamera != null)
		{
			firstPersonCamera.Priority = isFirstPerson ? activeCameraPriority : inactiveCameraPriority;
		}

		if (thirdPersonCamera != null)
		{
			thirdPersonCamera.Priority = isFirstPerson ? inactiveCameraPriority : activeCameraPriority;
		}

		pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
		if (cameraPivot != null)
		{
			ApplyCameraPivotRotation();
			cameraPivot.localPosition = cameraPivotBaseLocalPosition;
		}
	}

	private void UpdateSprintSway()
	{
		float targetSwayAngle = 0f;

		if (isFirstPerson && inputHandler != null && inputHandler.Movement != null)
		{
			bool shouldSway =
				inputHandler.Movement.IsGrounded &&
				!inputHandler.Movement.IsFlying &&
				inputHandler.IsSprinting &&
				inputHandler.MoveInput.sqrMagnitude > moveInputThreshold;

			if (shouldSway)
			{
				targetSwayAngle = sprintSwayAngle * Mathf.Sin(Time.time * 2.2f);
			}
		}

		currentSwayAngle = Mathf.MoveTowards(currentSwayAngle, targetSwayAngle, swaySmoothSpeed * Time.deltaTime);
	}

	private void ApplyCameraPivotRotation()
	{
		if (cameraPivot == null)
		{
			return;
		}

		cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, currentSwayAngle);
	}

	private void UpdateCameraBob()
	{
		if (cameraPivot == null)
		{
			return;
		}

		if (!isFirstPerson)
		{
			ResetCameraBob();
			return;
		}

		Vector3 targetOffset = Vector3.zero;
		bool canBob = IsBobGrounded();
		Vector2 moveInput = inputHandler != null ? inputHandler.MoveInput : Vector2.zero;
		bool hasMoveInput = moveInput.sqrMagnitude > moveInputThreshold;

		if (canBob && hasMoveInput)
		{
			float bobFrequency = inputHandler.IsSprinting ? sprintBobFrequency : walkBobFrequency;
			float bobAmplitude = inputHandler.IsSprinting ? sprintBobAmplitude : walkBobAmplitude;
			bobTimer += Time.deltaTime * bobFrequency * (1f + moveInput.magnitude * 0.1f);

			float bobPhase = bobTimer * Mathf.PI * 2f;
			float horizontalBob = Mathf.Sin(bobPhase) * bobAmplitude * 0.5f;
			float verticalBob = Mathf.Abs(Mathf.Sin(bobPhase * 2f)) * bobAmplitude;
			targetOffset = new Vector3(horizontalBob, verticalBob, 0f);
		}

		if (canBob && !wasGrounded)
		{
			float landingStrength = Mathf.Clamp01(inputHandler.Movement.LandingImpactVelocity / landingFallVelocityScale);
			float landingAmount = landingBobDistance * Mathf.Lerp(0.5f, 1f, landingStrength);
			landingBobOffset = Mathf.Min(landingBobOffset, -landingAmount);
		}

		landingBobOffset = Mathf.MoveTowards(landingBobOffset, 0f, landingBobRecoverySpeed * Time.deltaTime);

		Vector3 desiredOffset = targetOffset + Vector3.up * landingBobOffset;
		currentCameraPivotOffset = Vector3.Lerp(currentCameraPivotOffset, desiredOffset, bobBlendSpeed * Time.deltaTime);
		cameraPivot.localPosition = cameraPivotBaseLocalPosition + currentCameraPivotOffset;

		wasGrounded = canBob;
	}

	private bool IsBobGrounded()
	{
		return inputHandler != null && inputHandler.Movement != null && inputHandler.Movement.IsGrounded && !inputHandler.Movement.IsFlying;
	}

	private void ResetCameraBob()
	{
		currentCameraPivotOffset = Vector3.Lerp(currentCameraPivotOffset, Vector3.zero, bobBlendSpeed * Time.deltaTime);
		landingBobOffset = Mathf.MoveTowards(landingBobOffset, 0f, landingBobRecoverySpeed * Time.deltaTime);
		if (cameraPivot != null)
		{
			cameraPivot.localPosition = cameraPivotBaseLocalPosition + currentCameraPivotOffset + Vector3.up * landingBobOffset;
		}

		wasGrounded = IsBobGrounded();
	}

	private void UpdateCameraNearClip()
	{
		if (firstPersonCamera != null)
		{
			firstPersonCamera.Lens.NearClipPlane = isFirstPerson
				? firstPersonNearClipPlane
				: defaultFirstPersonNearClipPlane;
		}

		if (outputCamera == null)
		{
			outputCamera = Camera.main;
			if (outputCamera == null)
			{
				return;
			}

			defaultNearClipPlane = outputCamera.nearClipPlane;
		}

		outputCamera.nearClipPlane = isFirstPerson
			? Mathf.Min(defaultNearClipPlane, firstPersonNearClipPlane)
			: defaultNearClipPlane;
	}

	private void CacheCharacterVisuals()
	{
		if (characterVisualRoot == null)
		{
			characterVisualRoot = FindVisualRoot();
		}

		if (characterVisualRoot == null)
		{
			return;
		}

		characterRenderers = characterVisualRoot.GetComponentsInChildren<Renderer>(true);
		characterRendererInitialStates = new bool[characterRenderers.Length];

		for (int i = 0; i < characterRenderers.Length; i++)
		{
			characterRendererInitialStates[i] = characterRenderers[i] != null && characterRenderers[i].enabled;
		}
	}

	private Transform FindVisualRoot()
	{
		Transform bestRoot = null;
		int bestRendererCount = 0;

		foreach (Transform child in transform)
		{
			if (child == cameraPivot)
			{
				continue;
			}

			int rendererCount = child.GetComponentsInChildren<Renderer>(true).Length;
			if (rendererCount > bestRendererCount)
			{
				bestRendererCount = rendererCount;
				bestRoot = child;
			}
		}

		return bestRoot;
	}

	private void SetCharacterVisualVisible(bool visible)
	{
		if (characterRenderers == null || characterRendererInitialStates == null)
		{
			return;
		}

		for (int i = 0; i < characterRenderers.Length; i++)
		{
			Renderer characterRenderer = characterRenderers[i];
			if (characterRenderer == null)
			{
				continue;
			}

			characterRenderer.enabled = visible && characterRendererInitialStates[i];
		}
	}
}
