using System.Collections.Generic;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Helpers;
using Phobos.Navigation;

namespace Phobos.Orchestration;

public class SquadSystem(LocationQueue locationQueue)
{
    private readonly TimePacing _pacing = new(1);

    private readonly List<Squad> _squads = new(16);
    private readonly List<Squad> _emptySquads = new(8);
    private readonly Dictionary<int, Squad> _squadIdMap = new(16);

    public Squad this[int squadId] => _squadIdMap[squadId];

    public void Update()
    {
        if (_pacing.Blocked())
            return;

        for (var i = 0; i < _squads.Count; i++)
        {
            var squad = _squads[i];

            if (squad.Objective != null) continue;

            // If the squad does not have an objective yet, grab one.
            var location = locationQueue.Next();
            squad.Objective = location;
            DebugLog.Write($"Assigned {location} to {squad}");
        }
    }

    public void AddAgent(Agent agent)
    {
        if (!_squadIdMap.TryGetValue(agent.SquadId, out var squad))
        {
            squad = new Squad(agent.SquadId);
            _squadIdMap.Add(agent.SquadId, squad);
            _squads.Add(squad);
            DebugLog.Write($"Registered new {squad}");
        }
        else if (_emptySquads.SwapRemove(squad))
        {
            // Move the empty squad back to the main list
            _squads.Add(squad);
            DebugLog.Write($"Re-activated previously inactive {squad}");
        }

        squad.AddAgent(agent);
        DebugLog.Write($"Added {agent} to {squad} with {squad.Count} members");
    }

    public void RemoveAgent(Agent agent)
    {
        if (!_squadIdMap.TryGetValue(agent.SquadId, out var squad)) return;

        squad.RemoveAgent(agent);
        DebugLog.Write($"Removed {agent} from {squad} with {squad.Count} members");

        if (squad.Count != 0) return;

        DebugLog.Write($"{squad} is empty and deactivated");

        _squads.SwapRemove(squad);
        _emptySquads.Add(squad);
    }
}