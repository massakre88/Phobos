# TODO:
* Add actor and squad level objective management
  * Move after-suspension retry to the objective system. It shouldn't be the movement system that decide to retry moving to the target after the bot is unsuspended.
* Add simplistic door handling where the bot auto-opens nearby doors and stops sprinting if there's a door within 5 meters
* Extend the movement system so that the Movement component can be used to define the target speed and look point. By default the look point will be null, in which case the bot will look forward at the path. However, the squad will sometimes instruct members to slow down (e.g. 0.5 speed) and look at doors and pois in the vicinity.
* Sprinting
  * Re-enable sprinting
  * Add stamina system to the movement and sprinting
  * Add movement and stance status tracking to a component, so we keep track of stuff like the path angle jitter, etc.

# End Goals (add ideas over time)
## Gameplay
* GOAP style strategic planning including picking ideal points of entry, approaching from multiple angles, etc...
* Strategically picking objectives at raid start.

* Bot personalities affect strategic planning
* Bosses and special bot types can have "hunt the player(s)" objectives.
  * Some even can have "stalk" objectives where they try to stay hidden.

## Objectives
* Stalk players (bosses/rogues only?)
* Quests
* Loot goblining
* Exfil (after enough other objectives have been accomplished or got hurt too much)
* Regroup (if a team member is under attack or the team got spread out too much)

# Notes
## Doors
* Initial door logic will be to put all doors in a grid, we then look up nearby doors on every frame and doors which are closer than 3m we open
* We also slow down movement whenever in 5m vicinity of doors to avoid glitching into them.
