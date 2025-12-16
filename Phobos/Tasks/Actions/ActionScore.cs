namespace Phobos.Tasks.Actions;

public struct ActionScore(float score, BaseAction action)
{
    public readonly float Score = score;
    public readonly BaseAction Action = action;
}