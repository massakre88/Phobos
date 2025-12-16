namespace Phobos.Tasks.Strategies;

public struct StrategyScore(float score, BaseStrategy strategy)
{
    public readonly float Score = score;
    public readonly BaseStrategy Strategy = strategy;
}