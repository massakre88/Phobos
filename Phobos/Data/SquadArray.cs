using Phobos.Entities;

namespace Phobos.Data;

public class SquadArray : EntityArray<Squad>
{
    public Squad Add()
    {
        var id = Reserve();
        var squad = new Squad(id);
        Values.Add(squad);
        return squad;
    }
}