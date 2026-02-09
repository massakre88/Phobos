using System;
using UnityEngine;

namespace Phobos.Navigation;

public enum CoverCategory
{
    Hard,
    Soft,
    None
}

public readonly struct CoverPoint : IEquatable<CoverPoint>
{
    public readonly Vector3 Position;
    public readonly Vector3 Direction;
    public readonly CoverCategory Category;
    public readonly CoverLevel Level;

    public CoverPoint(Vector3 position, Vector3 direction, CoverCategory category, CoverLevel level)
    {
        Position = position;
        Direction = direction;
        Category = category;
        Level = level;
    }

    public CoverPoint(Vector3 position, Vector3 direction, CoverType category, CoverLevel level)
    {
        Position = position;
        Direction = direction;
        Category = category switch
        {
            CoverType.Wall => CoverCategory.Hard,
            CoverType.Foliage => CoverCategory.Soft,
            _ => CoverCategory.None
        };
        Level = level;
    }


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

    public override string ToString()
    {
        return $"{nameof(CoverPoint)}(category: {Category}, level: {Level})";
    }
}