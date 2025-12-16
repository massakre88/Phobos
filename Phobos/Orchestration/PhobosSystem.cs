using EFT;
using Phobos.Data;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Navigation;

namespace Phobos.Orchestration;

public class PhobosSystem
{
    public readonly Dataset Dataset;
    
    public readonly ActionSystem ActionSystem;
    public readonly SquadSystem SquadSystem;
    
    private readonly Telemetry _telemetry;

    public PhobosSystem(Telemetry telemetry)
    {
        var objectiveQueue = new LocationQueue();
        
        Dataset = new Dataset();
        ActionSystem = new ActionSystem(Dataset);
        SquadSystem =  new SquadSystem(objectiveQueue);
        
        _telemetry = telemetry;
    }

    public void RegisterComponents()
    {
        // Register components with the Dataset
    }

    public void RegisterActions()
    {
        // Register actions
    }
    
    public Agent AddAgent(BotOwner bot)
    {
        var agent = Dataset.AddAgent(bot);
        DebugLog.Write($"Adding {agent} to Phobos");
        SquadSystem.AddAgent(agent);
        _telemetry.AddAgent(agent);
        return agent;
    }

    public void RemoveAgent(Agent agent)
    {
        DebugLog.Write($"Removing {agent} from Phobos");
        Dataset.RemoveAgent(agent);
        ActionSystem.RemoveAgent(agent);
        SquadSystem.RemoveAgent(agent);
        _telemetry.RemoveAgent(agent);
    }

    public void Update()
    {
        ActionSystem.Update();
        SquadSystem.Update();
    }
}