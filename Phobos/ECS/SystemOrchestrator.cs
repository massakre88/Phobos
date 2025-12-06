using Phobos.Diag;
using Phobos.ECS.Entities;
using Phobos.ECS.Systems;
using Phobos.Navigation;
using Phobos.Objectives;

namespace Phobos.ECS;

// ReSharper disable MemberCanBePrivate.Global
// Every AI project needs a shitty, llm generated component name like "orcehstrator".
public class SystemOrchestrator
{
    public readonly MovementSystem MovementSystem;
    public readonly ActorTaskSystem ActorTaskSystem;
    public readonly SquadOrchestrator SquadOrchestrator;
    public readonly ActorList LiveActors;

    public SystemOrchestrator(NavJobExecutor navJobExecutor, ObjectiveQueue objectiveQueue)
    {
        LiveActors = new ActorList(32);
        
        DebugLog.Write("Creating MovementSystem");
        MovementSystem = new MovementSystem(navJobExecutor, LiveActors);
        DebugLog.Write("Creating ActorTaskSystem");
        ActorTaskSystem = new ActorTaskSystem(MovementSystem, LiveActors);
        DebugLog.Write("Creating SquadOrchestrator");
        SquadOrchestrator = new SquadOrchestrator(ActorTaskSystem, objectiveQueue);
    }

    public void AddActor(Actor actor)
    {
        DebugLog.Write($"Adding {actor} to Phobos systems");
        LiveActors.Add(actor);
        SquadOrchestrator.AddActor(actor);
    }

    public void RemoveActor(Actor actor)
    {
        LiveActors.SwapRemove(actor);
        SquadOrchestrator.RemoveActor(actor);
    }

    public void Update()
    {
        SquadOrchestrator.Update();
        ActorTaskSystem.Update();
        MovementSystem.Update();
    }
}