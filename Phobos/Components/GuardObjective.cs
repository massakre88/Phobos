using Phobos.Navigation;
using UnityEngine;

namespace Phobos.Components;

public class GuardObjective
{
    public Location Location;
    public Vector3[] ArrivalPath;
    
    public override string ToString()
    {
        return $"{nameof(GuardObjective)}({Location})";
    }
}