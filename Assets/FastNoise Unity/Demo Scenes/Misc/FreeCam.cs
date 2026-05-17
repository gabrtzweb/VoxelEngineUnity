using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FreeCam : MonoBehaviour
{
	public float moveSpeed = 12.0f;
	public float shiftSpeedModifier = 3.0f;
	public float lookSensitivty = 0.8f;
	public float verticalLookMinMax = 80f;

	Vector3 lastMouse = new Vector3();

	// Update is called once per frame
	void Update()
	{
		#if ENABLE_LEGACY_INPUT_MANAGER
		UpdateRotation();
		UpdateMovement();
		#elif ENABLE_INPUT_SYSTEM
		UpdateRotation_NewInput();
		UpdateMovement_NewInput();
		#else
		// No input system available. Enable Input System or Legacy Input Manager in Project Settings.
		#endif
	}

#if ENABLE_INPUT_SYSTEM
	void UpdateRotation_NewInput()
	{
		if (Mouse.current == null)
			return;

		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			lastMouse = Mouse.current.position.ReadValue();
		}

		if (!Mouse.current.leftButton.isPressed)
			return;

		Vector3 newRotation = transform.localEulerAngles;
		Vector2 mousePos = Mouse.current.position.ReadValue();
		newRotation.x += (lastMouse.y - mousePos.y) * lookSensitivty;
		newRotation.y += (mousePos.x - lastMouse.x) * lookSensitivty;

		if (newRotation.x > 180f)
			newRotation.x -= 360f;

		newRotation.x = Mathf.Clamp(newRotation.x, -verticalLookMinMax, verticalLookMinMax);

		transform.localEulerAngles = newRotation;

		lastMouse = mousePos;
	}

	void UpdateMovement_NewInput()
	{
		if (Keyboard.current == null)
			return;

		float modifiedSpeed = moveSpeed;

		if (Keyboard.current.leftShiftKey.isPressed)
			modifiedSpeed *= shiftSpeedModifier;

		float h = 0f;
		float v = 0f;
		if (Keyboard.current.aKey.isPressed) h -= 1f;
		if (Keyboard.current.dKey.isPressed) h += 1f;
		if (Keyboard.current.sKey.isPressed) v -= 1f;
		if (Keyboard.current.wKey.isPressed) v += 1f;

		Vector3 movement = new Vector3(h, 0, v);

		movement = transform.rotation * movement;

		if (Keyboard.current.qKey.isPressed)
			movement.y = -1f;
		else if (Keyboard.current.eKey.isPressed)
			movement.y = 1f;

		movement *= modifiedSpeed * Time.deltaTime;
		transform.Translate(movement, Space.World);
	}
#endif

	void UpdateRotation()
	{
		if (Input.GetMouseButtonDown(0))
		{
			lastMouse = Input.mousePosition;
		}

		if (!Input.GetMouseButton(0))
			return;

		Vector3 newRotation = transform.localEulerAngles;
		newRotation.x += (lastMouse.y - Input.mousePosition.y) * lookSensitivty;
		newRotation.y += (Input.mousePosition.x - lastMouse.x) * lookSensitivty;

		if (newRotation.x > 180f)
			newRotation.x -= 360f;

		newRotation.x = Mathf.Clamp(newRotation.x, -verticalLookMinMax, verticalLookMinMax);

		transform.localEulerAngles = newRotation;

		lastMouse = Input.mousePosition;
	}

	void UpdateMovement()
	{
		float modifiedSpeed = moveSpeed;

		if (Input.GetKey(KeyCode.LeftShift))
			modifiedSpeed *= shiftSpeedModifier;

		Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

		movement = transform.rotation * movement;

		if (Input.GetKey(KeyCode.Q))
			movement.y = -1f;
		else if (Input.GetKey(KeyCode.E))
			movement.y = 1f;

		movement *= modifiedSpeed * Time.deltaTime;
		transform.Translate(movement, Space.World);
	}
}
