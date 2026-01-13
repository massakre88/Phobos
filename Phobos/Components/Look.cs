using UnityEngine;

namespace Phobos.Components;

public enum LookType
{
    Position,
    Direction
}

public class Look
{
    public Vector3? Target = null;
    public LookType Type = LookType.Position;
}