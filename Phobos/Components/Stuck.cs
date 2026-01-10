using Phobos.Helpers;
using UnityEngine;

namespace Phobos.Components;

public enum StuckState
{
    None,
    Vaulting,
    Jumping,
    Retrying,
    Teleport,
    Failed
}

public class Stuck
{
    public readonly TimePacing Pacing = new(0.1f);
    
    public StuckState State = StuckState.None;
    public Vector3 LastPosition;
    public float LastSpeed;
    public float LastUpdate; 
    public float Timer;
    
    public override string ToString()
    {
        return $"Stuck(State: {State} timer: {Timer} last speed: {LastSpeed} last position: {LastPosition})";
    }
}