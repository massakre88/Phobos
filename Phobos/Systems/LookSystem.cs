using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Components;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Helpers;
using UnityEngine;

namespace Phobos.Systems;

public class LookSystem
{
    private const float MoveLookAheadDistSqr = 1f;
    private const float MoveTargetProxmityDistSqr = 5f * 5f;

    public static void Update(List<Agent> liveAgents)
    {
        for (var i = 0; i < liveAgents.Count; i++)
        {
            var agent = liveAgents[i];

            // Bail out if the agent is inactive
            if (!agent.IsActive)
            {
                agent.Look.Target = null;
                continue;
            }

            var bot = agent.Bot;
            var movement = agent.Movement;

            if (agent.Look.Target != null)
            {
                switch (agent.Look.Type)
                {
                    case LookType.Position:
                        bot.Steering.LookToPoint(agent.Look.Target.Value, 360f);
                        break;
                    case LookType.Direction:
                        bot.Steering.LookToDirection(agent.Look.Target.Value, 360f);
                        break;
                    default:
                        DebugLog.Write($"LookType {agent.Look.Type} not implemented");
                        break;
                }
            }
            else if (movement.IsValid)
            {
                if ((movement.Target - bot.Position).sqrMagnitude > MoveTargetProxmityDistSqr)
                {
                    var fwdPoint = PathHelper.CalcForwardPoint(
                        movement.Path, bot.Position, movement.CurrentCorner, MoveLookAheadDistSqr
                    ) + 1.25f * Vector3.up;
                    bot.Steering.LookToPoint(fwdPoint, 540f);
                }
                else
                {
                    bot.Steering.LookToMovingDirection();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LookToPoint(Agent agent, Vector3 target)
    {
        agent.Look.Target = target;
        agent.Look.Type = LookType.Position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LookToDirection(Agent agent, Vector3 target)
    {
        agent.Look.Target = target;
        agent.Look.Type = LookType.Direction;
    }
}