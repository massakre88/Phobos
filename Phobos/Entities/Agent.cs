using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT;
using Phobos.Components;
using Phobos.Tasks;
using Phobos.Tasks.Actions;

namespace Phobos.Entities;

public class Agent(BotOwner bot, int id) : Entity(id)
{
    public readonly int BotId = bot.Id;
    public readonly int SquadId = bot.BotsGroup.Id;
    public readonly BotOwner Bot = bot;

    public readonly List<IComponent> Components = new(32);
    public readonly List<ActionScore> Actions = new(16);
    public BaseAction CurrentAction;
    
    public bool IsLayerActive = false;
    public bool IsPhobosActive = true;
    
    public bool IsActive
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsLayerActive && IsPhobosActive;
    }

    public override string ToString()
    {
        return $"Actor(Id: {Id}, Name: {Bot.Profile.Nickname}, LayerActive: {IsLayerActive}, PhobosActive: {IsPhobosActive})";
    }
}