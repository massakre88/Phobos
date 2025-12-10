using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.ECS.Entities;

namespace Phobos.ECS.Systems.Objectives;

public abstract class BaseObjectiveSystem
{
    protected readonly AgentList Agents = new(16);
    private readonly HashSet<Agent> _actorSet = [];

    public abstract void ResetObjective(Agent agent);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AddAgent(Agent agent)
    {
        if (!_actorSet.Add(agent))
            return;

        Agents.Add(agent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAgent(Agent agent)
    {
        if (!_actorSet.Remove(agent))
            return;
        
        Agents.SwapRemove(agent);
    }
}