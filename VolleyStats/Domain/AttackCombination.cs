using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Domain
{
    public class AttackCombination
    {
        required public string Combination { get; set; }
        public int StartPoint { get; set; }
        // TODO: EnuM??
        public char Dir { get; set; }
        public char Type { get; set; }
        public string? Desc { get; set; }
        public string? IdkRandom { get; set; }           // TODO
        required public string Color { get; set; }
        public char SetterCall { get; set; }
        public bool IdkRandom2 { get; set; }            // TODO 
    }
}
