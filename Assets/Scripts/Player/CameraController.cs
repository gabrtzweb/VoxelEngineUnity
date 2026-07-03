using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	[SerializeField] private Transform cameraPivot;
	[SerializeField] private Transform characterVisualRoot;
	[SerializeField] private CinemachineCamera firstPersonCamera;
	[SerializeField] private CinemachineCamera thirdPersonCamera;
	[SerializeField] private CameraConfig config;
	private CinemachineBasicMultiChannelPerlin firstPersonPerlin;
	private CinemachineBasicMultiChannelPerlin thirdPersonPerlin;

	private float idlePerlinAmplitude;
	private float movingPerlinAmplitude;
	private float perlinBlendSpeed;
	private float lookSensitivity;
	private float minPitch;
	private float maxPitch;
	private bool startInFirstPerson;
	private int activeCameraPriority;
	private int inactiveCameraPriority;
	private float normalFov;
	private float sprintFov;
	private float fovLerpSpeed;
	private float firstPersonNearClipPlane;
	private float walkBobFrequency;
	private float walkBobAmplitude;
	private float sprintBobFrequency;
	private float sprintBobAmplitude;
	private float landingBobDistance;
	private float landingBobRecoverySpeed;
	private float bobBlendSpeed;
	private float landingFallVelocityScale;
	private float sprintSwayAngle;
	private float swaySmoothSpeed;
	private float moveInputThresholdSqr;

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
		if (config == null)
		{
			Debug.LogError($"{nameof(CameraController)} requires a {nameof(CameraConfig)} asset assigned.", this);
			enabled = false;
			return;
		}

		inputHandler = GetComponent<InputHandler>();
		CacheConfigValues();

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

		if (firstPersonCamera != null)
		{
			firstPersonPerlin =
				firstPersonCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
		}

		if (thirdPersonCamera != null)
		{
			thirdPersonPerlin =
				thirdPersonCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
		}

		CacheCharacterVisuals();

		PlayerMovement movement = GetComponent<PlayerMovement>();
		if (movement != null)
		{
			moveInputThresholdSqr = movement.MoveInputThresholdSqr;
		}

		isFirstPerson = startInFirstPerson;
		wasGrounded = IsBobGrounded();
		ApplyPerspective();
	}

	private void CacheConfigValues()
	{
		idlePerlinAmplitude = config.idlePerlinAmplitude;
		movingPerlinAmplitude = config.movingPerlinAmplitude;
		perlinBlendSpeed = config.perlinBlendSpeed;
		lookSensitivity = config.lookSensitivity;
		minPitch = config.minPitch;
		maxPitch = config.maxPitch;
		startInFirstPerson = config.startInFirstPerson;
		activeCameraPriority = config.activeCameraPriority;
		inactiveCameraPriority = config.inactiveCameraPriority;
		normalFov = config.normalFov;
		sprintFov = config.sprintFov;
		fovLerpSpeed = config.fovLerpSpeed;
		firstPersonNearClipPlane = config.firstPersonNearClipPlane;
		walkBobFrequency = config.walkBobFrequency;
		walkBobAmplitude = config.walkBobAmplitude;
		sprintBobFrequency = config.sprintBobFrequency;
		sprintBobAmplitude = config.sprintBobAmplitude;
		landingBobDistance = config.landingBobDistance;
		landingBobRecoverySpeed = config.landingBobRecoverySpeed;
		bobBlendSpeed = config.bobBlendSpeed;
		landingFallVelocityScale = config.landingFallVelocityScale;
		sprintSwayAngle = config.sprintSwayAngle;
		swaySmoothSpeed = config.swaySmoothSpeed;
	}

	private void OnEnable()
	{
		LockCursor();
	}

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
		UpdateCameraHeightFromStance();
		UpdateSprintSway();
		UpdatePerlinNoise();
		ApplyCameraPivotRotation();
		UpdateCameraNearClip();

		if (firstPersonCamera != null)
		{
			float targetFov = inputHandler.IsSprinting ? sprintFov : normalFov;

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

	private void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void ApplyPerspective()
	{
		SetCharacterVisualVisible(!isFirstPerson);
		UpdateCameraHeightFromStance();
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

	private void UpdateCameraHeightFromStance()
	{
		if (cameraPivot == null || inputHandler == null || inputHandler.Movement == null)
		{
			return;
		}

		cameraPivotBaseLocalPosition = new Vector3(
			cameraPivotBaseLocalPosition.x,
			inputHandler.Movement.CameraEyeHeight,
			cameraPivotBaseLocalPosition.z);
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
				inputHandler.MoveInput.sqrMagnitude > moveInputThresholdSqr;

			if (shouldSway)
			{
				targetSwayAngle = sprintSwayAngle * Mathf.Sin(Time.time * 2.2f);
			}
		}

		currentSwayAngle = Mathf.MoveTowards(currentSwayAngle, targetSwayAngle, swaySmoothSpeed * Time.deltaTime);
	}

	private void UpdatePerlinNoise()
	{
		bool isMoving =
			inputHandler != null &&
			inputHandler.MoveInput.sqrMagnitude > moveInputThresholdSqr;

		float targetAmplitude = isMoving ? movingPerlinAmplitude : idlePerlinAmplitude;

		if (firstPersonPerlin != null)
		{
			firstPersonPerlin.AmplitudeGain =
				Mathf.Lerp(
					firstPersonPerlin.AmplitudeGain,
					targetAmplitude,
					perlinBlendSpeed * Time.deltaTime);
		}

		if (thirdPersonPerlin != null)
		{
			thirdPersonPerlin.AmplitudeGain =
				Mathf.Lerp(
					thirdPersonPerlin.AmplitudeGain,
					targetAmplitude,
					perlinBlendSpeed * Time.deltaTime);
		}
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
		bool hasMoveInput = moveInput.sqrMagnitude > moveInputThresholdSqr;

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
