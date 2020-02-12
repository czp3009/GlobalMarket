using System.Collections.Generic;
using System.Linq;
using VRage;

namespace GlobalMarket
{
    internal static class MyFixedPointExtensions
    {
        internal static MyFixedPoint Aggregate(this IEnumerable<MyFixedPoint> enumerable) =>
            enumerable.Aggregate(MyFixedPoint.Zero, (acc, x) => acc + x);
    }
}