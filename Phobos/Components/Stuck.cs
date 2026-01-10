using Phobos.Helpers;
using UnityEngine;

namespace Phobos.Components;

public enum StuckState
{
    None,
    Vaulting,
    Jumping,
    Retrying,
    Teleport,
    Failed
}

public class Stuck
{
    // Thresholds
    public const float MaxMoveSpeed = 5f; // Maximum bot movement speed in m/s
    public const float StuckThresholdMultiplier = 0.5f; // Bot moving at less than 50% expected speed is stuck
    public const float VaultAttemptDelay = 1.5f;
    public const float JumpAttemptDelay = 1.5f + VaultAttemptDelay;
    public const float PathRetryDelay = 3f + JumpAttemptDelay;
    public const float TeleportDelay = 3f + PathRetryDelay;
    public const float FailedDelay = 3f + PathRetryDelay;

    public readonly TimePacing Pacing = new(0.1f);
    
    public StuckState State = StuckState.None;
    public Vector3 LastPosition;
    public float LastSpeed;
    public float LastUpdate; 
    public float Timer;
    
    public override string ToString()
    {
        return $"Stuck(State: {State} timer: {Timer} last speed: {LastSpeed} last position: {LastPosition})";
    }
}