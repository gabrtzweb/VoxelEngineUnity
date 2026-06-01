using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	// Rig References
	[SerializeField] private Transform cameraPivot;
	[SerializeField] private Transform yawRoot;
	[SerializeField] private CinemachineCamera firstPersonCamera;
	[SerializeField] private CinemachineCamera thirdPersonCamera;

	// Camera Tuning
	[SerializeField] private float lookSensitivity = 1.5f;
	[SerializeField] private float minPitch = -89f;
	[SerializeField] private float maxPitch = 89f;
	[SerializeField] private bool startInFirstPerson = true;
	[SerializeField] private int activeCameraPriority = 20;
	[SerializeField] private int inactiveCameraPriority = 0;
	[SerializeField] private float normalFov = 70f;
	[SerializeField] private float sprintFov = 84f;
	[SerializeField] private float fovLerpSpeed = 10f;

	// Runtime State
	private InputHandler inputHandler;
	private float pitch;
	private bool isFirstPerson;

	private void Awake()
	{
		inputHandler = GetComponent<InputHandler>();

		if (yawRoot == null)
		{
			yawRoot = transform;
		}

		if (cameraPivot == null)
		{
			cameraPivot = transform.Find("CameraPivot");
		}

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

		yawRoot.Rotate(Vector3.up * lookInput.x, Space.World);

		pitch = Mathf.Clamp(pitch - lookInput.y, minPitch, maxPitch);
		cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

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
		if (firstPersonCamera != null)
		{
			firstPersonCamera.Priority = isFirstPerson ? activeCameraPriority : inactiveCameraPriority;
		}

		if (thirdPersonCamera != null)
		{
			thirdPersonCamera.Priority = isFirstPerson ? inactiveCameraPriority : activeCameraPriority;
		}

		pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
		cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
	}
}
