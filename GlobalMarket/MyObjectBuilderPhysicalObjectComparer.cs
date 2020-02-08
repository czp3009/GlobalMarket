using System.Collections.Generic;
using VRage.Game;

namespace GlobalMarket
{
    public class MyObjectBuilderPhysicalObjectComparer : IEqualityComparer<MyObjectBuilder_PhysicalObject>
    {
        public bool Equals(MyObjectBuilder_PhysicalObject x, MyObjectBuilder_PhysicalObject y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            return x.GetObjectId() == y.GetObjectId();
        }

        public int GetHashCode(MyObjectBuilder_PhysicalObject obj)
        {
            return obj.GetObjectId().GetHashCode();
        }
    }
}