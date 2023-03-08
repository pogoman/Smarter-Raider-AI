using HarmonyLib;
using Mono.Unix.Native;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using Unity.Baselib.LowLevel;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Tilemaps;
using UnityEngine.XR;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.GridBrushBase;

namespace PogoAI
{
	public static class Misc
    {/*	

		[HarmonyPatch(typeof(JobGiver_AISapper), nameof(JobGiver_AISapper.TryGiveJob))]
		private static class JobGiver_AISapper_TryGiveJob_Patch
		{
			public static bool Prefix(Pawn pawn, ref Job __result, JobGiver_AISapper __instance)
			{
				Log.Message($"JobGiver_AISapper");
				if (pawn.CurJobDef == JobDefOf.UseVerbOnThing || pawn.CurJobDef == JobDefOf.Goto || pawn.mindState.duty.def == DutyDefOf.Sapper
					|| pawn.CurJobDef == JobDefOf.AttackMelee)
				{
					Log.Message($"{pawn} Too Busy too Sap");
					return false;
				}

				__result = TryGiveBreachJob(pawn);
				if (__result != null)
				{
					return false;
				}

				Thing clostestPlayerPOI = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, 
					pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.Door), 
					PathEndMode.Touch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false));

				if (clostestPlayerPOI == null)
				{
					Log.Message($"No doors");
					clostestPlayerPOI = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map,
					pawn.Map.listerBuildings.allBuildingsColonist,
					PathEndMode.Touch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false));
				}
				var distanceFromDoor = pawn.Position.DistanceToSquared(clostestPlayerPOI.Position);
				if (distanceFromDoor > 1000)
				{
					using (PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, clostestPlayerPOI.Position, 
						TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, true, false), PathEndMode.Touch))
					{
						Log.Message($"{pawn} Too far to sap {distanceFromDoor} {clostestPlayerPOI} {clostestPlayerPOI.Position}");
						Job job = JobMaker.MakeJob(JobDefOf.Goto, path.LastNode);
						job.checkOverrideOnExpire = true;
						job.expiryInterval = 500;
						job.collideWithPawns = true;
						__result = job;
						return false;
					}
				}

				Log.Message($"{pawn} Try Sap");
				IntVec3 intVec = pawn.mindState.duty.focus.Cell;
				if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
				{
					pawn.GetLord().Notify_ReachedDutyLocation(pawn);
					return false;
				}
				if (!intVec.IsValid)
				{
					IAttackTarget attackTarget;
					if (!(from x in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
						  where !x.ThreatDisabled(pawn) && x.Thing.Faction == Faction.OfPlayer && pawn.CanReach(x.Thing, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings)
						  select x).TryRandomElement(out attackTarget))
					{
						return false;
					}
					intVec = attackTarget.Thing.Position;
				}
				if (!pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings))
				{
					return false;
				}
				using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, false, false), PathEndMode.OnCell, null))
				{
					IntVec3 cellBeforeBlocker;
					Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
					if (thing != null)
					{
						Job job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, __instance.canMineMineables, __instance.canMineNonMineables);
						if (job != null)
						{
							__result = job;
							return false;
						}
					}
				}
				__result = JobMaker.MakeJob(JobDefOf.Goto, intVec, 500, true);
				return false;
			}
		}

		[HarmonyPatch(typeof(JobGiver_AIFightEnemy), nameof(JobGiver_AIFightEnemy.TryGiveJob))]
		private static class JobGiver_AIFightEnemy_TryGiveJob_Patch
		{
			public static void Prefix(Pawn pawn, JobGiver_AIFightEnemy __instance)
			{
				Log.Message($"JobGiver_AIFightEnemy {pawn.mindState.duty.def}");
				//if (pawn.mindState.duty.def != DutyDefOf.AssaultColony)
				//{
				//	Log.Message($"should assault"); Lord lord = pawn.GetLord();
				//	LordToil_AssaultColony toil = (LordToil_AssaultColony)pawn.GetLord().CurLordToil;
				//	pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
				//	pawn.mindState.duty.attackDownedIfStarving = toil.attackDownedIfStarving;
				//	pawn.mindState.duty.pickupOpportunisticWeapon = toil.canPickUpOpportunisticWeapons;
				//}					
				__instance.needLOSToAcquireNonPawnTargets = true;
			}
		}

		[HarmonyPatch(typeof(JobGiver_AITrashColonyClose), nameof(JobGiver_AITrashColonyClose.TryGiveJob))]
		private static class JobGiver_AITrashColonyClose_TryGiveJob_Patch
		{
			public static void Prefix(Pawn pawn, ref Job __result)
			{
				Log.Message($"JobGiver_AITrashColonyClose");
			}
		}

		[HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), nameof(JobGiver_AIGotoNearestHostile.TryGiveJob))]
		private static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
		{
			public static bool Prefix(Pawn pawn, ref Job __result)
			{
				Log.Message($"JobGiver_AIGotoNearestHostile");
				if (pawn.CurJobDef == JobDefOf.Goto)
				{
					Log.Message("Already going to hostile");
					return false;
				}
				
				float num = float.MaxValue;
				Thing thing = null;
				List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
				//.Where(x => x.Thing.Position.DistanceToSquared(pawn.Position) < 500).ToList();
				Log.Message($"Targets nearby to check pathcoist {potentialTargetsFor.Count}");
				for (int i = 0; i < potentialTargetsFor.Count; i++)
				{
					IAttackTarget attackTarget = potentialTargetsFor[i];
					Pawn pawn2;
					if (!attackTarget.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget) && ((pawn2 = attackTarget.Thing as Pawn) == null || pawn2.IsCombatant() || GenSight.LineOfSightToThing(pawn.Position, pawn2, pawn.Map, false, null)))
					{
						Thing thing2 = (Thing)attackTarget;
						int num2 = thing2.Position.DistanceToSquared(pawn.Position);
						if (num2 < num && pawn.CanReach(thing2, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
						{
							num = num2;
							thing = thing2;
						}
					}
				}

				if (thing != null)
				{
					var pathCostAvoidGrid = GetPathCost(pawn, thing, TraverseParms.For(pawn, Danger.None,
						TraverseMode.ByPawn, false, true, false));
					var pathCostNoAvoid = GetPathCost(pawn, thing, TraverseParms.For(pawn, Danger.None,
						TraverseMode.ByPawn, false, false, false));
					Log.Message($"{pawn} {thing} {pathCostAvoidGrid} {pathCostNoAvoid}");
					var pathSafe = pathCostAvoidGrid == pathCostNoAvoid;
					if (pathSafe)
					{
						//Its safe
						Job job = JobMaker.MakeJob(JobDefOf.Goto, thing);
						job.checkOverrideOnExpire = true;
						job.expiryInterval = 500;
						job.collideWithPawns = false;
						__result = job;
						return false;
					}
				}

				return false;
			}

			private static float GetPathCost(Pawn pawn, Thing thing, TraverseParms traverseParams)
			{
				var cell = thing.Position;
				var pos = pawn.Position;

				using (PawnPath path = pawn.Map.pathFinder.FindPath(pos, cell, traverseParams, PathEndMode.OnCell))
				{
					return path.TotalCost;
				}
			}
		}

		[HarmonyPatch(typeof(JobGiver_AITrashBuildingsDistant), nameof(JobGiver_AITrashBuildingsDistant.TryGiveJob))]
		private static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
		{
			public static bool Prefix(Pawn pawn, ref Job __result)
			{
				return false;
			}
		}

		//private static bool isMeleeAmmoCheck(Pawn pawn, Verb verb)
		//{
		//	var meleeAttack = verb?.IsMeleeAttack ?? true;			
		//	var equip = pawn.equipment?.PrimaryEq?.ToString() ?? "";
		//	if (meleeAttack)
		//	{
		//		meleeAttack = !new string[] { "Stick", "Concussion", "HE", "Rocket", "Inferno", "Blast" }.Any(x => equip.Contains(x));
		//		if (meleeAttack)
		//		{
		//			meleeAttack = !pawn.inventory.innerContainer.Any(x => x.ToString().Contains("HE"));
		//		}
		//	}
		//	Log.Message($"{pawn} {verb} {meleeAttack}");
		//	return meleeAttack;
		//}

		private static bool IsBreacher(Pawn pawn)
		{
			Verb verb = BreachingUtility.FindVerbToUseForBreaching(pawn);
			return verb?.verbProps?.ai_IsBuildingDestroyer ?? false;
		}

		private static Job TryGiveBreachJob(Pawn pawn)
		{
			if ((pawn.CurJobDef == JobDefOf.UseVerbOnThing || pawn.CurJobDef == JobDefOf.Goto || pawn.CurJobDef == JobDefOf.AttackMelee) &&
				pawn.mindState.duty.def != DutyDefOf.Sapper)
			{
				Log.Message($"{pawn} Already breaching or moving");
				return null;
			}

			Verb verb = BreachingUtility.FindVerbToUseForBreaching(pawn);
			var isBreacher = IsBreacher(pawn);

			if (!isBreacher)
				return null;

			LordToil toil = pawn.GetLord().CurLordToil;
			if (toil.data == null)
			{
				toil.data = new LordToilData_AssaultColonyBreachingPogo(toil.lord);
				((LordToilData_AssaultColonyBreachingPogo)toil.data).Breachers =
					toil.lord.ownedPawns.Where(x => IsBreacher(x)).ToList();
			}

			var data = (LordToilData_AssaultColonyBreachingPogo)toil.data;
			data.Breachers = data.Breachers.Where(x => IsBreacher(x)).ToList();
			if (data.Breachers?.Count() == 0)
			{
				return null;
			}

			IntVec3 cell = pawn.mindState.duty.focus.Cell;
			if (cell.IsValid && cell.DistanceToSquared(pawn.Position) < 25f && cell.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) && cell.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
			{
				pawn.GetLord().Notify_ReachedDutyLocation(pawn);
				return null;
			}
			
			BreachingTargetData breachingTarget = data.BreachingTargetData;
			if (isBreacher)
			{
				breachingTarget = UpdateBreachingTarget(pawn, verb, toil, data);
			}

			Log.Message($"pawn {pawn} verb: {verb} equp: {pawn.equipment?.PrimaryEq} firingpos: {breachingTarget.firingPosition}");	


			if (data.currentTarget == null || breachingTarget == null)
			{
				if (cell.IsValid && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
				{
					Job job = JobMaker.MakeJob(JobDefOf.Goto, cell, 500, true);
					BreachingUtility.FinalizeTrashJob(job);
					return job;
				}
				Log.Message($"no breachting target");
				return null;
			}
			else
			{
				pawn.mindState.breachingTarget = breachingTarget;
				Thing target = breachingTarget.target;
				IntVec3 firingPosition = breachingTarget.firingPosition;
				Log.Message($" firingpos: {firingPosition} target {target} destroyed: {target.Destroyed}");

				if (!firingPosition.IsValid)
				{
					return null;
				}

				if (!isBreacher)
				{
					Job job;
					if (firingPosition.DistanceToSquared(pawn.Position) < 50)
					{
						job = JobMaker.MakeJob(JobDefOf.Wait_Combat, firingPosition);
						job.collideWithPawns = false;
					}
					else
					{
						job = JobMaker.MakeJob(JobDefOf.Goto, firingPosition);
						
					}
					job.collideWithPawns = false;
					job.checkOverrideOnExpire = true;
					job.expiryInterval = 500;
					return job;
				}
				bool flag = firingPosition.Standable(pawn.Map) && pawn.Map.pawnDestinationReservationManager.CanReserve(firingPosition, pawn, false);
				
				Job job3 = JobMaker.MakeJob(JobDefOf.UseVerbOnThing, target, flag ? firingPosition : IntVec3.Invalid);
				job3.verbToUse = verb;
				job3.preventFriendlyFire = true;
				job3.checkOverrideOnExpire = true;
				job3.expireRequiresEnemiesNearby = true;
				job3.expiryInterval = 500;
				pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job3, firingPosition);
				return job3;
			}
		}

		private static Verb FindVerbToUseForBreaching(Pawn pawn)
		{
			Pawn_EquipmentTracker equipment = pawn.equipment;
			CompEquippable compEquippable = equipment != null ? equipment.PrimaryEq : null;
			if (compEquippable == null)
			{
				return null;
			}
			Verb primaryVerb = compEquippable.PrimaryVerb;
			if (BreachingUtility.UsableVerb(primaryVerb))
			{
				return primaryVerb;
			}
			List<Verb> allVerbs = compEquippable.AllVerbs;
			for (int i = 0; i < allVerbs.Count; i++)
			{
				Verb verb = allVerbs[i];
				if (BreachingUtility.UsableVerb(verb))
				{
					return verb;
				}
			}
			return null;
		}

		internal class LordToilData_AssaultColonyBreachingPogo : LordToilData_AssaultColonyBreaching
		{
			public LordToilData_AssaultColonyBreachingPogo(Lord lord) : base(lord)
			{
			}

			public BreachingTargetData BreachingTargetData { get; set; }

			public IEnumerable<Pawn> Breachers { get; set; } = null;
		}

		// Token: 0x06003DF6 RID: 15862 RVA: 0x00165648 File Offset: 0x00163848
		private static BreachingTargetData UpdateBreachingTarget(Pawn pawn, Verb verb, LordToil toil, LordToilData_AssaultColonyBreachingPogo data)
		{
			//Setup breach grid
			if (!data.breachDest.IsValid)
			{
				data.Reset();
				data.maxRange = data.Breachers.Min(x => x.equipment?.PrimaryEq?.PrimaryVerb?.verbProps?.range ?? 0);
				Log.Message($"maxrange: {data.maxRange}");
				data.preferMelee = Rand.Chance(0.5f);
				data.breachStart = pawn.PositionHeld;
				data.breachDest = GenAI.RandomRaidDest(data.breachStart, toil.Map);
				int breachRadius = Mathf.RoundToInt(LordToil_AssaultColonyBreaching.BreachRadiusFromNumRaiders.Evaluate(toil.lord.ownedPawns.Count));
				int walkMargin = Mathf.RoundToInt(LordToil_AssaultColonyBreaching.WalkMarginFromNumRaiders.Evaluate(toil.lord.ownedPawns.Count));
				data.breachingGrid.CreateBreachPath(data.breachStart, data.breachDest, breachRadius, walkMargin, true);
			}


			if (data.currentTarget != null && data.currentTarget.Destroyed)
			{
				data.currentTarget = null;
			}
				
			if (data.currentTarget == null)
			{
				data.currentTarget = FindBuildingToBreach(data.breachingGrid);
				data.soloAttacker = pawn;
			}

			if (data.currentTarget == null)
			{
				Log.Message($" NO BREACHING TARGET");
				return null;
			}

			Log.Message($"BREACHING TARGET FOUND");

			Log.Message($"{pawn} target {data.currentTarget} destroyed {data.currentTarget?.Destroyed}");

			BreachingGrid breachingGrid = data.breachingGrid;
			BreachingTargetData breachingTargetData = pawn.mindState.breachingTarget;
			bool flag = false;
			if (breachingTargetData != null && (breachingTargetData.target.Destroyed || data.currentTarget != breachingTargetData.target 
				|| breachingGrid.MarkerGrid[pawn.Position] == 10 || breachingTargetData.firingPosition.IsValid 
				&& !verb.IsMeleeAttack && !verb.CanHitTargetFrom(breachingTargetData.firingPosition, breachingTargetData.target) 
				|| breachingTargetData.firingPosition.IsValid && 
				!pawn.CanReach(breachingTargetData.firingPosition, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn) 
				|| data.soloAttacker != null && pawn != data.soloAttacker))
			{
				breachingTargetData = null;
				flag = true;
			}
			if (breachingTargetData == null && data.currentTarget != null)
			{
				flag = true;
				breachingTargetData = new BreachingTargetData(data.currentTarget, IntVec3.Invalid);
			}
			if (verb != null && !verb.IsMeleeAttack && breachingTargetData != null && !breachingTargetData.firingPosition.IsValid
				&& BreachingUtility.CanDamageTarget(verb, breachingTargetData.target)
				&& TryFindRangedCastPosition(data, pawn, verb, breachingTargetData.target, out breachingTargetData.firingPosition))
			{
				flag = true;
			}
			if (flag || verb == null || (verb.IsMeleeAttack && breachingTargetData != null))
			{
				Log.Message($"firingpos: {breachingTargetData.firingPosition}");
				pawn.mindState.breachingTarget = breachingTargetData;
				data.BreachingTargetData = breachingTargetData;
				breachingGrid.Notify_PawnStateChanged(pawn);
			}
			return data.BreachingTargetData;
		}

		private static bool TryFindRangedCastPosition(LordToilData_AssaultColonyBreaching lordToilData_AssaultColonyBreaching, Pawn pawn,
			Verb verb, Thing target, out IntVec3 result)
		{
			bool result2;
			try
			{
				result = IntVec3.Invalid;
				if (lordToilData_AssaultColonyBreaching == null)
				{
					result2 = false;
				}
				else
				{
					BreachingUtility.cachedRangedCastPositionFinder.breachingGrid = lordToilData_AssaultColonyBreaching.breachingGrid;
					BreachingUtility.cachedRangedCastPositionFinder.verb = verb;
					BreachingUtility.cachedRangedCastPositionFinder.target = target;
					CastPositionRequest castPositionRequest = default;
					castPositionRequest.caster = pawn;
					castPositionRequest.target = target;
					castPositionRequest.verb = verb;
					castPositionRequest.maxRangeFromTarget = verb.verbProps.range;
					if (lordToilData_AssaultColonyBreaching.soloAttacker == null)
					{
						castPositionRequest.maxRangeFromTarget = Mathf.Min(lordToilData_AssaultColonyBreaching.maxRange, castPositionRequest.maxRangeFromTarget);
					}
					castPositionRequest.validator = BreachingUtility.cachedRangedCastPositionFinder.safeForRangedCastFunc;
					IntVec3 intVec;
					if (CastPositionFinder.TryFindCastPosition(castPositionRequest, out intVec))
					{
						result = intVec;
						result2 = true;
					}
					else
					{
						result2 = false;
					}
				}
			}
			finally
			{
			}
			return result2;
		}

		private static Thing FindBuildingToBreach(BreachingGrid grid)
		{
			Building bestBuilding = null;
			int bestBuildingDist = int.MaxValue;
			int bestBuildingReachableSideCount = 0;
			grid.RegenerateCachedGridIfDirty();
			var randomPlayerBuilding = grid.map.listerBuildings.allBuildingsColonist.RandomElement();
			grid.Map.floodFiller.FloodFill(grid.breachStart, (c) => grid.BreachGrid[c], delegate (IntVec3 c, int dist)
			{
				List<Thing> thingList = c.GetThingList(grid.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					Building building;
					if ((building = thingList[i] as Building) != null && BreachingUtility.ShouldBreachBuilding(building)
						&& BreachingUtility.IsWorthBreachingBuilding(grid, building))
					{
						int num = BreachingUtility.CountReachableAdjacentCells(grid, building);
						var distBase = building.Position.DistanceToSquared(randomPlayerBuilding.Position);
						if (num > 0 && num > bestBuildingReachableSideCount && ((building.Faction?.IsPlayer ?? false) || distBase < 1000))
						{
							Log.Message($"Breach Target: {building} {building.Position} Player building: {randomPlayerBuilding} {randomPlayerBuilding.Position} " +
								$"Distance: {building.Position.DistanceToSquared(randomPlayerBuilding.Position)}");
							bestBuilding = building;
							bestBuildingDist = dist;
							bestBuildingReachableSideCount = num;
							break;
						}
					}
				}
				return dist - 2 > bestBuildingDist;
			}, int.MaxValue, false, null);
			return bestBuilding;
		}
		*/
    }
}
