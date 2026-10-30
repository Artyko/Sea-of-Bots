using System;
using UnityEngine;

// Classifies a damaged tapered shell by its real local outline.  The old
// fixed |x| > 2.9 test treated every ironclad side opening as a bow/stern
// opening and drew its jet along the outside of the armour belt.
public static class NavalLeakVisualMath
{
    public static bool IsSide(Vector3 point,NavalShipDefinition ship)
    {return Math.Abs(point.x)>=ship.HullHalfWidth(point.z)-.20f;}

    public static Vector3 Inward(Vector3 point,NavalShipDefinition ship)
    {
        if(IsSide(point,ship))return new Vector3(-Math.Sign(point.x),0f,0f);
        return new Vector3(0f,0f,-Math.Sign(point.z));
    }
}
