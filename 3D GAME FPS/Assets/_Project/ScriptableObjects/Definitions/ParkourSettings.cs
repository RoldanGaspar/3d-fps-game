using UnityEngine;

/// <summary>
/// ScriptableObject for tunable parkour parameters.
/// Create instances via: Create > Game > Parkour Settings
/// </summary>
[CreateAssetMenu(fileName = "New ParkourSettings", menuName = "Game/Parkour Settings")]
public class ParkourSettings : ScriptableObject
{
    [Header("Wall Run")]
    public float wallRunSpeed = 8f;
    public float wallRunDuration = 1.5f;
    public float wallRunGravity = -2f;
    public float wallRunCameraTilt = 15f;

    [Header("Wall Jump")]
    public float wallJumpForce = 7f;
    public float wallJumpSideForce = 5f;

    [Header("Slide")]
    public float slideSpeed = 10f;
    public float slideDuration = 0.8f;
    public float slideCooldown = 1f;
    public float slideColliderHeight = 0.5f;

    [Header("Vault")]
    public float vaultDuration = 0.4f;
    public float vaultMaxHeight = 1.2f;

    [Header("Mantle")]
    public float mantleDuration = 0.6f;
    public float mantleMaxHeight = 2.5f;

    [Header("Double Jump")]
    public float doubleJumpForce = 4f;
    public int maxAirJumps = 1;

    [Header("General")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchSpeed = 3f;
}