using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;

namespace Cthulhu.Detour
{
    internal static class _MentalBreaker
    {
        internal static FieldInfo _pawn;
        internal static FieldInfo _randomMentalBreakReason;

        internal static Pawn GetPawn(this MentalBreaker _this)
        {
            if (_MentalBreaker._pawn == null)
            {
                _MentalBreaker._pawn = typeof(MentalBreaker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
                if (_MentalBreaker._pawn == null)
                {
                    Log.ErrorOnce("Unable to reflect MentalBreaker.pawn!", 215432421);
                }
            }
            return (Pawn)_MentalBreaker._pawn.GetValue(_this);
        }
        internal static Thought GetRandomMentalBreakReason(this MentalBreaker _this)
        {
            if (_MentalBreaker._randomMentalBreakReason == null)
            {
                _MentalBreaker._randomMentalBreakReason = typeof(MentalBreaker).GetField("RandomMentalBreakReason", BindingFlags.Instance | BindingFlags.NonPublic);
                if (_MentalBreaker._randomMentalBreakReason == null)
                {
                    Log.ErrorOnce("Unable to reflect MentalBreaker.RandomMentalBreakReason!", 215432421);
                }
            }
            return (Thought)_MentalBreaker._randomMentalBreakReason.GetValue(_this);
        }
        internal static MentalBreakIntensity GetCurrentDesiredMoodBreakIntensity(this MentalBreaker _this)
        {
            //if (_MentalBreaker._pawn == null)
            //{
            var result = typeof(MentalBreaker).GetProperty("CurrentDesiredMoodBreakIntensity", BindingFlags.Instance | BindingFlags.NonPublic);
            //    if (_MentalBreaker._pawn == null)
            //    {
            //        Log.ErrorOnce("Unable to reflect MentalBreaker.pawn!", 215432421);
            //    }
            //}
            return (MentalBreakIntensity)result.GetValue(_this, null);
        }
        internal static bool GetCanDoRandomMentalBreaks(this MentalBreaker _this)
        {
            //if (_MentalBreaker._pawn == null)
            //{
            var result = typeof(MentalBreaker).GetProperty("CanDoRandomMentalBreaks", BindingFlags.Instance | BindingFlags.NonPublic);
            //    if (_MentalBreaker._pawn == null)
            //    {
            //        Log.ErrorOnce("Unable to reflect MentalBreaker.pawn!", 215432421);
            //    }
            //}
            return (bool)result.GetValue(_this, null);
        }
        internal static IEnumerable<MentalBreakDef> GetCurrentPossibleMoodBreaks(this MentalBreaker _this)
        {
            //if (_MentalBreaker._pawn == null)
            //{
            var result = typeof(MentalBreaker).GetProperty("CurrentPossibleMoodBreaks", BindingFlags.Instance | BindingFlags.NonPublic);
            //    if (_MentalBreaker._pawn == null)
            //    {
            //        Log.ErrorOnce("Unable to reflect MentalBreaker.pawn!", 215432421);
            //    }
            //}
            return (IEnumerable<MentalBreakDef>)result.GetValue(_this, null);
        }

        // Verse.MentalBreaker
        [Detour(typeof(MentalBreaker), bindingFlags = (BindingFlags.Instance | BindingFlags.Public))]
        internal static bool TryDoRandomMoodCausedMentalBreak(this MentalBreaker _this)
        {

            if (!_this.GetCanDoRandomMentalBreaks() || _this.GetPawn().Downed || !_this.GetPawn().Awake())
            {
                return false;
            }

            if (_this.GetPawn().Faction != Faction.OfPlayer && _this.GetCurrentDesiredMoodBreakIntensity() != MentalBreakIntensity.Extreme)
            {
                return false;
            }

            MentalBreakDef mentalBreakDef;
            if (!_this.GetCurrentPossibleMoodBreaks().TryRandomElementByWeight((MentalBreakDef d) => d.Worker.CommonalityFor(_this.GetPawn()), out mentalBreakDef))
            {
                Log.Message(_this.GetCurrentDesiredMoodBreakIntensity().ToString());
                foreach (MentalBreakDef def in _this.GetCurrentPossibleMoodBreaks())
                {
                    Log.Message(def.ToString());
                }
                return false;
            }

            MethodInfo method = typeof(MentalBreaker).GetMethod("RandomMentalBreakReason", BindingFlags.Instance | BindingFlags.NonPublic);

            Thought thought = null;
            if (method != null)
            {
                thought = (Thought)method.Invoke(_this, new object[] { });
            }

            //if (temp != null) thought = (Thought)temp.GetValue(_this);



            string reason = (thought == null) ? null : thought.LabelCap;


            if (_this.IsWearingStraitJacket())
            {
                if (Rand.Range(0, 100) < 95) //95% of the time
                {
                    Cthulhu.Utility.DebugReport("StraitJacket :: Mental Break Triggered");
                    MentalStateDef stateDef = mentalBreakDef.mentalState;
                    string label = "MentalBreakAvertedLetterLabel".Translate() + ": " + stateDef.beginLetterLabel;
                    string text = string.Format(stateDef.beginLetter, _this.GetPawn().Label).AdjustedFor(_this.GetPawn()).CapitalizeFirst();
                    if (reason != null)
                    {
                        text = text + "\n\n" + "MentalBreakReason".Translate(new object[]
                        {
                reason
                        });
                        text = text + "\n\n" + "StraitjacketBenefit".Translate(new object[]
                        {
                        _this.GetPawn().gender.GetPossessive(),
                        _this.GetPawn().gender.GetObjective(),
                        _this.GetPawn().gender.GetObjective() + "self"
                        });
                    }
                    Find.LetterStack.ReceiveLetter(label, text, stateDef.beginLetterType, _this.GetPawn(), null);
                    return false;
                }
                _this.StripStraitJacket();
                Messages.Message(_this.GetPawn().LabelCap + " has escaped out of their straitjacket!", MessageSound.SeriousAlert);
            }
            _this.GetPawn().mindState.mentalStateHandler.TryStartMentalState(mentalBreakDef.mentalState, reason, false, true, null);
            return true;
        }

        internal static bool IsWearingStraitJacket(this MentalBreaker _this)
        {
            if (_this.GetPawn().apparel != null)
            {
                foreach (Apparel clothing in _this.GetPawn().apparel.WornApparel)
                {
                    if (clothing.def.defName == "Straitjacket") return true;
                }
            }
            return false;
        }


        internal static void StripStraitJacket(this MentalBreaker _this)
        {
            Pawn pawn = _this.GetPawn();
            if (pawn.apparel != null)
            {
                Apparel droppedClothing = null;
                List<Apparel> clothingList = new List<Apparel>(pawn.apparel.WornApparel);
                foreach (Apparel clothing in clothingList)
                {
                    if (clothing.def.defName == "Straitjacket")
                    {
                        pawn.apparel.TryDrop(clothing, out droppedClothing, pawn.Position, true);
                    }
                }
            }
        }
    }
}
