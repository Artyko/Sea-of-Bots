public static class NavalStationArrival
{
    // Only small station alignment is allowed; returning sailors must actually walk the route.
    public static bool CanAnchor(float squaredWorldDistance){return squaredWorldDistance<=.16f;}
    public static bool OnRaisedSupport(float dx,float dy,float dz,float scale)
    {return dy>=0&&dy*scale<=.8f&&(dx*dx+dz*dz)*scale*scale<=.45f*.45f;}
}
