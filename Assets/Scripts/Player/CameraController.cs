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

	// Runtime State
	private InputHandler inputHandler;
	private Renderer[] characterRenderers;
	private bool[] characterRendererInitialStates;
	private Camera outputCamera;
	private float defaultNearClipPlane;
	private float defaultFirstPersonNearClipPlane;
	private float pitch;
	private bool isFirstPerson;

	private void Awake()
	{
		inputHandler = GetComponent<InputHandler>();

		if (cameraPivot == null)
		{
			cameraPivot = transform.Find("CameraPivot");
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
		cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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
			cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
		}
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
