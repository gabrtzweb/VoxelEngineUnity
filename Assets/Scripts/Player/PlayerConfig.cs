using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "VoxelEngine/Player Configuration")]
public class PlayerConfig : ScriptableObject
{
    [Header("Physics Constants")]
    public float respawnHeightOffset = 16f;
    public float stanceTransitionSpeed = 10f;
    public float groundedGravityReset = -2f;
    public float edgeCheckDistance = 0.5f;
    public float edgeCheckYOffset = 0.1f;

    [Header("Movement Speeds")]
    public float walkSpeed = 4.8f;
    public float runSpeed = 6.8f;
    public float crouchSpeed = 1.4f;
    public float crawlSpeed = 0.8f;

    [Header("Water Settings")]
    public float swimSpeed = 3.5f;
    public float fastSwimSpeed = 6.8f;
    public float swimUpSpeed = 4f;
    public float swimDownSpeed = 4f;
    public float waterSinkingSpeed = -1.0f;
    public float verticalWaterDrag = 4f;

    [Header("Flight Settings")]
    public float flySpeed = 16f;
    public float fastFlySpeed = 32f;
    public float verticalFlySpeed = 8f;
    public float doubleTapWindow = 0.4f;

    [Header("Physics")]
    public float jumpHeight = 1.32f;
    public float gravity = -16f;

    [Header("Stance & Camera")]
    public float standingHeight = 1.8f;
    public float crouchingHeight = 1.4f;
    public float crawlingHeight = 0.8f;

    [Header("Interaction Settings")]
    public float reach = 6f;
    public float interactionCooldown = 0.16f;
    public float boundsExpansion = -0.05f;
    public float raycastStepSize = 0.05f;
}
