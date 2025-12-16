using Phobos.Entities;

namespace Phobos.Tasks.Actions;

public abstract class BaseAction(float hysteresis) : BaseTask<Agent>(hysteresis)
{
    
}