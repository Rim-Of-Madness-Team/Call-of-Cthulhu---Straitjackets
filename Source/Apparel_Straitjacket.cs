using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace StraitJacket
{
    public class Apparel_Straitjacket : Apparel
    {
        //We don't want to wear this...
        public override float GetSpecialApparelScoreOffset()
        {
            return -1000f;
        }
    }
}
