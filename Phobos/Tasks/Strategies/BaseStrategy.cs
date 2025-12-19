using Phobos.Entities;

namespace Phobos.Tasks.Strategies;

public abstract class BaseStrategy(float hysteresis) : BaseTask<Squad>(hysteresis);