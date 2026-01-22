using System.Collections.Generic;
using System.Reflection;
using EFT;
using Phobos.Diag;
using SPT.Reflection.Patching;

namespace Phobos.Patches;

/// <summary>
/// Shrink the open state carvers to prevent narrow hallways being completedy blocked off on the navmesh by open doors
/// </summary>
public class ShrinkDoorNavMeshCarversPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    // ReSharper disable once InconsistentNaming
    [PatchPostfix]
    public static void Patch()
    {
        var processed = new HashSet<NavMeshDoorLink>();
        var doorsController = UnityEngine.Object.FindObjectOfType<BotDoorsController>();

        if (doorsController == null)
        {
            return;
        }
        
        Log.Debug($"Shrinking {doorsController._navMeshDoorLinks.Count} door navmesh cutters");
        
        for (var i = 0; i < doorsController._navMeshDoorLinks.Count; i++)
        {
            var doorLink = doorsController._navMeshDoorLinks[i];
            
            if (!processed.Add(doorLink))
            {
                continue;
            }

            doorLink.Carver_Opened.size = 0.375f * doorLink.Carver_Opened.size;
            doorLink.Carver_Closed.size = 0.375f * doorLink.Carver_Closed.size;
            doorLink.Carver_Breached.size = 0.375f * doorLink.Carver_Breached.size;
        }
    }
}