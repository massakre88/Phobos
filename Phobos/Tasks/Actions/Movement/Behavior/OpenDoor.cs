namespace Phobos.Tasks.Actions.Movement.Behavior;

public class OpenDoor
{
    // Logic:
    // Detect doors like we do with the voxel links
    // Pick the door the current path is actually going through (don't bother with other doors).
    // Stash away this door in a DoorOpen component
    // This action is only run if a valid door is defined in the component and it is closed, then it runs at max priority until the door is opened
    // Once the door is opened, it gets deprioritized to 0.
}