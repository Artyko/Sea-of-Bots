public static class NavalStationArrival
{
    // Only small station alignment is allowed; returning sailors must actually walk the route.
    public static bool CanAnchor(float squaredWorldDistance){return squaredWorldDistance<=.16f;}
    // A seated operator is physically carried by a slowly training mount. This
    // limit is applied to the mount's per-tick displacement, never to a route
    // destination, so it cannot retrieve a sailor from elsewhere aboard.
    public static bool CanRideMovingPost(float squaredWorldShift){return squaredWorldShift<=1f;}
    public static bool KeepMovingPost(bool atPreviousPose,float squaredWorldShift)
    {return atPreviousPose&&CanRideMovingPost(squaredWorldShift);}
    // The main-battery seat is mounted on a known solid turntable.  Generic
    // station arrival also asks the physics scene whether the sampled feet are
    // supported; that is intentionally conservative for deck work, but the
    // thin rotating floor can sit between support rays and leave an operator
    // walking in place a few centimetres above the seat forever.
    public static bool AtMainBatterySeat(float dx,float dy,float dz,float scale)
    {
        float horizontal=(dx*dx+dz*dz)*scale*scale;
        float vertical=dy*scale;
        return horizontal<=.45f*.45f&&vertical>=-.12f&&vertical<=.8f;
    }
    public static bool OnRaisedSupport(float dx,float dy,float dz,float scale)
    {return dy>=0&&dy*scale<=.8f&&(dx*dx+dz*dz)*scale*scale<=.45f*.45f;}
}
