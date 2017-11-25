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
    public class JobDriver_StraitjacketOff : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;
        
        protected Pawn Takee
        {
            get
            {
                return (Pawn)base.job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public override bool TryMakePreToilReservations()
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null) && this.pawn.Reserve(this.job.targetB, this.job, 1, -1, null);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            //Toil 6: Time to chant ominously
            Toil toil2 = new Toil();
            toil2.defaultCompleteMode = ToilCompleteMode.Delay;
            toil2.defaultDuration = 1000;
            toil2.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil2.initAction = delegate
            {
                PawnUtility.ForceWait((Pawn)TargetA.Thing, toil2.defaultDuration, this.pawn);
            };
            yield return toil2;

            yield return new Toil
            {
                initAction = delegate
                {
                    Apparel straitjacket = Takee.apparel.WornApparel.FirstOrDefault((Apparel x) => x.def == StraitjacketDefOf.ROM_Straitjacket);
                    Apparel straitjacketOut;
                    if (straitjacket != null)
                    {
                        Takee.apparel.TryDrop(straitjacket, out straitjacketOut, Takee.Position);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }
    }
}
