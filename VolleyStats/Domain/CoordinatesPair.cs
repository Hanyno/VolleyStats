using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Domain
{
    /// <summary>
    /// Int point in 2D (example: 45,45).
    /// </summary>
    public readonly struct IntPoint
    {
        public int X { get; }
        public int Y { get; }
        public IntPoint(int x, int y) { X = x; Y = y; }

        public override string ToString() => $"({X},{Y})";
    }

    public sealed class CoordinatesPair
    {
        public IntPoint? A { get; }
        public IntPoint? B { get; }

        public CoordinatesPair(IntPoint? a, IntPoint? b)
        {
            A = a;
            B = b;
        }

        public override string ToString()
        {
            var a = A?.ToString() ?? "null";
            var b = B?.ToString() ?? "null";
            return $"{a} -> {b}";
        }
    }
}
