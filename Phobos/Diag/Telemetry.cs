using System.Collections.Generic;
using System.Diagnostics;
using Phobos.Actions;
using Phobos.Entities;
using Phobos.Tasks.Actions;

namespace Phobos.Diag;

public class AgentTelemetry
{
    public readonly List<ActionScore> Scores = [];
}

public class Telemetry
{
    private readonly Dictionary<Agent, AgentTelemetry> _agentTelemetry = new();

    public string GenerateUtilityReport()
    {
        return "";
    }

    public string GenerateAgentReport(Agent agent)
    {
        return "";
    }

    [Conditional("DEBUG")]
    public void UpdateScores(Agent agent)
    {
        var telemetry = _agentTelemetry[agent];
        telemetry.Scores.Clear();
        telemetry.Scores.AddRange(agent.Actions);
    }

    [Conditional("DEBUG")]
    public void AddAgent(Agent agent)
    {
        DebugLog.Write($"Adding {agent} to Telemetry");
        _agentTelemetry[agent] = new();
    }

    [Conditional("DEBUG")]
    public void RemoveAgent(Agent agent)
    {
        DebugLog.Write($"Removing {agent} from Telemetry");
        _agentTelemetry.Remove(agent);
    }
}