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
    
    public ObjectiveState Status = ObjectiveState.Wait;

    public float Timeout;

    public override string ToString()
    {
        return $"SquadObjective({Location}, {Status}, timeout: {Timeout - Time.time})";
    }
}