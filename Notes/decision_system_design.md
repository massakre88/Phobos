# Overall architecture
We'll use Utility AI for both individual agents and squads. Squads could ostensibly use GOAP, but there's no real benefit for the complexity. Squads can always generate a primary strategic plan for the raid (e.g. go through a number of locations and then exfil at a particular place), and then assign a utility to it. 

* Squads will execute Strategies
* Agents will execute Actions

# Tactical Utility AI
Example would be a simple two function setup:

```csharp
float QuestUtility(...)
{
    if (IsNearObjective)
        return 0f
    // Sharply approach 1 as we near the objective
    return Mathf.Clamp01((CurrentDistance / TotalDistance) ^ 4);
}

float WaitUtility(...)
{
    // If objective is reached, this becomes important. Otherwise it'll trigger if nothing else is triggering.
    return HasAchievedQuestObjective && IsNearObjective ? 1f : 0.001f;
}
```

The bot will then organically pick the best action depending on the utility. If it's very close to the objective, it might want to finish that first even though an ally is in danger. On the other hand, if it's too far away from the nearest ally, it might want to head back and hook up with that ally first.

### Notes
* Tie Breaking:
  * If two or more actions have the same utility, switch to an *Undecided* action that makes the bot wait.
  * Alternatively, use an ex-ante priority defined for each action that we use as tie breakers.

### Actions
* Wait: fallback in case nothing else is triggering or there's a tie
* GotoObjective
* OpenDoor
* GotoObjectiveCareful: slows down and looks at stuff like doors 

# Strategic Utility AI 
Squads will influence agent behavior by modifying specific components. E.g. the GotoObjectiveStrategy will pick an squad objective if there isn't one and set all agents' ObjectiveComponent to this.

## Implementation
Similar architecture to the agent utility system.

```csharp
class Strategy
{
    // ... get squad and agent components
    
    public void UpdateUtility()
    {
        foreach (var squad in dataset.Squads)
        {
            foreach (var agent in squad.Agents)
            {
                var agentComponent = _someAgentComponentArray[agent.Id]
            }
            // ... calculate utility for the squad
            var squadComponent = _someSquadComponentArray[squad.Id]
            score = ...;
            squad.UtilityScores.Add((this, score));
        }
    }
    
    public void Update()
    {
        foreach (var squad in ActiveSquads)
        {
            // ... carry out the strategy logic
        }
    }
}

class StrategySystem
{
    // ... pretty much the same logic as ActionSystem, but operate on squads.
}
```

### Strategy
* Like Action but operates on squads
* Extracts out component arrays for both agents and squads.
* The Update() method gets the squad components from the arrays based on the Squad Id.
* Agent level components can be retrieved by iterating through the agents in the squad and using their id into the agent level components.

* SquadSystem
  * 


### SquadSystem
* Assigns agents to squads

### SquadManager
* Manages a single squad
* Data:
  * Squad entity
  * 
* Execution:
  * Iterate through each strategy 

### Strategy
* Since all agents in a squad must be available to each strategy, strategies do not keep an internal list of agents - the squad manager does that.
* The squad calculates the utility for each strategy
* The squad picks the strategy with the highest utility
* That strategy is executed.
* All agents 
