using System.Collections.Generic;
using Phobos.ECS.Entities;
using Phobos.ECS.Systems;
using Phobos.Navigation;
using Phobos.Objectives;

namespace Phobos.ECS;

// ReSharper disable MemberCanBePrivate.Global
public class SystemOrchestrator : IActorSystem
{
    public readonly MovementSystem MovementSystem;
    public readonly ActorTaskSystem ActorTaskSystem;
    public readonly SquadOrchestrator SquadOrchestrator;
    private readonly List<IActorSystem> _systems;

    public SystemOrchestrator(NavJobExecutor navJobExecutor, ObjectiveQueue objectiveQueue)
    {
        MovementSystem = new MovementSystem(navJobExecutor);
        ActorTaskSystem = new ActorTaskSystem(MovementSystem);
        SquadOrchestrator = new SquadOrchestrator(ActorTaskSystem, objectiveQueue);

        _systems = [MovementSystem, ActorTaskSystem, SquadOrchestrator];
    }

    public void AddActor(Actor actor)
    {
        for (var i = 0; i < _systems.Count; i++)
        {
            _systems[i].AddActor(actor);
        }
    }

    public void RemoveActor(Actor actor)
    {
        for (var i = 0; i < _systems.Count; i++)
        {
            _systems[i].RemoveActor(actor);
        }
    }

    public void Update()
    {
        SquadOrchestrator.Update();
        ActorTaskSystem.Update();
        MovementSystem.Update();
    }
}