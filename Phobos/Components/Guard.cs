using System.Runtime.CompilerServices;
using Phobos.Navigation;

namespace Phobos.Components;

public class Guard(int id) : IComponent
{
    public int Id
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => id;
    }
    public Location Location;
    
    public override string ToString()
    {
        return $"{nameof(Guard)}({Location})";
    }
}