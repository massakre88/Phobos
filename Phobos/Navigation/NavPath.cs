using UnityEngine;

namespace Phobos.Navigation;

public readonly struct NavPath(Vector3[] corners)
{
    public readonly Vector3[] Corners = corners;

    public NavPath() : this([])
    {
    }
    
    public NavPath(NavJob job) : this(job.Corners)
    {
    }
}