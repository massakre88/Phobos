using Phobos.Navigation;

namespace Phobos.Components;

public class Guard
{
    public CoverPoint? CoverPoint;
    public float WatchTimeout;
    public float WatchDirection;
    
    public override string ToString()
    {
        return $"{nameof(Guard)}({CoverPoint})";
    }
}