using System.Collections.Generic;
using Comfort.Common;
using Phobos.Components;
using Phobos.Entities;
using Phobos.Helpers;
using Phobos.Orchestration;
using UnityEngine;

namespace Phobos.Diag;

public class MoveTelemetry : MonoBehaviour
{
    private FramePacing _cleanupPacing;
    private Dictionary<Agent, VisState> _states;

    private List<Agent> _deleteBuffer;
    
    public void Awake()
    {
        _cleanupPacing = new FramePacing(10);
        _states = new();
        _deleteBuffer = [];
    }
    
    public void Update()
    {
        var phobos = Singleton<PhobosManager>.Instance;
        var agents = phobos.AgentData.Entities.Values;
        
        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];

            if (!_states.TryGetValue(agent, out var state))
            {
                state = new VisState();
                _states.Add(agent, state);
            }

            // ReSharper disable once InvertIf
            if (agent.Movement.HasPath && state.Path != agent.Movement.Path)
            {
                state.Path = agent.Movement.Path;
                state.PathVis.Set(agent.Movement.Path);
            }

            if (agent.Movement.Target != Vector3.zero && agent.Movement.Target != Movement.Infinity)
            {
                DebugGizmos.Line(agent.Position, agent.Movement.Target, color: Color.blue, expiretime: 0.15f);
            }
        }
        
        if(_cleanupPacing.Blocked())
            return;

        foreach (var (key, value) in _states)
        {
            if (phobos.AgentData.Entities.Values.Contains(key))
                continue;

            value.Path = null;
            value.PathVis.Clear();
            
            _deleteBuffer.Add(key);
        }

        for (var i = 0; i < _deleteBuffer.Count; i++)
        {
            var agent = _deleteBuffer[i];
            _states.Remove(agent);
        }
        
        _deleteBuffer.Clear();
    }

    public void OnDestroy()
    {
        foreach (var value in _states.Values)
        {
            value.Path = null;
            value.PathVis.Destroy();
        }
    }

    private class VisState
    {
        public Vector3[] Path;
        public readonly PathVis PathVis = new();
    }
}