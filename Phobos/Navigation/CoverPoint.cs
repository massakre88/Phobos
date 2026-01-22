using System;
using UnityEngine;

namespace Phobos.Navigation;

public readonly struct CoverPoint(Vector3 position, Vector3 wallDirection, CoverType coverType, CoverLevel coverLevel) : IEquatable<CoverPoint>
{
    public readonly Vector3 Position = position;
    public readonly Vector3 WallDirection = wallDirection;
    public readonly CoverType CoverType = coverType;
    public readonly CoverLevel CoverLevel = coverLevel;

    public bool Equals(CoverPoint other)
    {
        return Position.Equals(other.Position);
    }

    public override bool Equals(object obj)
    {
        return obj is CoverPoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }
}