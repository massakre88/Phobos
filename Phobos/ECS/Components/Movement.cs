using EFT;
using Phobos.Navigation;
using UnityEngine;

namespace Phobos.ECS.Components;

public enum MovementStatus
{
    Suspended,
    Active,
    Retry,
    Completed,
    Failed
}

public class Target
{
    public Vector3 Position;
    public float DistanceSqr;
    public Vector3[] Path;
}

public class Movement(BotOwner bot)
{
    public MovementStatus Status = MovementStatus.Suspended;
    public Target Target;
    public BotCurrentPathAbstractClass ActualPath => bot.Mover.ActualPathController.CurPath;

    public void Set(NavJob job)
    {
        Target ??= new Target();
        Target.Position = job.Destination;
        Target.Path = job.Path;
    }

    public override string ToString()
    {
        return
            $"Movement(HasTarget: {Target != null} DistanceSqr: {Target?.DistanceSqr}, Status: {Status} Path: {ActualPath?.CurIndex}/{ActualPath?.Length})";
    }
}