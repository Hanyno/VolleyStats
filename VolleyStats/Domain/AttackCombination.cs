using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Domain
{
    public class AttackCombination
    {
        public string Combination { get; set; }
        public int StartPoint { get; set; }
        public char Player { get; set; }
        // TODO: EnuM??
        public char Type { get; set; }
        public string Desc { get; set; }

    }
}
