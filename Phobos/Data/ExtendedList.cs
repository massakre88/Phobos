using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Phobos.Data;

public class ExtendedList<T>(int capacity) : List<T>(capacity)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SwapRemove(T member)
    {
        for (var i = 0; i < Count; i++)
        {
            var candidate = this[i];
            if (!candidate.Equals(member)) continue;
            SwapRemoveAt(i);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SwapRemoveAt(int index)
    {
        var lastIndex = Count - 1;
        this[index] = this[lastIndex];
        RemoveAt(lastIndex);
    }
}