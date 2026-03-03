using System;
using System.Collections.Generic;
using System.Text;

namespace VolleyStats.Domain
{
    public class SetterCall
    {
        public string Call { get; set; }
        public string idk { get; set; } // TODO
        public string Name { get; set; }
        public string Desc { get; set; }
        public string ArrColor { get; set; }
        public List<string> ArrCoordinates { get; set; } = new List<string>();
        public List<string> PolygonCoordinates { get; set; } = new List<string>();
        public string Color { get; set; }
    }
}
