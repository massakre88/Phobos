using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Helpers;
using Phobos.Tasks.Strategies;

namespace Phobos.Entities;


public class Squad(int id) : Entity(id)
{
    public readonly List<Agent> Members = new(6);
    
    public readonly List<StrategyScore> Strategies = new(16);
    public BaseStrategy CurrentStrategy;
    
    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Members.Count;
    }

    public void AddAgent(Agent member)
    {
        Members.Add(member);
    }

    public void RemoveAgent(Agent member)
    {
        Members.SwapRemove(member);
    }
    
    public override string ToString()
    {
        return $"Squad(id: {Id})";
    }
}