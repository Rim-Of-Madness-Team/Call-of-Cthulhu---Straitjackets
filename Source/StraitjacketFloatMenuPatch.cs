using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JecsTools;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace StraitJacket
{
    public class StraitjacketFloatMenuPatch : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> GetFloatMenus()
        {
            List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> floatMenus = new List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();

            _Condition straitjacketCondition = new _Condition(_ConditionType.IsType, typeof(Pawn));
            Func<Vector3, Pawn, Thing, List<FloatMenuOption>> straitjacketFunc = delegate (Vector3 clickPos, Pawn pawn, Thing curThing)
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                Pawn target = curThing as Pawn;
                if (pawn != target && !pawn.Dead && !pawn.Downed)
                {
                    IntVec3 c = clickPos.ToIntVec3();
                    if (target?.RaceProps?.Humanlike ?? false)
                    {
                        //Let's proceed if our 'actor' is capable of manipulation
                        if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        {
                            //Does the target have a straitjacket?
                            //We can help them remove the straitjacket.
                            if (target?.apparel?.WornApparel?.FirstOrDefault(x => x.def == StraitjacketDefOf.ROM_Straitjacket) != null)
                            {
                                if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                                {
                                    opts.Add(new FloatMenuOption("CannotRemoveStraitjacket".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                }
                                else if (!pawn.CanReserve(target, 1))
                                {
                                    opts.Add(new FloatMenuOption("CannotRemoveStraitjacket".Translate() + ": " + "Reserved".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                }
                                else
                                {
                                    Action action = delegate
                                    {
                                        Job job = new Job(StraitjacketDefOf.ROM_TakeOffStraitjacket, target);
                                        job.count = 1;
                                        pawn.jobs.TryTakeOrderedJob(job);
                                    };
                                    opts.Add(new FloatMenuOption("RemoveStraitjacket".Translate(new object[]
                                    {
                                        target.LabelCap
                                    }), action, MenuOptionPriority.High, null, target, 0f, null, null));
                                }
                            }
                            //We can put one on!
                            else
                            {
                                if (pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).FirstOrDefault((Thing x) => x.def == StraitjacketDefOf.ROM_Straitjacket) != null)
                                {
                                    if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                                    {
                                        opts.Add(new FloatMenuOption("CannotForceStraitjacket".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                    }
                                    else if (!pawn.CanReserve(target, 1))
                                    {
                                        opts.Add(new FloatMenuOption("CannotForceStraitjacket".Translate() + ": " + "Reserved".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                    }
                                    else
                                    {
                                        Action action = delegate
                                        {
                                            Thing straitjacket = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(StraitjacketDefOf.ROM_Straitjacket), PathEndMode.Touch, TraverseParms.For(pawn));
                                            Job job = new Job(StraitjacketDefOf.ROM_ForceIntoStraitjacket, target, straitjacket);
                                            job.count = 1;
                                            job.locomotionUrgency = LocomotionUrgency.Sprint;
                                            pawn.jobs.TryTakeOrderedJob(job);
                                        };
                                        opts.Add(new FloatMenuOption("ForceStraitjacketUpon".Translate(new object[]
                                        {
                                        target.LabelCap
                                        }), action, MenuOptionPriority.High, null, target, 0f, null, null));
                                    }
                                }
                            }
                        }
                    }
                    return opts;
                }
                return null;
            };
            KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>> 
                curSec = new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>
                (straitjacketCondition, straitjacketFunc);
            floatMenus.Add(curSec);
            return floatMenus;
        }
    }
}
