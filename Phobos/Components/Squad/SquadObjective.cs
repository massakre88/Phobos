using System.Collections.Generic;
using Phobos.Navigation;
using UnityEngine;

namespace Phobos.Components.Squad;

public enum ObjectiveState
{
    Active,
    Wait
}

public class SquadObjective
{
    public Location Location;
    public Location LocationPrevious;
    public readonly List<CoverPoint> CoverPoints = [];
    
    public ObjectiveState Status = ObjectiveState.Wait;

    public float StartTime;
    public float Duration;
    public bool DurationAdjusted;

    public override string ToString()
    {
        return $"SquadObjective({Location}, {Status}, timeout: {Time.time - StartTime} / {Duration})";
    }
}