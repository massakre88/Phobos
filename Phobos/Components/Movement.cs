using System.Runtime.CompilerServices;
using Phobos.Helpers;
using UnityEngine;

namespace Phobos.Components;

public enum MovementStatus
{
    Stopped,
    Moving,
    Failed
}

public enum MovementUrgency
{
    High,
    Medium,
    Low
}

public class Movement
{
    public static readonly Vector3 Infinity = new(float.MaxValue, float.MaxValue, float.MaxValue);
    
    // Ensure that the null target is far away from everything
    public Vector3 Target = Infinity;
    public Vector3[] Path;
    public MovementStatus Status = MovementStatus.Stopped;

    public int CurrentCorner;
    public int Retry;

    public float Speed = 1f;
    public float Pose = 1f;
    public bool Sprint = false;
    public bool Prone = false;
    public MovementUrgency Urgency = MovementUrgency.Medium;

    public bool HasPath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Path is { Length: > 0 };
    }

    public readonly TimePacing VoxelUpdatePacing = new(0.25f);

    public override string ToString()
    {
        return
            $"Movement(Path: {CurrentCorner}/{Path?.Length}, Status: {Status} Retry: {Retry} Speed: {Speed}, Pose: {Pose} Sprint {Sprint}, Prone: {Prone})";
    }
}