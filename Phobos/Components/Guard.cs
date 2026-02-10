using Phobos.Navigation;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Phobos.Components;

public struct OverwatchJob
{
    public JobHandle Handle;
    public NativeArray<RaycastCommand> Commands;
    public NativeArray<RaycastHit> Hits;
}

public enum GuardState
{
    None,
    Moving,
    Watching
}

public class Guard
{
    public CoverPoint? CoverPoint;
    public OverwatchJob? OverwatchJob;
    public Vector3? WatchDirection;
    public float SweepAngle;
    public float WatchTimeout;

    public override string ToString()
    {
        return $"{nameof(Guard)}({CoverPoint})";
    }
}