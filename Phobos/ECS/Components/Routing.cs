using EFT;
using Phobos.Navigation;
using UnityEngine;

namespace Phobos.ECS.Components;

public enum RoutingStatus
{
    Inactive,
    Active,
    Completed,
    Failed
}

public class Routing
{
    public NavPath Path = new();
    
    public RoutingStatus Status = RoutingStatus.Inactive;
    public Vector3 Destination;
    public int CurrentCorner;
    public float SqrDistance;
    
    public void Set(NavJob job)
    {
        Path = new NavPath(job);
        Status = RoutingStatus.Active;
        Destination = job.Destination;
        CurrentCorner = 1;
    }

    public void Update(BotOwner bot)
    {
        SqrDistance = (Destination - bot.Position).sqrMagnitude;
        CurrentCorner = bot.Mover.ActualPathController.CurPath.CurIndex;
    }

    public override string ToString()
    {
        return $"Routing(Corner: {CurrentCorner}/{Path.Corners.Length}, SqrDistance: {SqrDistance}, Status: {Status})";
    }
}