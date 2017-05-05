// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation
using System.Reflection;
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace Cthulhu
{
    public static class SanityLossSeverity
    {
        public const float Initial = 0.1f;
        public const float Minor = 0.25f;
        public const float Major = 0.5f;
        public const float Severe = 0.7f;
        public const float Extreme = 0.95f;
    }

    static public class Utility
    {
        public enum SanLossSev { None = 0, Hidden, Initial, Minor, Major, Extreme };
        public const string SanityLossDef = "CosmicHorror_SanityLoss";
        public const string AltSanityLossDef = "Cults_SanityLoss";
        
        public static bool modCheck = false;
        public static bool loadedCosmicHorrors = false;
        public static bool loadedIndustrialAge = false;
        public static bool loadedCults = false;


        public static bool IsMorning(Map map) { return GenLocalDate.HourInt(map) > 6 && GenLocalDate.HourInt(map) < 10; } 
        public static bool IsEvening(Map map) { return GenLocalDate.HourInt(map) > 18 && GenLocalDate.HourInt(map) < 22; } 
        public static bool IsNight(Map map) { return GenLocalDate.HourInt(map) > 22; } 

        //[DefOf]
        //public static class PawnKindDefOf
        //{
        //    public static PawnKindDef DarkYoung;
        //}

        public static bool isCosmicHorror(Pawn thing)
        {
            if (!IsCosmicHorrorsLoaded()) return false;

            var type = Type.GetType("CosmicHorror.CosmicHorrorPawn");
            if (type != null)
            {
                if (thing.GetType() == type)
                {
                    return true;
                }
            }
            return false;
        }

        public static float GetSanityLossRate(PawnKindDef kindDef)
        {
            float sanityLossRate = 0f;
            if (kindDef.ToString() == "CosmicHorror_StarVampire")
                sanityLossRate = 0.04f;
            if (kindDef.ToString() == "StarSpawnOfCthulhu")
                sanityLossRate = 0.02f;
            if (kindDef.ToString() == "DarkYoung")
                sanityLossRate = 0.004f;
            if (kindDef.ToString() == "DeepOne")
                sanityLossRate = 0.008f;
            if (kindDef.ToString() == "DeepOneGreat")
                sanityLossRate = 0.012f;
            if (kindDef.ToString() == "MiGo")
                sanityLossRate = 0.008f;
            if (kindDef.ToString() == "Shoggoth")
                sanityLossRate = 0.012f;
            return sanityLossRate;
        }

        public static bool CapableOfViolence(Pawn pawn, bool allowDowned = false)
        {
            if (pawn == null) return false;
            if (pawn.Dead) return false;
            if (pawn.Downed && !allowDowned) return false;
            List<WorkTags> list = pawn.story.DisabledWorkTags.ToList<WorkTags>();
	        if (list.Count == 0)
            {
                return true;
            }
	        else
            {
                foreach (WorkTags current in list)
                {
                    if (current == WorkTags.Violent) return false;
                }
            }
            return true;
        }


        public static void SpawnThingDefOfCountAt(ThingDef of, int count, TargetInfo target)
        {
            while (count > 0)
            {
                Thing thing = ThingMaker.MakeThing(of, null);

                thing.stackCount = Math.Min(count, of.stackLimit);
                GenPlace.TryPlaceThing(thing, target.Cell, target.Map, ThingPlaceMode.Near);
                count -= thing.stackCount;
            }
        }

        public static void SpawnPawnsOfCountAt(PawnKindDef kindDef, IntVec3 at, Map map, int count, Faction fac = null, bool berserk = false, bool target = false)
        {
            for (int i = 1; i <= count; i++)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(new TargetInfo(at, map))
                     where at.Walkable(map)
                     select cell).TryRandomElement(out at))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(kindDef, fac);
                    if (GenPlace.TryPlaceThing(pawn, at, map, ThingPlaceMode.Near, null))
                    {
                        //if (target) MapComponent_SacrificeTracker.Get.lastLocation = at;
                        //continue;
                    }
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    if (berserk) pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
            }
        }

        public static void ChangeResearchProgress(ResearchProjectDef projectDef, float progressValue, bool deselectCurrentResearch = false)
        {
            FieldInfo researchProgressInfo = typeof(ResearchManager).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);
            var researchProgress = researchProgressInfo.GetValue(Find.ResearchManager);
            PropertyInfo itemPropertyInfo = researchProgress.GetType().GetProperty("Item");
            itemPropertyInfo.SetValue(researchProgress, progressValue, new[] { projectDef });
            if (deselectCurrentResearch) Find.ResearchManager.currentProj = null;
            Find.ResearchManager.ReapplyAllMods();
        }

        public static float CurrentSanityLoss(Pawn pawn)
        {
            string sanityLossDef;
            sanityLossDef = AltSanityLossDef;
            if (IsCosmicHorrorsLoaded()) sanityLossDef = SanityLossDef;

            Hediff pawnSanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed(sanityLossDef));
            if (pawnSanityHediff != null)
            {
                return pawnSanityHediff.Severity;
            }
            return 0f;
        }


        public static void ApplyTaleDef(string defName, Map map)
        {
            Pawn randomPawn = map.mapPawns.FreeColonists.RandomElement<Pawn>();
            TaleDef taleToAdd = TaleDef.Named(defName);
            TaleRecorder.RecordTale(taleToAdd, new object[]
                    {
                        randomPawn,
                    });
        }

        public static void ApplyTaleDef(string defName, Pawn pawn)
        {
            TaleDef taleToAdd = TaleDef.Named(defName);
            if ((pawn.IsColonist || pawn.HostFaction == Faction.OfPlayer) && taleToAdd != null)
            {
                TaleRecorder.RecordTale(taleToAdd, new object[]
                {
                    pawn,
                });
            }
        }
        public static void ApplySanityLoss(Pawn pawn, float sanityLoss=0.3f, float sanityLossMax=1.0f)
        {
            if (pawn == null) return;
            string sanityLossDef;
            sanityLossDef = SanityLossDef;
            if (!IsCosmicHorrorsLoaded()) sanityLossDef = AltSanityLossDef;
            Hediff pawnSanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed(sanityLossDef));
            float trueMax = sanityLossMax;
            if (pawnSanityHediff != null)
            {
                if (pawnSanityHediff.Severity > trueMax) trueMax = pawnSanityHediff.Severity;
                float result = pawnSanityHediff.Severity;
                result += sanityLoss;
                result = Mathf.Clamp(result, 0.0f, trueMax);
                pawnSanityHediff.Severity = result;
            }
            else
            {
                Hediff sanityLossHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed(sanityLossDef), pawn, null);
                
                sanityLossHediff.Severity = sanityLoss;
                pawn.health.AddHediff(sanityLossHediff, null, null);
                
            }
        }

        public static int GetSocialSkill(Pawn p)
        {
            return p.skills.GetSkill(SkillDefOf.Social).Level;
        }

        public static int GetResearchSkill(Pawn p)
        {
            return p.skills.GetSkill(SkillDefOf.Research).Level;
        }

        public static bool IsCosmicHorrorsLoaded()
        {

            if (!modCheck) ModCheck();
            return loadedCosmicHorrors;
        }


        public static bool IsIndustrialAgeLoaded()
        {
            if (!modCheck) ModCheck();
            return loadedIndustrialAge;
        }



        public static bool IsCultsLoaded()
        {
            if (!modCheck) ModCheck();
            return loadedCults;
        }

        public static bool IsRandomWalkable8WayAdjacentOf(IntVec3 cell, Map map, out IntVec3 resultCell)
        {
            IntVec3 temp = cell.RandomAdjacentCell8Way();
            for (int i = 0; i < 100; i++)
            {
                temp = cell.RandomAdjacentCell8Way();
                if (temp.Walkable(map))
                {
                    resultCell = temp;
                    return true;
                }
            }
            resultCell = IntVec3.Invalid;
            return false;
        }

        public static void ModCheck()
        {
            loadedCosmicHorrors = false;
            loadedIndustrialAge = false;
            foreach (ModContentPack ResolvedMod in LoadedModManager.RunningMods)
            {
                if (loadedCosmicHorrors && loadedIndustrialAge && loadedCults) break; //Save some loading
                if (ResolvedMod.Name.Contains("Call of Cthulhu - Cosmic Horrors"))
                {
                    DebugReport("Loaded - Call of Cthulhu - Cosmic Horrors");
                    loadedCosmicHorrors = true;
                }
                if (ResolvedMod.Name.Contains("Call of Cthulhu - Industrial Age"))
                {
                    DebugReport("Loaded - Call of Cthulhu - Industrial Age");
                    loadedIndustrialAge = true;
                }
                if (ResolvedMod.Name.Contains("Call of Cthulhu - Cults"))
                {
                    DebugReport("Loaded - Call of Cthulhu - Cults");
                    loadedCults = true;
                }
            }
            modCheck = true;
            return;
        }

        public static void DebugReport(string x)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                Log.Message(x);
            }
        }


    }
}
