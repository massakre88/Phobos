using System;
using System.Collections.Generic;
using EFT.Interactive;
using UnityEngine;

namespace Phobos.Navigation;

public enum LocationCategory
{
    ContainerLoot,
    LooseLoot,
    Quest,
    Synthetic,
    Exfil
}

public class Location(
    int id,
    LocationCategory category,
    string name,
    Vector3 position,
    float radiusSqr,
    List<Door> doors,
    List<CoverPoint> coverPoints
) : IEquatable<Location>
{
    private readonly int _id = id;
    public readonly Vector3 Position = position;
    public readonly float RadiusSqr = radiusSqr;
    public readonly LocationCategory Category = category;
    public readonly List<Door> Doors = doors;
    public readonly List<CoverPoint> CoverPoints = coverPoints;

    public bool Equals(Location other)
    {
        if (other is null) return false;
        return _id == other._id;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Location)obj);
    }

    public override int GetHashCode()
    {
        return _id;
    }

    public static bool operator ==(Location lhs, Location rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
            // null == null = true.
            // Only the left side is null.
        }

        // Equals handles the case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Location lhs, Location rhs) => !(lhs == rhs);

    public override string ToString()
    {
        return $"Location({_id}, {Category}, {name})";
    }
}