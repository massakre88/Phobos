using Phobos.Navigation;

namespace Phobos.Components;

public class Guard
{
    public CoverPoint? CoverPoint;
    
    public override string ToString()
    {
        return $"{nameof(Guard)}";
    }
}