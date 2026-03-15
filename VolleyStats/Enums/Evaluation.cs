using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Enums
{
    public enum Evaluation
    {
        Unknown = 0,
        Error,                 // =
        VeryPoorOrBlocked,     // /
        Poor,                  // -
        InsufficientOrCovered, // !
        Positive,              // +
        Point                  // #
    }
}
