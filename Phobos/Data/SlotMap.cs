using System;
using System.Collections.Generic;

namespace Phobos.Data;

public abstract class Entity
{
    public int Id = -1;
}

public class SlotMap<T> where T : Entity
{
    private readonly int?[] _slots;
    private readonly List<int> _emptySlots;
    private readonly List<T> _data;

    public List<T> Data => _data;
    
    public SlotMap(int capacity)
    {
        _slots = new int?[capacity];
        _emptySlots = new List<int>(capacity);
        for (var i = 0; i < capacity; i++)
            _emptySlots.Add(i);
        _data = new List<T>(capacity);
    }
    
    public void Add(T entity)
    {
        if (entity.Id >= 0)
            throw new ArgumentException($"Entity with id {entity.Id} already registered with a SlotMap.");
            
        var newLength = _data.Count + 1;
        
        if (newLength >= _slots.Length)
            throw new ArgumentOutOfRangeException($"SlotMap limit reached {newLength} > {_slots.Length}");
        
        var lastIndex = _emptySlots.Count - 1;
        var slotIndex = _emptySlots[lastIndex];
        _emptySlots.RemoveAt(lastIndex);
        entity.Id = slotIndex;
    }
    
    public bool Remove(T entity)
    {
        var slot = _slots[entity.Id];
        
        if (!slot.HasValue)
            return false;
        
        var entityIndex = slot.Value;
        var lastIndex = _data.Count - 1;
        
        // Swap the last item into the location of the item being removed
        _data[entityIndex] = _data[lastIndex];
        _data.RemoveAt(lastIndex);
        _emptySlots.Add(entity.Id);
        
        return true;
    }
}