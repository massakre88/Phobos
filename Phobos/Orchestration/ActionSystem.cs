using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Comfort.Common;
using Phobos.Data;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Tasks.Actions;

namespace Phobos.Orchestration;

public class ActionSystem(AgentData dataset)
{
    private readonly List<BaseAction> _actions = [];

    public void RemoveAgent(Agent agent)
    {
        DebugLog.Write($"Removing {agent} from all actions");
        for (var i = 0; i < _actions.Count; i++)
        {
            var action = _actions[i];
            action.Deactivate(agent);
        }
    }

    public void RegisterAction(BaseAction action)
    {
        _actions.Add(action);
    }

    public void Update()
    {
        UpdateUtilities();
        PickActions();
        UpdateActions();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateUtilities()
    {
        for (var i = 0; i < _actions.Count; i++)
        {
            _actions[i].UpdateUtility();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PickActions()
    {
        var agents = dataset.Entities.Values;

        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            
            if (!agent.IsActive)
            {
                if (agent.CurrentAction != null)
                {
                    agent.CurrentAction.Deactivate(agent);
                    agent.CurrentAction = null;
                }
                
                continue;
            }

            var nextAction = NextAgentAction(agent);

            if (agent.CurrentAction == nextAction || nextAction == null) continue;

            agent.CurrentAction?.Deactivate(agent);
            nextAction.Activate(agent);
            agent.CurrentAction = nextAction;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BaseAction NextAgentAction(Agent agent)
    {
        var highestScore = -1f;
        BaseAction nextAction = null;

        for (var j = 0; j < agent.Actions.Count; j++)
        {
            var entry = agent.Actions[j];
            var score = entry.Score;

            if (entry.Action == agent.CurrentAction)
            {
                score += entry.Action.Hysteresis;
            }

            if (score <= highestScore) continue;

            highestScore = score;
            nextAction = entry.Action;
        }

        Singleton<Telemetry>.Instance.UpdateScores(agent);

        agent.Actions.Clear();
        return nextAction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateActions()
    {
        for (var i = 0; i < _actions.Count; i++)
        {
            var action = _actions[i];
            action.Update();
        }
    }
}