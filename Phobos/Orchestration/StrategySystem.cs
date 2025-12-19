using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Comfort.Common;
using Phobos.Data;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Helpers;
using Phobos.Tasks.Strategies;

namespace Phobos.Orchestration;

public class StrategySystem(SquadData dataset)
{
    private readonly TimePacing _pacing = new(0.5f);
    private readonly List<BaseStrategy> _strategies = [];

    public void RemoveSquad(Squad squad)
    {
        DebugLog.Write($"Removing {squad} from all strategies");
        for (var i = 0; i < _strategies.Count; i++)
        {
            var strategy = _strategies[i];
            strategy.Deactivate(squad);
        }
    }

    public void RegisterStrategy(BaseStrategy strategy)
    {
        _strategies.Add(strategy);
    }

    public void Update()
    {
        if (_pacing.Blocked())
            return;

        UpdateUtilities();
        PickStrategies();
        UpdateStrategies();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateUtilities()
    {
        for (var i = 0; i < _strategies.Count; i++)
        {
            _strategies[i].UpdateUtility();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PickStrategies()
    {
        var squads = dataset.Entities.Values;

        for (var i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];

            if (squad.Count == 0)
            {
                if (squad.CurrentStrategy != null)
                {
                    squad.CurrentStrategy.Deactivate(squad);
                    squad.CurrentStrategy = null;
                }

                continue;
            }

            var nextStrategy = NextSquadStrategy(squad);

            if (squad.CurrentStrategy == nextStrategy || nextStrategy == null) continue;

            squad.CurrentStrategy?.Deactivate(squad);
            nextStrategy.Activate(squad);
            squad.CurrentStrategy = nextStrategy;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BaseStrategy NextSquadStrategy(Squad squad)
    {
        var highestScore = -1f;
        
        BaseStrategy nextStrategy = null;

        for (var j = 0; j < squad.Strategies.Count; j++)
        {
            var entry = squad.Strategies[j];
            var score = entry.Score;

            if (score <= highestScore) continue;

            highestScore = score;
            nextStrategy = entry.Strategy;
        }

        Singleton<Telemetry>.Instance.UpdateScores(squad);

        squad.Strategies.Clear();
        return nextStrategy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateStrategies()
    {
        for (var i = 0; i < _strategies.Count; i++)
        {
            var strategy = _strategies[i];
            strategy.Update();
        }
    }
}