using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace StraitJacket
{
    /*
     * 
     *  Harmony Classes
     *  ===============
     *  Harmony is a system developed by pardeike (aka Brrainz).
     *  It allows us to use pre/post method patches instead of using detours.
     * 
     */
    [StaticConstructorOnStartup]
    static class HarmonyStraitJacket
    {
        //Static Constructor
        /*
         * Contains 4 Harmony patches for 4 vanilla methods.
         * ===================
         * 
         * [PREFIX] JobGiver_OptimizeApparel -> SetNextOptimizeTick
         * [POSTFIX] ITab_Pawn_Gear -> InterfaceDrop
         * [POSTFIX] MentalBreaker -> get_CurrentPossibleMoodBreaks
         * [POSTFIX] FloatMenuMakerMap -> AddHumanlikeOrders
         * 
         */
        static HarmonyStraitJacket()
        {
            Harmony harmony = new Harmony("rimworld.jecrell.straitjacket");
            harmony.Patch(AccessTools.Method(typeof(JobGiver_OptimizeApparel), "SetNextOptimizeTick"), new HarmonyMethod(typeof(HarmonyStraitJacket).GetMethod("SetNextOptimizeTickPreFix")), null);
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "InterfaceDrop"), new HarmonyMethod(typeof(HarmonyStraitJacket).GetMethod("InterfaceDropPreFix")), null);
            harmony.Patch(AccessTools.Method(typeof(MentalBreaker), "get_CurrentPossibleMoodBreaks"), null, new HarmonyMethod(typeof(HarmonyStraitJacket).GetMethod("CurrentPossibleMoodBreaksPostFix")), null);
            //harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null, new HarmonyMethod(typeof(HarmonyStraitJacket).GetMethod("AddHumanlikeOrdersPostFix")));
        }

        // Verse.MentalBreaker
        public static void CurrentPossibleMoodBreaksPostFix(MentalBreaker __instance, ref IEnumerable<MentalBreakDef> __result)
        {
            //Declare variables for the process
            var pawn = (Pawn)AccessTools.Field(typeof(MentalBreaker), "pawn").GetValue(__instance);

            //IsWearingStraitJacket
            if (pawn?.apparel?.WornApparel?.FirstOrDefault(x => x.def == StraitjacketDefOf.ROM_Straitjacket) != null)
            {
                var thought = (Thought)AccessTools.Method(typeof(MentalBreaker), "RandomFinalStraw").Invoke(__instance, new object[] { });
                string reason = thought?.LabelCap ?? "";

                //Reset the mind state because we probably tried to start something before this process started.
                //pawn.mindState.mentalStateHandler.Reset();

                MentalBreakDef mentalBreakDef = null;
                if (!(__result?.TryRandomElementByWeight((MentalBreakDef d) => d.Worker.CommonalityFor(pawn), out mentalBreakDef) ?? false))
                {
                    return;
                }

                if (Rand.Range(0, 100) < 95) //95% of the time
                {
                    Cthulhu.Utility.DebugReport("StraitJacket :: Mental Break Triggered");
                    var stateDef = mentalBreakDef?.mentalState ?? ((Rand.Value > 0.5f) ? DefDatabase<MentalStateDef>.GetNamed("Berserk") : DefDatabase<MentalStateDef>.GetNamed("Wander_Psychotic"));
                    string label = "MentalBreakAvertedLetterLabel".Translate() + ": " + stateDef.beginLetterLabel;
                    string text = string.Format(stateDef.beginLetter, pawn.Label).AdjustedFor(pawn).CapitalizeFirst();
                    if (reason != null)
                    {
                        text = text + "\n\n" + "StraitjacketBenefit".Translate(new object[]
                        {
                        pawn.gender.GetPossessive(),
                        pawn.gender.GetObjective(),
                        pawn.gender.GetObjective() + "self"
                        });
                    }
                    Find.LetterStack.ReceiveLetter(label, text, stateDef.beginLetterDef, pawn, null);
                    __result = new List<MentalBreakDef>();
                    return;
                }
                //StripStraitJacket
                if (pawn?.apparel?.WornApparel?.FirstOrDefault(x => x.def == StraitjacketDefOf.ROM_Straitjacket) is Apparel clothing)
                {
                    if (pawn?.apparel?.TryDrop(clothing, out Apparel droppedClothing, pawn.Position, true) != null)
                    {
                        Messages.Message("StraitjacketEscape".Translate(pawn.LabelCap), MessageTypeDefOf.ThreatBig);// MessageSound.SeriousAlert);
                        pawn.mindState.mentalStateHandler.TryStartMentalState(mentalBreakDef.mentalState, reason, false, true, null);
                        __result = new List<MentalBreakDef>();


                    }
                }
            }
        }
        // RimWorld.ITab_Pawn_Gear
        /*
         *  PreFix
         * 
         *  Disables the drop button's effect if the user is wearing a straitjacket.
         *  A straitjacket user should not be able to take it off by themselves, right?
         *  
         */
        public static bool InterfaceDropPreFix(ITab_Pawn_Gear __instance, Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            Pawn __pawn = (Pawn)AccessTools.Method(typeof(ITab_Pawn_Gear), "get_SelPawnForGear").Invoke(__instance, new object[0]);
            if (__pawn != null)
            {
                if (apparel != null && __pawn.apparel != null && __pawn.apparel.WornApparel.Contains(apparel))
                {
                    if (apparel.def == StraitjacketDefOf.ROM_Straitjacket)
                    {
                        Messages.Message("CannotRemoveByOneself".Translate(new object[]
                        {
                        __pawn.Label
                        }), MessageTypeDefOf.RejectInput);//MessageSound.RejectInput);
                        return false;
                    }
                }
            }
            return true;
        }


        // RimWorld.JobGiver_OptimizeApparel
        /*
         *  PreFix
         * 
         *  This code prevents prisoners/colonists from automatically changing
         *  out of straitjackets into other clothes.
         *  
         */
        public static bool SetNextOptimizeTickPreFix(JobGiver_OptimizeApparel __instance, Pawn pawn)
        {
            if (pawn != null)
            {
                if (pawn.outfits != null)
                {
                    Outfit currentOutfit = pawn.outfits.CurrentOutfit;
                    List<Apparel> wornApparel = pawn.apparel.WornApparel;
                    if (wornApparel != null)
                    {
                        if (wornApparel.Count > 0)
                        {
                            if (wornApparel.FirstOrDefault((Apparel x) => x.def == StraitjacketDefOf.ROM_Straitjacket) != null)
                            {
                                return false;
                            }
                        }

                    }
                }
            }
            return true;
        }

        // RimWorld.FloatMenuMakerMap
        /*
         *  PostFix
         * 
         *  This code adds to the float menu list.
         * 
         *  Adds:
         *    + Force straitjacket on _____
         *    + Help _____ out of straitjacket
         * 
         */
        public static void AddHumanlikeOrdersPostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            foreach (Thing current in c.GetThingList(pawn.Map))
            {
                if (current is Pawn target && pawn != null && pawn != target && !pawn.Dead && !pawn.Downed)
                {
                    //We sadly can't handle aggro mental states or non-humanoids.
                    if ((target?.RaceProps?.Humanlike ?? false) && !target.InAggroMentalState)
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
                }
            }
        }

        // Verse.MentalBreaker
        /*
         *  PreFix
         * 
         *  By calling this code first, we can check if the pawn involved is wearing a straitjacket.
         *  If the colonist is wearing a straitjacket, do not trigger a standard mental break.
         *  Instead, declare te mental break averted.
         * 
         */
        public static bool TryDoRandomMoodCausedMentalBreakPreFix(MentalBreaker __instance)
        {
            //Declare variables for the process
            var pawn = (Pawn)AccessTools.Field(typeof(MentalBreaker), "pawn").GetValue(__instance);

            //IsWearingStraitJacket
            bool isWearingStraitJacket = false;
            if (pawn.apparel != null)
            {
                foreach (Apparel clothing in pawn.apparel.WornApparel)
                {
                    if (clothing.def == StraitjacketDefOf.ROM_Straitjacket) isWearingStraitJacket = true;
                }
            }
            if (!isWearingStraitJacket) return true;

            Thought thought = (Thought)AccessTools.Method(typeof(MentalBreaker), "RandomMentalBreakReason").Invoke(__instance, new object[] { });
            IEnumerable<MentalBreakDef> mentalBreaksList = (IEnumerable<MentalBreakDef>)AccessTools.Property(typeof(MentalBreaker), "CurrentPossibleMoodBreaks").GetValue(__instance, null);
            string reason = (thought == null) ? null : thought.LabelCap;

            //Reset the mind state because we probably tried to start something before this process started.
            pawn.mindState.mentalStateHandler.Reset();


            MentalBreakDef mentalBreakDef;
            if (!(mentalBreaksList.TryRandomElementByWeight((MentalBreakDef d) => d.Worker.CommonalityFor(pawn), out mentalBreakDef)))
            {
                return false;
            }

            if (Rand.Range(0, 100) < 95) //95% of the time
            {
                Cthulhu.Utility.DebugReport("StraitJacket :: Mental Break Triggered");
                MentalStateDef stateDef = mentalBreakDef.mentalState;
                string label = "MentalBreakAvertedLetterLabel".Translate() + ": " + stateDef.beginLetterLabel;
                string text = string.Format(stateDef.beginLetter, pawn.Label).AdjustedFor(pawn).CapitalizeFirst();
                if (reason != null)
                {
                    text = text + "\n\n" + "StraitjacketBenefit".Translate(new object[]
                    {
                        pawn.gender.GetPossessive(),
                        pawn.gender.GetObjective(),
                        pawn.gender.GetObjective() + "self"
                    });
                }
                Find.LetterStack.ReceiveLetter(label, text, stateDef.beginLetterDef, pawn, null);
                return false;
            }
            //StripStraitJacket
            if (pawn.apparel != null)
            {
                Apparel droppedClothing = null;
                List<Apparel> clothingList = new List<Apparel>(pawn.apparel.WornApparel);
                foreach (Apparel clothing in clothingList)
                {
                    if (clothing.def == StraitjacketDefOf.ROM_Straitjacket)
                    {
                        pawn.apparel.TryDrop(clothing, out droppedClothing, pawn.Position, true);
                    }
                }
            }
            Messages.Message("StraitjacketEscape".Translate(pawn.LabelCap), MessageTypeDefOf.ThreatBig);// MessageSound.SeriousAlert);

            pawn.mindState.mentalStateHandler.TryStartMentalState(mentalBreakDef.mentalState, reason, false, true, null);
            return false;
        }
    }
}
