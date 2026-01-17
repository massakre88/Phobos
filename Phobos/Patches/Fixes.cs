using System.Diagnostics;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using Phobos.Diag;
using Phobos.Orchestration;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Phobos.Patches;

// After SAIN/Phobos deactivates, this function will often teleport them to some weird position that BSG thinks the bot should have
// It's preferable if the bot is simply stuck rather than this.
public class GotoPositionTeleportFixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        /*
        Vector3 target,
        bool slowAtTheEnd,
        float reachDist,
        bool getUpWithCheck,
        bool mustHaveWay,
        bool onlyShortTrie = false,
        bool force = false,
        bool slowCalcUsingNativeNavMesh = false
         */
        return AccessTools.Method(
            typeof(BotPathFinderClass),
            nameof(BotPathFinderClass.GoToPosition),
            parameters: [typeof(Vector3), typeof(bool), typeof(float), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)]
        );
    }

    // ReSharper disable once InconsistentNaming
    [PatchPrefix]
    public static void Patch(ref bool mustHaveWay)
    {
        mustHaveWay = false;
    }
}

public class BotMoverTeleportFixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotMover).GetMethod(nameof(BotMover.Teleport));
    }

    // ReSharper disable once InconsistentNaming
    [PatchPrefix]
    public static bool Patch(BotMover __instance)
    {
        if (__instance is GClass493)
            return true;

        DebugLog.Write($"Teleport role: {__instance.BotOwner_0.GetPlayer.Profile?.Info?.Settings?.Role} name: {__instance.BotOwner_0.GetPlayer.Profile?.Nickname} {new StackTrace(true)}");
        return false;
    }
}

// Stolen from Solarint's SAIN
// Disables the check for is ai in movement context. could break things in the future
public class MovementContextIsAIPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.IsAI));
    }

    // ReSharper disable once InconsistentNaming
    [PatchPrefix]
    public static bool Patch(ref bool __result)
    {
        __result = false;
        return false;
    }
}

// Stolen from Solarint's SAIN
// Disable specific functions in Manual Update that might be causing erratic movement in sain bots if they are in combat.
public class BotMoverManualFixedUpdatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotMover).GetMethod(nameof(BotMover.ManualFixedUpdate));
    }

    // ReSharper disable once InconsistentNaming
    [PatchPrefix]
    public static bool PatchPrefix(BotMover __instance)
    {
        return Singleton<PhobosManager>.Instance == null || !Singleton<BsgBotRegistry>.Instance.IsPhobosActive(__instance.BotOwner_0);
    }
}

// Stolen from Solarint's SAIN
// Disable specific functions in Manual Update that might be causing erratic movement in sain bots.
// NB: Currently unused as it seems unneeded.
// ReSharper disable once ClassNeverInstantiated.Global
public class BotMoverManualUpdatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotMover).GetMethod(nameof(BotMover.ManualUpdate));
    }

    // ReSharper disable once InconsistentNaming
    [PatchPrefix]
    public static bool PatchPrefix(BotMover __instance)
    {
        if (Singleton<PhobosManager>.Instance == null || !Singleton<BsgBotRegistry>.Instance.IsPhobosActive(__instance.BotOwner_0))
        {
            return true;
        }

        __instance.LocalAvoidance.DropOffset();
        __instance.PositionOnWayInner = __instance.BotOwner_0.Position;

        //__instance.method_16();
        //__instance.method_15();
        __instance.method_14();
        //__instance.LocalAvoidance.ManualUpdate();
        return false;
    }
}

// Stolen from Solarint's SAIN
public class EnableVaultPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.InitVaultingComponent));
    }

    // ReSharper disable once InconsistentNaming
    [PatchPrefix]
    public static void Patch(Player __instance, ref bool aiControlled)
    {
        if (__instance.UsedSimplifiedSkeleton)
        {
            return;
        }

        aiControlled = false;
    }
}