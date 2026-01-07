using System.Collections.Generic;
using Phobos.Entities;
using Phobos.Helpers;
using UnityEngine;

namespace Phobos.Systems;

public class LookSystem
{
    private const float MoveLookAheadDistSqr = 1.5f;
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
                bot.Steering.LookToPoint(agent.Look.Target.Value);
            }
            else if (movement.IsValid && (movement.Target - bot.Position).sqrMagnitude > MoveTargetProxmityDistSqr)
            {
                var fwdPoint = PathHelper.CalcForwardPoint(
                    movement.Path, bot.Position, movement.CurrentCorner, MoveLookAheadDistSqr
                ) + 1.5f * Vector3.up;
                bot.Steering.LookToPoint(fwdPoint, 360f);
            }
            else
            {
                bot.Steering.LookToMovingDirection();
            }
        }
    }
}