using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace StraitJacket
{
    public class JobDriver_StraitjacketOn : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;

        private const TargetIndex StraitjacketIndex = TargetIndex.B;

        
        protected Pawn Takee
        {
            get
            {
                return (Pawn)base.job.GetTarget(TargetIndex.A).Thing;
            }
        }

        protected Apparel Straitjacket
        {
            get
            {
                return (Apparel)base.job.GetTarget(TargetIndex.B).Thing;
            }
        }

        // Verse.Pawn
        public bool CheckAcceptStraitJacket(Pawn victim, Pawn arrester)
        {
            if (victim.Faction != arrester.Faction || victim.InMentalState) 
            {
                if (JecsTools.GrappleUtility.TryGrapple(arrester, victim))
                    return true;
                return false;
            }
            return true;
        }


        public override bool TryMakePreToilReservations(bool yeaa)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null) && this.pawn.Reserve(this.job.targetB, this.job, 1, -1, null);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    Thing straitjacket = null;
                    this.pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out straitjacket, null);

                    Pawn pawnToForceIntoStraitjacket = (Pawn)TargetA.Thing;
                    if (pawnToForceIntoStraitjacket != null)
                    {
                        if (!pawnToForceIntoStraitjacket.InAggroMentalState)
                        {
                            GenClamor.DoClamor(pawn, 10f, ClamorDefOf.Harm);
                            if (!CheckAcceptStraitJacket(pawnToForceIntoStraitjacket, this.pawn))
                            {
                                this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                            }
                        }
                    }
                }, defaultCompleteMode = ToilCompleteMode.Instant
            };
            Toil toil2 = new Toil();
            toil2.defaultCompleteMode = ToilCompleteMode.Delay;
            toil2.defaultDuration = 500;
            toil2.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil2.initAction = delegate
            {
                Pawn pawnToForceIntoStraitjacket = (Pawn)TargetA.Thing;

                if (pawnToForceIntoStraitjacket != null)
                {
                    if (!pawnToForceIntoStraitjacket.InAggroMentalState)
                    {
                        PawnUtility.ForceWait(pawnToForceIntoStraitjacket, toil2.defaultDuration, this.pawn);
                    }
                }
            };
            yield return toil2;

            yield return new Toil
            {
                initAction = delegate
                {
                    Takee.apparel.Wear(Straitjacket);
                    Takee.outfits.forcedHandler.SetForced(Straitjacket, true);
                    Hediff pawnJacketHediff = Takee.health.hediffSet.GetFirstHediffOfDef(StraitjacketDefOf.ROM_RestainedByStraitjacket);
                    if (pawnJacketHediff == null)
                    {
                        pawnJacketHediff = HediffMaker.MakeHediff(StraitjacketDefOf.ROM_RestainedByStraitjacket, Takee);
                        Takee.health.AddHediff(pawnJacketHediff);
                    }
                }, defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }
    }
}
