using HarmonyLib;
using RimWorld;
using Verse.AI.Group;
using Verse;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(RimWorld.LordJob_MechanoidsDefend), "CreateGraph")]
    static class LordJob_MechanoidsDefend_CreateGraph
    {
        static bool Prefix(ref StateGraph __result, RimWorld.LordJob_MechanoidsDefend __instance)
        {
            StateGraph stateGraph = new StateGraph();
            var instance = Traverse.Create(__instance);
            var defSpot = instance.Field("defSpot");
            if (!defSpot.Property("IsValid").GetValue<bool>())
            {
                Log.Warning("LordJob_MechanoidsDefendShip defSpot is invalid. Returning graph for LordJob_AssaultColony.");
                stateGraph.AttachSubgraph(new RimWorld.LordJob_AssaultColony(instance.Field("faction").GetValue<Faction>(), true, true, false, false, true, false, false).CreateGraph());
                __result = stateGraph;
                return false;
            }
            LordToil_DefendPoint lordToil_DefendPoint = new LordToil_DefendPoint(defSpot.GetValue<IntVec3>(), instance.Field("defendRadius").GetValue<float>(), null);
            stateGraph.StartingToil = lordToil_DefendPoint;
            var lordToil_AssaultColony = new RimWorld.LordToil_AssaultColonyBreaching();
            stateGraph.AddToil(lordToil_AssaultColony);
            if (instance.Field("canAssaultColony").GetValue<bool>())
            {
                var lordToil_AssaultColony2 = new RimWorld.LordToil_AssaultColonyBreaching();
                stateGraph.AddToil(lordToil_AssaultColony2);
                Transition transition = new Transition(lordToil_DefendPoint, lordToil_AssaultColony, false, true);
                transition.AddSource(lordToil_AssaultColony2);
                transition.AddTrigger(new Trigger_PawnCannotReachMapEdge());
                stateGraph.AddTransition(transition, false);
                Transition transition2 = new Transition(lordToil_DefendPoint, lordToil_AssaultColony2, false, true);
                transition2.AddTrigger(new Trigger_PawnHarmed(0.5f, true, null));
                transition2.AddTrigger(new Trigger_PawnLostViolently(true));
                transition2.AddTrigger(new Trigger_Memo(LordJob_MechanoidsDefend.MemoDamaged));
                transition2.AddPostAction(new TransitionAction_EndAllJobs());
                stateGraph.AddTransition(transition2, false);
                Transition transition3 = new Transition(lordToil_AssaultColony2, lordToil_DefendPoint, false, true);
                transition3.AddTrigger(new Trigger_TicksPassedWithoutHarmOrMemos(1380, new string[]
                {
                    LordJob_MechanoidsDefend.MemoDamaged
                }));
                transition3.AddPostAction(new TransitionAction_EndAttackBuildingJobs());
                stateGraph.AddTransition(transition3, false);
                Transition transition4 = new Transition(lordToil_DefendPoint, lordToil_AssaultColony, false, true);
                transition4.AddSource(lordToil_AssaultColony2);
                transition4.AddTrigger(new Trigger_AnyThingDamageTaken(__instance.things, 0.5f));
                transition4.AddTrigger(new Trigger_Memo(HediffGiver_Heat.MemoPawnBurnedByAir));
                stateGraph.AddTransition(transition4, false);
            }
            Transition transition5 = new Transition(lordToil_DefendPoint, lordToil_AssaultColony, false, true);
            transition5.AddTrigger(new Trigger_ChanceOnSignal(TriggerSignalType.MechClusterDefeated, 1f));
            stateGraph.AddTransition(transition5, false);
            if (!instance.Field("isMechCluster").GetValue<bool>())
            {
                Transition transition6 = new Transition(lordToil_DefendPoint, lordToil_AssaultColony, false, true);
                transition6.AddTrigger(new Trigger_AnyThingDamageTaken(__instance.things, 1f));
                stateGraph.AddTransition(transition6, false);
            }
            __result = stateGraph;
            return false;
        }
    }
}
