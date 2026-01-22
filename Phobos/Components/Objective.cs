using Phobos.Navigation;
using UnityEngine;

namespace Phobos.Components;


public class Objective
{
    public Location Location;
    public Vector3[] ArrivalPath;

    public override string ToString()
    {
        return $"Objective({Location})";
    }
}