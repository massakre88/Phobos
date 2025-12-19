using System.Collections.Generic;
using Phobos.Helpers;

namespace Phobos.Tasks;

public abstract class BaseTask<T>(float hysteresis)
{
    public readonly float Hysteresis = hysteresis;
    
    protected readonly List<T> ActiveEntities = new(16);
    private readonly HashSet<T> _agentSet = [];

    public abstract void UpdateUtility();
    public abstract void Update();

    public virtual void Activate(T entity)
    {
        if (!_agentSet.Add(entity))
            return;

        ActiveEntities.Add(entity);
    }

    public virtual void Deactivate(T entity)
    {
        if (!_agentSet.Remove(entity))
            return;

        ActiveEntities.SwapRemove(entity);
    }
}