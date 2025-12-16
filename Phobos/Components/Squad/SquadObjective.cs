using System.Runtime.CompilerServices;
using Phobos.Navigation;

namespace Phobos.Components.Squad;

public class SquadObjective(int id) : IComponent
{
    public int Id
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => id;
    }
    
    public Location Location;
}