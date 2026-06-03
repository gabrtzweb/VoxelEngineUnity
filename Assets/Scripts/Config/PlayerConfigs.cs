using UnityEngine;

[CreateAssetMenu(fileName = "MovementConfig", menuName = "Voxel Engine/Player/Movement Config")]
public class MovementConfig : ScriptableObject
{
	[Header("Movement Speeds")]
	public float walkSpeed = 4.317f;
	public float sprintSpeed = 5.612f;

	[Header("Jump and Gravity")]
	public float jumpHeight = 1.252f;
	public float gravity = -20f;
	public float groundAcceleration = 35f;
	public float groundDeceleration = 40f;
	public float airAcceleration = 12f;

	[Header("Stance Settings")]
	public float standingHeight = 1.8f;
	public float standingEyeHeight = 1.62f;
	public float crouchHeight = 1.5f;
	public float crouchEyeHeight = 1.27f;
	public float crouchSpeed = 1.295f;
	public float crawlHeight = 0.6f;
	public float crawlEyeHeight = 0.42f;
	public float crawlSpeed = 0.85f;
	public float stanceTransitionSpeed = 10f;

	[Header("Movement Sounds")]
	public AudioClip[] footstepClips;
	public AudioClip jumpClip;
	public AudioClip landingClip;
	public float footstepDistance = 1.75f;
	public float pitchVariation = 0.05f;

	[Header("Flight Mode")]
	public float flightSpeed = 10.92f;
	public float flightSprintMultiplier = 2f;
	public float flightVerticalSpeed = 10.92f;
	public float doubleTapJumpWindow = 0.28f;

	[Header("Debug")]
	public bool debugAnimator;
}

[CreateAssetMenu(fileName = "CameraConfig", menuName = "Voxel Engine/Player/Camera Config")]
public class CameraConfig : ScriptableObject
{
	[Header("Perlin Noise")]
	public float idlePerlinAmplitude = 0.5f;
	public float movingPerlinAmplitude = 0.08f;
	public float perlinBlendSpeed = 8f;

	[Header("Look and Perspective")]
	public float lookSensitivity = 0.4f;
	public float minPitch = -89f;
	public float maxPitch = 89f;
	public bool startInFirstPerson = true;
	public int activeCameraPriority = 20;
	public int inactiveCameraPriority = 0;

	[Header("Field of View")]
	public float normalFov = 70f;
	public float sprintFov = 84f;
	public float fovLerpSpeed = 10f;
	public float firstPersonNearClipPlane = 0.03f;

	[Header("Camera Bob")]
	public float walkBobFrequency = 8f;
	public float walkBobAmplitude = 0.02f;
	public float sprintBobFrequency = 12f;
	public float sprintBobAmplitude = 0.03f;
	public float landingBobDistance = 0.04f;
	public float landingBobRecoverySpeed = 10f;
	public float bobBlendSpeed = 12f;
	public float landingFallVelocityScale = 8f;

	[Header("Input and Sway")]
	public float moveInputThreshold = 0.01f;
	public float sprintSwayAngle = 0.5f;
	public float swaySmoothSpeed = 10f;
}

[CreateAssetMenu(fileName = "InteractionConfig", menuName = "Voxel Engine/Player/Interaction Config")]
public class InteractionConfig : ScriptableObject
{
	public float interactionRange = 4.5f;
	public float hitEpsilon = 0.05f;
	public LayerMask interactionMask = ~0;
	public bool allowPickWater = true;
	public float holdRepeatDelay = 0.2f;
	public float holdRepeatInterval = 0.2f;
}
